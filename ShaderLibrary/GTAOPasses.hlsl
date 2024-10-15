#ifndef GTAO_PASSES
#define GTAO_PASSES

#include "Packages/com.barkar.bsrp/ShaderLibrary/GTAOFunctions.hlsl"

half4 FragGTAO(Varyings IN): SV_Target
{
    half2 uv = IN.uv;
    return GTAO(uv);
}

half4 FragGTAOSpatialX(Varyings IN) : SV_Target
{
    half2 uv = IN.uv.xy;
    half depth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_linear_clamp, uv).r;
    half linearDepth = Linear01Depth(depth, _ZBufferParams);
    half4 BentNormal_ao = SAMPLE_TEXTURE2D(_GTAOTexture, sampler_linear_clamp, uv);
    half4 AO = BilateralBlur(BentNormal_ao, linearDepth,uv, half2(1 / _RenderSizeParams.x, 0));//w?
    return AO;
}

half4 FragGTAOSpatialY(Varyings IN) : SV_Target
{
    half2 uv = IN.uv.xy;
    half depth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_linear_clamp, uv).r;
    half linearDepth = Linear01Depth(depth, _ZBufferParams);
    half4 BentNormal_ao = SAMPLE_TEXTURE2D(_GTAOTexture, sampler_linear_clamp, uv);
    return  BilateralBlur(BentNormal_ao, linearDepth,uv, half2(0, 1 / _RenderSizeParams.y));//w?
/*
    //////Reflection Occlusion
    half3 bentNormal = (AO.rgb);
   // bentNormal.z = -bentNormal.z;
    bentNormal = SafeNormalize(bentNormal);
   // bentNormal = SafeNormalize(SpheremapDecodeNormal(bentNormal));
   // bentNormal.z = -bentNormal.z;
    half2 normal = SAMPLE_TEXTURE2D(_GBuffer2, sampler_linear_clamp, IN.uv).rg;
    half3 normalWS = SafeNormalize( mul(UNITY_MATRIX_I_V,SpheremapDecodeNormal(normal)));// mul(UNITY_MATRIX_I_V;

    half4 gbuffer0 = SAMPLE_TEXTURE2D(_GBuffer0, sampler_linear_clamp, uv);
    half roughness = gbuffer0.a;

    half4 positionWS = mul(unity_MatrixIVP, half4(half3(uv * 2 - 1, depth), 1));
    positionWS.xyz /= positionWS.w;

    half3 viewDir= normalize(positionWS.xyz - _WorldSpaceCameraPos.rgb);
    half3 reflectionDir = reflect(viewDir, normalWS);
    half GTRO = ReflectionOcclusion(bentNormal, reflectionDir, roughness, 0.5);

    return lerp(1, half2(AO.a, GTRO), 1);//_AO_INTENSITY
    */
} 



#endif
