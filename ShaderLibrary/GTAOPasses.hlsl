#ifndef GTAO_PASSES
#define GTAO_PASSES

#include "Packages/com.barkar.bsrp/ShaderLibrary/GTAOFunctions.hlsl"

half4 FragGTAO(Varyings IN):SV_Target
{
    half2 uv = IN.uv;
    half depth = 0;
    
    half4 bn_ao = GTAO(uv, depth);
    half2 bentNormal = mul(UNITY_MATRIX_V, SpheremapEncodeNormal(half3(bn_ao.rg, -bn_ao.b)));
    return half4(bentNormal,bn_ao.a, depth);
    
}

half2 FragGTAOSpatialX(Varyings IN) : SV_Target
{
    half2 uv = IN.uv.xy;
    half2 AO = BilateralBlur(uv, half2(1 / _RenderSizeParams.x, 0)); //z?
    return AO;
}

half2 FragGTAOSpatialY(Varyings IN) : SV_Target
{
    half2 uv = IN.uv.xy;
    half2 AO = BilateralBlur(uv, half2(0, 1 / _RenderSizeParams.y));//w?

    //////Reflection Occlusion
    half3 bentNormal = SAMPLE_TEXTURE2D(_GTAOTexture, sampler_linear_clamp, uv);
    bentNormal = SafeNormalize(mul(UNITY_MATRIX_I_V, (SpheremapDecodeNormal(bentNormal))));
   // bentNormal.z = -bentNormal.z;
    half2 normal = SAMPLE_TEXTURE2D(_GBuffer2, sampler_linear_clamp, IN.uv).rg;
    half3 normalWS = SafeNormalize( mul(UNITY_MATRIX_I_V,(SpheremapDecodeNormal(normal))));// mul(UNITY_MATRIX_I_V;

    half4 gbuffer0 = SAMPLE_TEXTURE2D(_GBuffer0, sampler_linear_clamp, uv);
    half roughness = gbuffer0.a;

    half depth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_linear_clamp, uv).r;
    half4 positionWS = mul(unity_MatrixIVP, half4(half3(uv * 2 - 1, depth), 1));
    positionWS.xyz /= positionWS.w;

    half3 viewDir= normalize(positionWS.xyz - _WorldSpaceCameraPos.rgb);
    half3 reflectionDir = reflect(viewDir, normalWS);
    half GTRO = ReflectionOcclusion(bentNormal, reflectionDir, roughness, 0.5);

    return lerp(1, half2(AO.r, GTRO), 1);//_AO_INTENSITY
} 



#endif
