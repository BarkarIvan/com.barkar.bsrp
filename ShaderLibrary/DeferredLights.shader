Shader "Hidden/DeferredLights"
{
    HLSLINCLUDE
    #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/Surface.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/CustomBRDF.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"
    TEXTURE2D_HALF(_GBuffer0); //albedo, roughtness
    TEXTURE2D_HALF(_GBuffer1); //radiance, metallic
    TEXTURE2D_HALF(_GBuffer2); //normal
    TEXTURE2D_HALF(_GBuffer3); //emission
    TEXTURE2D_HALF(_CameraDepth);
    TEXTURE2D_HALF(_GTAOBentNormalTexture);

    StructuredBuffer<int> _TileLightCountBuffer;
    StructuredBuffer<int> _TileLightIndicesBuffer;
    int2 _TextureParams;


    half4 DirLightPassFragment(Varyings IN): SV_Target
    {
        half4 g0 = SAMPLE_TEXTURE2D(_GBuffer0, sampler_linear_clamp, IN.uv);
        half4 g1 = SAMPLE_TEXTURE2D(_GBuffer1, sampler_linear_clamp, IN.uv);
        float4 g2 = SAMPLE_TEXTURE2D(_GBuffer2, sampler_linear_clamp, IN.uv);
        float4 bent_ao = SAMPLE_TEXTURE2D(_GTAOBentNormalTexture, sampler_linear_clamp, IN.uv);
        half3 albedo = g0.rgb;
        half roughtness = g0.a;
        half metallic = g1.a;
        half3 normalWS = g2.rgb;
        normalWS = SafeNormalize(mul(UNITY_MATRIX_I_V, (SpheremapDecodeNormal(normalWS))));
        
        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_linear_clamp, IN.uv);
        float4 positionNDC = float4(IN.uv * 2 - 1, depth, 1);
        float4 positionWS = mul(unity_MatrixIVP, positionNDC);
        positionWS *= rcp(positionWS.w);

        float4 shadowCoord = TransformWorldToShadowCoord(positionWS);

        CustomLitData litData;
        litData.N = normalWS;
        litData.V = SafeNormalize(_WorldSpaceCameraPos - positionWS);
        
        CustomSurfaceData surfaceData;
        surfaceData.albedo = albedo;
        surfaceData.metallic = metallic;
        surfaceData.roughness = roughtness;
        surfaceData.albedo = lerp(surfaceData.albedo, float3(0.0, 0.0, 0.0), surfaceData.metallic);
        surfaceData.specular = lerp(kDielectricSpec.rgb, albedo, surfaceData.metallic);
        surfaceData.occlusion = bent_ao.a;
        
        Light light = GetMainLight(shadowCoord, positionWS);
        half3 brdf = StandardBRDF(litData, surfaceData, light.direction, light.color*surfaceData.occlusion, light.shadowAttenuation);
        
        half4 result;
        result.rgb = brdf;

        return half4(result.rgb, 1);
    }

    half4 PointLightsPassFRagment(Varyings IN): SV_Target
    {
        half4 g0 = SAMPLE_TEXTURE2D(_GBuffer0, sampler_linear_clamp, IN.uv);
        half metallic = SAMPLE_TEXTURE2D(_GBuffer1, sampler_linear_clamp, IN.uv).a;
        half2 normal = SAMPLE_TEXTURE2D(_GBuffer2, sampler_linear_clamp, IN.uv).rg;
        half3 normalWS = SafeNormalize(mul(UNITY_MATRIX_I_V, (SpheremapDecodeNormal(normal))));

        half3 albedo = g0.rgb;
        half smoothness = g0.a;

        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_linear_clamp, IN.uv).r;
        float4 positionNDC = float4(IN.uv * 2 - 1, depth, 1);
        float4 positionWS = mul(unity_MatrixIVP, positionNDC);
        positionWS *= rcp(positionWS.w);

        CustomLitData litData;
        litData.N = normalWS;
        litData.positionWS = positionWS;
        litData.V = SafeNormalize(_WorldSpaceCameraPos - positionWS.xyz);
        
        CustomSurfaceData surfaceData;
        surfaceData.albedo = albedo;
        surfaceData.metallic = metallic;
        surfaceData.occlusion = 1.0;
        surfaceData.albedo = lerp(surfaceData.albedo, float3(0.0, 0.0, 0.0), surfaceData.metallic);
        surfaceData.specular = lerp(kDielectricSpec.rgb, albedo, surfaceData.metallic);
        surfaceData.metallic = metallic;
        surfaceData.roughness = smoothness;
      
        surfaceData.alpha = 1.0;

        float2 pixelCoord = IN.uv * _TextureParams;
        int2 tileCoord = (pixelCoord) / (TILESIZE);
        int tileIndex = tileCoord.y * (_TextureParams.x / TILESIZE) + tileCoord.x;

        half3 result;
        int lightCount = _TileLightCountBuffer[tileIndex];
        Light light;
        
        for (int l = 0; l < lightCount; l++)
        {
            half constantOffset = 0.1;
            int lightIndex = _TileLightIndicesBuffer[tileIndex * PER_TILE_LIGHT_COUNT + l];
            float4 lightPos = PointLightPositionsAndRadius[lightIndex];
            half4 color = PointLightColors[lightIndex];
            float lighRange = color.w;
            half3 L = SafeNormalize(lightPos.xyz - positionWS.xyz);
            half distanceToLight = distance(positionWS.xyz, lightPos.xyz);
            half NoL = max(0, dot(L, normalWS));
            half p = distanceToLight * rcp(lighRange);
            half attenuation = rcp(constantOffset + distanceToLight * distanceToLight) * saturate(1.0 - p * p * p * p);

            light.color = PointLightColors[lightIndex];
            half3 brdf = StandardBRDF(litData, surfaceData, L, light.color, 1.0);
            result += NoL * brdf * attenuation;
        }
        return half4(result, 1);
    }
    ENDHLSL

    SubShader
    {
        Cull Off
        Blend One SrcAlpha
        BlendOp Add, Add
        ZWrite Off
        ZTest Always

        Stencil
        {
            Ref 8
            Comp Equal
        }

        Pass
        {
            Name "Directional Light Pass"
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment DirLightPassFragment
            #pragma multi_compile _ _SOFT_SHADOWS_LOW _SOFT_SHADOWS_MEDIUM _SOFT_SHADOWS_HIGH
            ENDHLSL
        }

        Pass
        {
            Name "Point Light Pass"
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment PointLightsPassFRagment
            ENDHLSL
        }
    }
}