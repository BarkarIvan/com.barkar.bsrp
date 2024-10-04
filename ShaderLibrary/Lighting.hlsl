#ifndef CUSTOM_LIGHTING
#define CUSTOM_LIGHTING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.barkar.bsrp/ShaderLibrary/CustomLitData.hlsl"
#include "Packages/com.barkar.bsrp/ShaderLibrary/RealTimeLight.hlsl"
#include "Packages/com.barkar.bsrp/ShaderLibrary/OpenSimplex.hlsl"

#define MIN_REFLECTIVITY 0.04
#define kDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04)

real3 DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    real alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);

    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
}


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

    //#if defined(_REFLECTION_PROBE_BLENDING)
    //irradiance = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness, normalizedScreenSpaceUV);
    // #else
    //#ifdef _REFLECTION_PROBE_BOX_PROJECTION
    //reflectVector = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    //#endif // _REFLECTION_PROBE_BOX_PROJECTION
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encodedIrradiance =
        half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));

    irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    //#endif // _REFLECTION_PROBE_BLENDING
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

#define _DiffractionVar_R  0
#define _DiffractionVar_G 0   
#define _DiffractionVar_B 0
#define _DiffractionCovar_RG 0
#define _DiffractionCovar_RB 0
#define _DiffractionCovar_GB 0
#define _DiffractionCovInit_Row_1 float4(1, 1, 1, 0)
#define _DiffractionCovInit_Row_2 float4(1, 1, 1, 0)
#define _DiffractionCovInit_Row_3 float4(1, 1, 1, 0)

// Specular, Cook-Torrance BRDF used in Clausen et al. with Diffraction pattern
// Based on [Clausen et al. 2022]
// https://github.com/MartinMisiak/Realtime-Diffraction-PBR
float3 shift_function(float NdotH, float w, float h)
{
    float theta_m = acos(NdotH); //FastACos?
    float m = h * cos(w * theta_m);
    //  srgb values
    float m_r = 42.45318742;
    float m_g = -56.98651893;
    float m_b = -159.23105974;
    // adobe rgb
    //    float m_r = 14.12228819;
    //    float m_g = -56.99255935;
    //    float m_b = -155.01640388;

    float3 shift_rgb = float3(m_r * m + 1.0, m_g * m + 1.0, m_b * m + 1.0);
    return shift_rgb;
}

float3 cartesian2Polar(float3 cartPos)
{
    float radius = length(cartPos);
    float theta = atan2(cartPos.y, cartPos.x);
    float phi = acos(cartPos.z / radius);
    return float3(theta, phi, radius);
}

float cov_model(float NdotH, float w)
{
    float theta_m = acos(NdotH);
    float slope = cos(w * theta_m);
    slope = (slope + 1.0) * 0.5;

    return slope;
}

// Also performs run-time filtering via multisampling(4x)
// For stereoscopic rendering, halfVecWs should be computed using the cyclopean eye position
float3 sampleDiffractionPattern(float2 uvs, float3 halfVecWs, float3 normalWs)
{
    const float UV_TO_SPECKLE_FACTOR = 0.5;
    // How large is a speckle in the used noise function (speckle size = 1/2 period)
    const float M = _DiffractionZW_Scale;

    // If more than 1 speckle fits into the dimension of a screen pixel, the speckle intensity is reduced linearly to their density.
    // This value describes how many additional speckles can be present in the dimension of a screen pixel, before reaching 0 intensity.
    // We use a value of 1 here, as we can fit a total of 2 speckles per pixel dimension without introducing aliasing (due to 4x multisampling)
    // however due to the low contrast of the speckle pattern, we could go up to a value of 2 without any noticeable aliasing
    const float AMPLITUDE_REDUCTION_FALLOFF = 1.45; // MAX_SPECKLES_PER_PIXEL = (AMPLITUDE_REDUCTION_FACTOR + 1)^2

    // Compute screen-space derivative of uvs -> How much do uvs change from one pixel to the next ?
    float2 dx_vtc = ddx(uvs);
    float2 dy_vtc = ddy(uvs);
    float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));
    float delta_uv = sqrt(delta_max_sqr);
    // How many speckles fit along one dimension of a screen pixel
    float sqrt_speckles_per_pixel = delta_uv / UV_TO_SPECKLE_FACTOR;
    // 0 to 1 speckles_per_pixel -> no modulation. 1 to (1 + AMP_RED_FALLOFF) -> linear reduction
    float amplitude_modulation = 1.0 - min((max(sqrt_speckles_per_pixel - 1, 0) / AMPLITUDE_REDUCTION_FALLOFF), 1.0);


    // Determine the last two dimensions of the 4D lookup via polar coordinate deltas
    float3 h_polar = cartesian2Polar(halfVecWs);
    float3 n_polar = cartesian2Polar(normalWs);
    float h_a = h_polar.x - n_polar.x;
    float h_p = h_polar.y - n_polar.y;
    h_a *= M;
    h_p *= M;

    float3 filtered_noise = float3(0, 0, 0);
    if (sqrt_speckles_per_pixel > 1.0 && (sqrt_speckles_per_pixel <= (1 + AMPLITUDE_REDUCTION_FALLOFF)))
    // 4x multi-sampling
    {
        for (int i = 0; i < 4; i++)
        {
            float2 sample_location = uvs + MASK_2X2_GRID[i] * delta_uv;
            float3 sampled_noise = float3(snoise(float4(sample_location, float2(h_a, h_p))),
                                          snoise(float4(sample_location + 43, float2(h_a, h_p))),
                                          snoise(float4(sample_location - 17, float2(h_a, h_p))));

            filtered_noise += sampled_noise;
        }
        filtered_noise /= 4.0;
    }
    else if (sqrt_speckles_per_pixel <= 1) // no multi-sampling
    {
        filtered_noise = float3(snoise(float4(uvs, float2(h_a, h_p))),
                                snoise(float4(uvs + 43, float2(h_a, h_p))),
                                snoise(float4(uvs - 17, float2(h_a, h_p))));
    }

    // Modulate noise amplitude towards 0, to converge against mean
    return amplitude_modulation * filtered_noise;
}


half3 StandardBRDFDiffraction(CustomLitData customLitData, CustomSurfaceData customSurfaceData, half3 L,
                              half3 lightColor, float shadow, float diffWidth, float diffHeight)
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

    float3 shift = shift_function(NoH, diffWidth, diffHeight);
    //shift = float3(0.40367881, 0.3801785, 0.34816286);
    float3 rnd_number = 0;

    // #if defined _DIRECT_LIGHT_BRDF_DIFFRACTION_PATTERN
    ///////////////////////////////////
    float cov_model_factor = cov_model(NoL, diffWidth);
    // Cholesky-decomposed cov_init matrix
    float3x3 cov_init_decomp = {
        _DiffractionCovInit_Row_1.x, _DiffractionCovInit_Row_1.y, _DiffractionCovInit_Row_1.z,
        _DiffractionCovInit_Row_2.x, _DiffractionCovInit_Row_2.y, _DiffractionCovInit_Row_2.z,
        _DiffractionCovInit_Row_3.x, _DiffractionCovInit_Row_3.y, _DiffractionCovInit_Row_3.z
    };

    cov_init_decomp *= sqrt(cov_model_factor);

    float2 diffraction_uvs = customSurfaceData.uvs * float2(_DiffractionUV_ScaleX, _DiffractionUV_ScaleY);
    rnd_number = sampleDiffractionPattern(diffraction_uvs, H, customLitData.N);

    rnd_number /= 0.15;
    // In case of OpenSimplex(version 1) implementation, this is the normalization factor. TODO: Is it also the normalization for OpenSimplex2 or Gustavsons simplex ?
    rnd_number = mul(cov_init_decomp, rnd_number);


    // EXPERIMENTAL: Heavy, physically-not-based speckle-approximation
    //const float speckle_cutoff = 0.32;
    //const float speckle_mult   = 10;
    //if (rnd_number.x >= speckle_cutoff)
    //    rnd_number += speckle_mult * (rnd_number.x - speckle_cutoff);
    //////////////////////////////////////////////////////////////////////


    return shift + rnd_number * (diffuseTerm + specularTerm) * radiance;
}


#endif
