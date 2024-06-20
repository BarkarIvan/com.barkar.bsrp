#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"


TEXTURE2D_SHADOW(_MainLightShadowMap);
SAMPLER_CMP(sampler_LinearClampCompare);



#if defined(_SOFT_SHADOWS_LOW)
#define DIRECTIONAL_FILTER_SAMPLES 4
#define DIRECTIONAL_FILTER_TENT SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_SOFT_SHADOWS_MEDIUM)
#define DIRECTIONAL_FILTER_SAMPLES 9
#define DIRECTIONAL_FILTER_TENT SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_SOFT_SHADOWS_HIGH)
#define DIRECTIONAL_FILTER_SAMPLES 16
#define DIRECTIONAL_FILTER_TENT SampleShadow_ComputeSamples_Tent_7x7
#endif



half4 _MainLightShadowDistanceFade; //max distance, fadeWidth, 0, 0,
half4 _MainLightShadowMapSize;

half SampleDirectionalShadowMap(float3 shadowCoord)
{
    return SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowMap, sampler_LinearClampCompare, shadowCoord);
}

half ApplyShadowFade(float3 positionWS, half shadowAttenuation, half4 shadowData)
{
    half fade = 1.0 - saturate(
                    (distance(positionWS, _WorldSpaceCameraPos.xyz) - _MainLightShadowDistanceFade.x) /
                    _MainLightShadowDistanceFade.y);

    return LerpWhiteTo(shadowAttenuation, shadowData.x * fade);
}

half SampleFilteredShadowMap(float3 positionWS,float4 shadowCoord, half4 shadowData)
{
    shadowCoord.xyz = shadowCoord.xyz / shadowCoord.w;
    #if defined(DIRECTIONAL_FILTER_TENT)
    real weights[DIRECTIONAL_FILTER_SAMPLES];
    real2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _MainLightShadowMapSize.yyxx;
    DIRECTIONAL_FILTER_TENT(size, shadowCoord.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleDirectionalShadowMap(
            float3(positions[i].xy, shadowCoord.z));
    }
    return ApplyShadowFade(positionWS, shadow, shadowData);
    #else
    half shadow = SampleDirectionalShadowMap(shadowCoord);
    return ApplyShadowFade(positionWS, shadow, shadowData);
    #endif
}




#endif
