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


    half4 LightPassFragment(Varyings IN): SV_Target
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
        result.rgb = environmentBRDF;//* radiance;

        return half4(result.rgb, 1);
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
            #pragma fragment LightPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "Point Lights Pass"
        }
    }
}