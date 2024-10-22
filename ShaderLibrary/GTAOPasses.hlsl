#ifndef GTAO_PASSES
#define GTAO_PASSES

#include "Packages/com.barkar.bsrp/ShaderLibrary/GTAOFunctions.hlsl"

half4 FragGTAO(Varyings IN): SV_Target
{
    half2 uv = IN.uv;
    half4 result = GTAO(uv);
    return result;
}

half4 FragGTAOSpatialX(Varyings IN) : SV_Target
{
    half2 uv = IN.uv.xy;
    half depth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_linear_clamp, uv).r;
    half linearDepth = Linear01Depth(depth, _ZBufferParams);
    half4 BentNormal_ao = SAMPLE_TEXTURE2D(_GTAOTexture, sampler_linear_clamp, uv);
    half4 AO = BilateralBlur(BentNormal_ao, linearDepth,uv, half2(1 / _GTAOTextureParams.x, 0));//w?
    return AO;
}

half4 FragGTAOSpatialY(Varyings IN) : SV_Target
{
    half2 uv = IN.uv.xy;
    half depth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_linear_clamp, uv).r;
    half linearDepth = Linear01Depth(depth, _ZBufferParams);
    half4 BentNormal_ao = SAMPLE_TEXTURE2D(_GTAOTexture, sampler_linear_clamp, uv);
    half4 AO =  BilateralBlur(BentNormal_ao, linearDepth,uv, half2(0, 1 / _GTAOTextureParams.y));//w?
    AO.a = lerp(1, AO.a, _GTAO_Intencity);
    return AO;
   

    
} 



#endif
