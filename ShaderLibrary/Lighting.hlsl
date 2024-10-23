#ifndef CUSTOM_LIGHTING
#define CUSTOM_LIGHTING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.barkar.bsrp/ShaderLibrary/CustomLitData.hlsl"
#include "Packages/com.barkar.bsrp/ShaderLibrary/RealTimeLight.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"

//TODO GI
#define LIGHTMAP_NAME unity_Lightmap
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap
#define LIGHTMAP_SAMPLE_EXTRA_ARGS lightmapUV
half3 SampleLightmap(float2 lightmapUV, half3 normalWS)
{
    // The shader library sample lightmap functions transform the lightmap uv coords to apply bias and scale.
    // However, builtin pipeline already transformed those coords in vertex. We pass half4(1, 1, 0, 0) and
    // the compiler will optimize the transform away.
    half4 transformCoords = half4(1, 1, 0, 0);

    //#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
  //  return SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
    //    TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME),
   //     LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, normalWS, true);
  //  #elif defined(LIGHTMAP_ON)
    return SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, true);
  //  #else
   // return half3(0.0, 0.0, 0.0);
    #endif
}

half3 SampleSHPixel(half3 L2Term, half3 normalWS)
{
    half3 L0L1Term = SHEvalLinearL0L1(normalWS, unity_SHAr, unity_SHAg, unity_SHAb);
    half3 res = L2Term + L0L1Term;
    return max(half3(0, 0, 0), res);
}
//


#define MIN_REFLECTIVITY 0.04
#define kDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04)


#if defined(LIGHTMAP_ON)
#define SAMPLE_GI(lmName, shName, normalWSName) SampleLightmap(lmName, normalWSName)
#else
#define SAMPLE_GI(lmName, shName, normalWSName) SampleSHPixel(shName, normalWSName)
#endif


half3 GlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion)
{
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);
    half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    return irradiance * occlusion;
}


half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion)
//, float2 normalizedScreenSpaceUV
{
    //  #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
    half3 irradiance;

    #if defined(_REFLECTION_PROBE_BLENDING)
    irradiance = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness, normalizedScreenSpaceUV);
     #else
    //#ifdef _REFLECTION_PROBE_BOX_PROJECTION
    //reflectVector = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    //#endif // _REFLECTION_PROBE_BOX_PROJECTION
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encodedIrradiance =
        half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));

    irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    #endif // _REFLECTION_PROBE_BLENDING
    return irradiance * occlusion;
    //#else
    //return _GlossyEnvironmentColor.rgb * occlusion;
    ////#endif // _ENVIRONMENTREFLECTIONS_OFF
} //


//BRDF
//Based on https://github.com/Nuomi-Chobits/Unity-URP-PBR/tree/main

//D
// GGX / Trowbridge-Reitz
// [Walter et al. 2007, "Microfacet models for refraction through rough surfaces"]
float D_GGX_UE5(float a2, float NoH)
{
    float d = (NoH * a2 - NoH) * NoH + 1; // 2 mad
    return a2 / (PI * d * d); // 4 mul, 1 rcp
}

//Vis
float Vis_Implicit()
{
    return 0.25;
}

// Appoximation of joint Smith term for GGX
// [Heitz 2014, "Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs"]
float Vis_SmithJointApprox(float a2, float NoV, float NoL)
{
    float a = sqrt(a2);
    float Vis_SmithV = NoL * (NoV * (1 - a) + a);
    float Vis_SmithL = NoV * (NoL * (1 - a) + a);
    return 0.5 * rcp(Vis_SmithV + Vis_SmithL);
}

//F
float3 F_None(float3 SpecularColor)
{
    return SpecularColor;
}

// [Schlick 1994, "An Inexpensive BRDF Model for Physically-Based Rendering"]
float3 F_Schlick_UE5(float3 SpecularColor, float VoH)
{
    float Fc = Pow5(1 - VoH); // 1 sub, 3 mul
    //return Fc + (1 - Fc) * SpecularColor;		// 1 add, 3 mad

    // Anything less than 2% is physically impossible and is instead considered to be shadowing
    return saturate(50.0 * SpecularColor.g) * Fc + (1 - Fc) * SpecularColor;
}

float3 Diffuse_Lambert(float3 DiffuseColor)
{
    return DiffuseColor * (1 / PI);
}

half3 EnvBRDFApprox(half3 SpecularColor, half Roughness, half NoV)
{
    // [ Lazarov 2013, "Getting More Physical in Call of Duty: Black Ops II" ]
    // Adaptation to fit our G term.
    const half4 c0 = {-1, -0.0275, -0.572, 0.022};
    const half4 c1 = {1, 0.0425, 1.04, -0.04};
    half4 r = Roughness * c0 + c1;
    half a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
    half2 AB = half2(-1.04, 1.04) * a004 + r.zw;

    // Anything less than 2% is physically impossible and is instead considered to be shadowing
    // Note: this is needed for the 'specular' show flag to work, since it uses a SpecularColor of 0
    AB.y *= saturate(50.0 * SpecularColor.g);

    return SpecularColor * AB.x + AB.y;
}

float3 SpecularGGX(float a2, float3 specular, float NoH, float NoV, float NoL, float VoH)
{
    float D = D_GGX_UE5(a2, NoH);
    float Vis = Vis_SmithJointApprox(a2, NoV, NoL);
    float3 F = F_Schlick_UE5(specular, VoH);
    return (D * Vis) * F;
}


half3 StandardBRDF(CustomLitData customLitData, CustomSurfaceData customSurfaceData, half3 L, half3 lightColor,
                   float shadow)
{
    float a2 = Pow4(customSurfaceData.roughness) + 0.001;

    half3 H = normalize(customLitData.V + L);
    half NoH = saturate(dot(customLitData.N, H));
    half NoV = saturate(abs(dot(customLitData.N, customLitData.V)) + 1e-5);
    half NoL = saturate(dot(customLitData.N, L));
    half VoH = saturate(dot(customLitData.V, H)); //LoH
    float3 radiance = NoL * lightColor * shadow * PI;
    float3 diffuseTerm = Diffuse_Lambert(customSurfaceData.albedo);
    float3 specularTerm = SpecularGGX(a2, customSurfaceData.specular, NoH, NoV, NoL, VoH);
    return (diffuseTerm + specularTerm) * radiance;
}

half3 EnvBRDF(CustomLitData customLitData, CustomSurfaceData customSurfaceData, float envRotation, float3 positionWS,
              half3 indirectDiffuse)
{
    half NoV = saturate(abs(dot(customLitData.N, customLitData.V)) + 1e-5);
    half3 R = reflect(-customLitData.V, customLitData.N);
    R = RotateDirection(R, envRotation);

    //SH
    float3 diffuseAO = GTAOMultiBounce(customSurfaceData.occlusion, customSurfaceData.albedo);
    float3 indirectDiffuseTerm = indirectDiffuse * customSurfaceData.albedo * diffuseAO;

    //IBL
    //The Split Sum: 1nd Stage
    half3 specularLD = GlossyEnvironmentReflection(R, positionWS, customSurfaceData.roughness,
                                                   customSurfaceData.occlusion);
    //The Split Sum: 2nd Stage
    half3 specularDFG = EnvBRDFApprox(customSurfaceData.specular, customSurfaceData.roughness, NoV);
    //AO
    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(NoV, customSurfaceData.occlusion,
                                                                       customSurfaceData.roughness);
    float3 specularAO = GTAOMultiBounce(specularOcclusion, customSurfaceData.specular);
    float3 indirectSpecularTerm = specularLD * specularDFG * specularAO;
    return indirectDiffuseTerm + indirectSpecularTerm;
}

//BENT NORMAL TEST
half3 EnvBRDF(CustomLitData customLitData, CustomSurfaceData customSurfaceData, float envRotation, float3 positionWS,
              half3 indirectDiffuse, half3 BentNormal)
{
    half NoV = saturate(abs(dot(customLitData.N, customLitData.V)) + 1e-5);
    half3 R = reflect(-customLitData.V, customLitData.N);
    R = RotateDirection(R, envRotation);

    //SH
    float3 diffuseAO = GTAOMultiBounce(customSurfaceData.occlusion, customSurfaceData.albedo);
    float3 indirectDiffuseTerm = indirectDiffuse * customSurfaceData.albedo * diffuseAO;

    //IBL
    //The Split Sum: 1nd Stage
    half3 specularLD = GlossyEnvironmentReflection(R, positionWS, customSurfaceData.roughness,
                                                   customSurfaceData.occlusion);
    //The Split Sum: 2nd Stage
    half3 specularDFG = EnvBRDFApprox(customSurfaceData.specular, customSurfaceData.roughness, NoV);
    //AO
    float specularOcclusion = GetSpecularOcclusionFromBentAO(NoV, BentNormal, customLitData.N, customSurfaceData.occlusion,
                                                                       customSurfaceData.roughness);
    float3 specularAO = GTAOMultiBounce(specularOcclusion, customSurfaceData.specular);
    float3 indirectSpecularTerm = specularLD * specularDFG * specularAO;
    return indirectDiffuseTerm + indirectSpecularTerm;
}


// Specular, Cook-Torrance BRDF used in Clausen et al. with Diffraction pattern
// Based on [Clausen et al. 2022]
// https://github.com/MartinMisiak/Realtime-Diffraction-PBR
half3 shift_function(half NdotH, half w, half h)
{
    half theta_m = acos(NdotH); //FastACos?
    half m = h * cos(w * theta_m);
    //  srgb values
    half m_r = 42.45318742;
    half m_g = -56.98651893;
    half m_b = -159.23105974;
    // adobe rgb
    //    float m_r = 14.12228819;
    //    float m_g = -56.99255935;
    //    float m_b = -155.01640388;

    half3 shift_rgb = half3(m_r * m + 1.0, m_g * m + 1.0, m_b * m + 1.0);
    return shift_rgb;
}

half3 RainbowShift(half thetaM, half roughness)
{
    half shift = cos(acos(thetaM) * 5.0 + roughness * 50.0);  
    
    half3 rainbow = half3(
        sin(shift + 0.0) * 0.5 + 0.5,  
        sin(shift + 2.0) * 0.5 + 0.5, 
        sin(shift + 4.0) * 0.5 + 0.5   
    );
    
    return rainbow;
}


float3 cartesian2Polar(float3 cartPos)
{
    float radius = length(cartPos);
    float theta = atan2(cartPos.y, cartPos.x);
    float phi = acos(cartPos.z / radius);
    return float3(theta, phi, radius);
}


//#endif
