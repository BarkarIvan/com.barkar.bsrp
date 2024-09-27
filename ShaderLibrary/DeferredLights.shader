Shader "Hidden/DeferredLights"
{
    HLSLINCLUDE
    #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/Surface.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/CustomBRDF.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"
    TEXTURE2D_HALF(_GBuffer0); //albedo, smoothness
    TEXTURE2D_HALF(_GBuffer1); //radiance, metallic
    TEXTURE2D_HALF(_GBuffer2); //normal
    TEXTURE2D_HALF(_GBuffer3); //emission
    TEXTURE2D_HALF(_CameraDepth);

    StructuredBuffer<int> _TileLightCountBuffer;
    StructuredBuffer<int> _TileLightIndicesBuffer;
    int2 _TextureParams;

 
    half4 DirLightPassFragment(Varyings IN): SV_Target
    {
        half4 g0 = SAMPLE_TEXTURE2D(_GBuffer0, sampler_linear_clamp, IN.uv);
        half4 g1 = SAMPLE_TEXTURE2D(_GBuffer1, sampler_linear_clamp, IN.uv);
        float4 g2 = SAMPLE_TEXTURE2D(_GBuffer2, sampler_linear_clamp, IN.uv);

        half3 albedo = g0.rgb;
        half smoothness = g0.a;
        half3 radiance = g1.rgb;
        half metallic = g1.a;
        half3 normal = g2.rgb;
        normal = SafeNormalize(mul(UNITY_MATRIX_I_V,(SpheremapDecodeNormal(normal))));


        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_linear_clamp, IN.uv);
        float4 positionNDC = float4(IN.uv * 2 - 1, depth, 1);
        float4 positionWS = mul(unity_MatrixIVP, positionNDC);
        positionWS *= rcp(positionWS.w);

        half3 lightColor = MainLightColor;

        Surface surface;
        surface.albedo = albedo;
        surface.normal = normal;
        surface.metallic = metallic;
        surface.smoothness = smoothness;
        surface.viewDir = SafeNormalize(_WorldSpaceCameraPos - positionWS.xyz);

        Light light = GetMainLight(positionWS);
        BRDF brdf = GetBRDFGBuffer(surface);
        lightColor *= DirectBRDF(surface, brdf, light) * radiance;
        
        half4 result;
        result.rgb = lightColor;

        return half4(result.rgb, 1);
    }

    half4 PointLightsPassFRagment(Varyings IN): SV_Target
    {
        half4 g0 = SAMPLE_TEXTURE2D(_GBuffer0, sampler_linear_clamp, IN.uv);
        half metallic  = SAMPLE_TEXTURE2D(_GBuffer1, sampler_linear_clamp, IN.uv).a;
        half2 normal = SAMPLE_TEXTURE2D(_GBuffer2, sampler_linear_clamp, IN.uv).rg;
        half3 normalWS = SafeNormalize(mul(UNITY_MATRIX_I_V,(SpheremapDecodeNormal(normal))));
        
        half3 albedo = g0.rgb;
        half smoothness = g0.a;
        
        //float3 normal = SafeNormalize(normalWS.rgb * 2 - 1);


        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_linear_clamp, IN.uv).r;
        float4 positionNDC = float4(IN.uv * 2 - 1, depth, 1);
        float4 positionWS = mul(unity_MatrixIVP, positionNDC);
        positionWS *= rcp(positionWS.w);
        
        Surface surface;
        surface.albedo = albedo;
        surface.normal = normalWS;
        surface.metallic = metallic;
        surface.smoothness = smoothness;
        surface.viewDir = SafeNormalize(_WorldSpaceCameraPos - positionWS.xyz);
        surface.alpha = 1.0;
        BRDF brdf = GetBRDFGBuffer(surface);
        //normalWS = SafeNormalize(normalWS) * 2.0 - 1.0;
        
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
            half3 dir = SafeNormalize(lightPos.xyz - positionWS.xyz);
            half distanceToLight = distance(positionWS.xyz, lightPos.xyz);
            half NoL = max(0, dot(dir, normalWS));
            half  p = distanceToLight * rcp(lighRange);
            half attenuation =  rcp(constantOffset + distanceToLight * distanceToLight) * saturate(1.0 - p * p * p * p);

            light.color = PointLightColors[lightIndex] ;
            light.direction = dir;
            light.shadowAttenuation = 0;
            half3 resultColor = (attenuation) *( light.color * DirectBRDF(surface, brdf, light));
            
            result += NoL * resultColor;
            //result = lightCount;
        }
        return half4(result, 1);
    }
    ENDHLSL

    SubShader
    {
        Cull Off
        Blend One One
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