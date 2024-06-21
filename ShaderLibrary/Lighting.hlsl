#ifndef  CUSTOM_LIGHTING
#define  CUSTOM_LIGHTING

#include "Packages/com.barkar.bsrp/ShaderLibrary/RealTimeLight.hlsl"




half3 CalculateFog(half4 color, float3 positionWS)
{
    float viewZ = -(mul(UNITY_MATRIX_V, float4(positionWS, 1)).z);
    float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
    half fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
    half intensity = ComputeFogIntensity(fogFactor);
    return lerp(color.rgb, unity_FogColor.rgb, (1.0 - intensity));
}

half3 GetDiffuseLighting(Light light, Surface surface)
{
    half3 attenuatedLightCol = light.color * light.shadowAttenuation;
    half NoL = saturate(dot(surface.normal, light.direction));
    half3 lightDiffuse = attenuatedLightCol * NoL;
    return lightDiffuse;
}

half3 GetDiffuseLighting(Light light, half NoL)
{
    half3 attenuatedLightCol = light.color * light.shadowAttenuation;
    half3 lightDiffuse = attenuatedLightCol * NoL;
    return lightDiffuse;
}



half3 GetDiffuseLightingHalfLambert(Light light, Surface surface)
{
    half3 attenuatedLightCol = light.color * light.shadowAttenuation;
    half NoL = saturate(dot(surface.normal, light.direction)) * 0.5 + 0.5;
    half3 lightDiffuse = attenuatedLightCol * NoL;
    return lightDiffuse;
}

real3 DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    real alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);

    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
}

half3 GetReflectionProbe(Surface surface)
{
    half3 rV = reflect(-surface.viewDir, surface.normal);
    half4 probe = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, rV,
                                         (1.0 - surface.smoothness) * 6 );//UNITY_SPECCUBE_LOD_STEPS
    half3 envirReflection = DecodeHDREnvironment(probe, unity_SpecCube0_HDR);
    return envirReflection;
}

//STYLIZED
struct Ramp
{
    half MediumThreshold;
    half MediumSmooth;
    half3 MediumColor;

    half ShadowThreshold;
    half ShadowSmooth;
    half3 ShadowColor;

    half ReflectThreshold;
    half ReflectSmooth;
    half3 ReflectColor;
};


half LinearStep(half minVal, half maxVal, half In)
{
    return saturate((In - minVal) / (maxVal - minVal));
}

half3 GetStylizeLighting(Ramp ramp, Light light, half NoL)
{
    return light.color * light.shadowAttenuation * (saturate(NoL));
}


half3 CalculateStylizedRadiance(half attenuation, Ramp ramp, half NoL, half3 brush, half3 brushStrenghtRGB)
{
    
    #if defined (_BRUSHTEX)
    half halfLambertMed = NoL * lerp(0.5, brush.r, brushStrenghtRGB.r)+0.5;
    half halfLambertShadow = NoL * lerp(0.5, brush.g, brushStrenghtRGB.g)+0.5;
    half halfLambertRefl = NoL * lerp(0.5, brush.b, brushStrenghtRGB.b)+0.5;
    #else
    half halfLambertMed = 0.5*NoL+0.5;
    half halfLambertShadow = halfLambertMed;
    half halfLambertRefl = halfLambertMed;
    #endif

    half smoothMed = LinearStep(ramp.MediumThreshold - ramp.MediumSmooth, ramp.MediumThreshold + ramp.MediumSmooth, halfLambertMed);
    half3 colorMed = lerp(ramp.MediumColor, 1, smoothMed);
    half smoothShadow = LinearStep(ramp.ShadowThreshold - ramp.ShadowSmooth, ramp.ShadowThreshold + ramp.ShadowSmooth, halfLambertShadow* (attenuation*(NoL))); 
    half3 colorShadow = lerp(ramp.ShadowColor, colorMed, smoothShadow);
    half smoothRefl = LinearStep(ramp.ReflectThreshold - ramp.ReflectSmooth, ramp.ReflectThreshold + ramp.ReflectSmooth, halfLambertRefl);
    half3 colorRefl = lerp(ramp.ReflectColor, colorShadow, smoothRefl);
    return colorRefl;
    
} 


#endif
