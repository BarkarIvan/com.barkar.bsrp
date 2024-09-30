#ifndef CUSTOM_REAL_TIME_LIGHT_INCLUDED
#define CUSTOM_REAL_TIME_LIGHT_INCLUDED


CBUFFER_START(MainLightDataBuffer)
    half4 MainLightColor;
    half4 MainLightDirectionaAndMask;
    half4 MainLightShadowsData; //light strength, shadowBias, normalBias
CBUFFER_END

float4x4 _MainLightMatrix;

#include "Packages/com.barkar.bsrp/ShaderLibrary/Shadows.hlsl"

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
    mainLight.direction = (MainLightDirectionaAndMask.xyz);
    mainLight.layerMask = MainLightDirectionaAndMask.w;
    mainLight.shadowAttenuation = SampleFilteredShadowMap(positionWS, shadowCoord, MainLightShadowsData);
    return mainLight;
}

Light GetMainLight(float3 positionWS)
{
    Light mainLight;
    mainLight.color =  MainLightColor;
    mainLight.direction = MainLightDirectionaAndMask.xyz;
    mainLight.layerMask = MainLightDirectionaAndMask.w;
    return mainLight;
}


#endif
