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
        float3 normal = SafeNormalize(g2.rgb * 2 - 1);


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

        half3 environmentBRDF = EnvironmentBRDF(surface, brdf, lightColor, radiance);

        half4 result;
        result.rgb = environmentBRDF; //* radiance;

        return half4(result.rgb, 1);
    }

    half4 PointLightsPassFRagment(Varyings IN): SV_Target
    {
        half3 normalWS = SAMPLE_TEXTURE2D(_GBuffer2, sampler_PointClamp, IN.uv) * 2.0 - 1.0;

        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_PointClamp, IN.uv).r;
        float4 positionNDC = float4(IN.uv * 2 - 1, depth, 1);
        float4 positionWS = mul(unity_MatrixIVP, positionNDC);
        positionWS *= rcp(positionWS.w);

        float2 pixelCoord = IN.uv * _TextureParams;
        int2 tileCoord = (pixelCoord / 16);
        int tileIndex = tileCoord.y * (_TextureParams.x / 16) + tileCoord.x;
        // int lightCount = 0;
        half3 result;
        int lightCount = _TileLightCountBuffer[tileIndex];
        half constantOffset = 0.1;
        for (int l = 0; l < lightCount; l++)
        {
            int lightIndex = _TileLightIndicesBuffer[tileIndex * PER_TILE_LIGHT_COUNT + l];
            float4 pos = PointLightPositionsAndRadius[lightIndex];
            half4 color = PointLightColors[lightIndex];
            float r = color.w;
            half dir = normalize(pos.xyz - positionWS.xyz);
            half dist = distance(positionWS.xyz, pos.xyz);
            half NoL = max(0, dot(dir, normalize(normalWS)));
            half  p = dist / r;
            half attenuation = (1.0 / (constantOffset + dist * dist)) * (1.0 - p * p * p * p);
            result =  NoL;//saturate(attenuation) * PointLightColors[lightIndex] ;
            
        }
        return half4(result, 1);
    }
    ENDHLSL

    SubShader
    {

        Cull Off
         Blend One One
        BlendOp Add, Add
        ZWrite On
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