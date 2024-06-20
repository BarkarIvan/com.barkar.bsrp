#ifndef CUSTOM_REAL_TIME_LIGHT_INCLUDED
#define CUSTOM_REAL_TIME_LIGHT_INCLUDED

#include "Shadows.hlsl"


CBUFFER_START(MainLightDataBuffer)
    half4 MainLightColor;
    half4 MainLightDirectionaAndMask;
    half4 MainLightShadowsData; //light strength, shadowBias, normalBias
CBUFFER_END

float4x4 _MainLightMatrix;


struct Light
{
    half3 direction;
    half3 color;
    half shadowAttenuation;
    uint layerMask;
};

Light GetMainLight(float4 shadowCoord, float3 positionWS)
{
    Light mainLight;
    mainLight.color =  MainLightColor;
    mainLight.direction = MainLightDirectionaAndMask.xyz;
    // TODO surface
    mainLight.shadowAttenuation = SampleFilteredShadowMap(positionWS, shadowCoord, MainLightShadowsData);
    return mainLight;
}


#endif
