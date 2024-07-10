#ifndef  CUSTOM_BRDF_INCLUDED
#define  CUSTOM_BRDF_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#define MIN_REFLECTIVITY 0.04
#define kDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)


struct BRDF
{
    half3 diffuse;
    half3 specular;
    half roughness;
};

struct BRDFData
{
    half3 albedo;
    half3 diffuse;
    half3 specular;
    half reflectivity;
    half perceptualRoughness;
    half roughness;
    half roughness2;
    half grazingTerm;

    // We save some light invariant BRDF terms so we don't have to recompute
    // them in the light loop. Take a look at DirectBRDF function for detailed explaination.
   // half normalizationTerm;     // roughness * 4.0 + 2.0
   // half roughness2MinusOne;    // roughness^2 - 1.0
};

half OneMinusReflectivity(half metallic)
{
    half range = 1.0 - kDielectricSpec.a;
    return range - metallic * range;
}

inline void InitializeBRDFDataDirect(half3 albedo, half metallic, half smoothness, out BRDFData outBRDFData)
{
    outBRDFData = (BRDFData)0;

    half oneMinusReflectivity = OneMinusReflectivity(metallic);
    half reflectivity = half(1.0) - oneMinusReflectivity;
    half3 brdfDiffuse = albedo * oneMinusReflectivity;
    half3 brdfSpecular = lerp(kDielectricSpec.rgb, albedo, metallic);
    
    outBRDFData.albedo = albedo;
    outBRDFData.diffuse = brdfDiffuse;
    outBRDFData.specular = brdfSpecular;
    outBRDFData.reflectivity = reflectivity;
    

    outBRDFData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
    outBRDFData.roughness           = max(PerceptualRoughnessToRoughness(outBRDFData.perceptualRoughness), HALF_MIN_SQRT);
    outBRDFData.roughness2          = max(outBRDFData.roughness * outBRDFData.roughness, HALF_MIN);
    outBRDFData.grazingTerm         = saturate(smoothness + metallic);
  //  outBRDFData.normalizationTerm   = outBRDFData.roughness * half(4.0) + half(2.0);
   // outBRDFData.roughness2MinusOne  = outBRDFData.roughness2 - half(1.0);

    // Input is expected to be non-alpha-premultiplied while ROP is set to pre-multiplied blend.
    // We use input color for specular, but (pre-)multiply the diffuse with alpha to complete the standard alpha blend equation.
    // In shader: Cs' = Cs * As, in ROP: Cs' + Cd(1-As);
    // i.e. we only alpha blend the diffuse part to background (transmittance).
    #if defined(_ALPHAPREMULTIPLY_ON)
    // TODO: would be clearer to multiply this once to accumulated diffuse lighting at end instead of the surface property.
    outBRDFData.diffuse *= alpha;
    #endif
}




BRDF GetBRDF(Surface surface)
{
    BRDF brdf;

    half oneMinusReflectivity = OneMinusReflectivity(surface.metallic);

    brdf.diffuse = surface.albedo * oneMinusReflectivity;
    brdf.specular = lerp(kDielectricSpec.x, surface.albedo, surface.metallic);
    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}

BRDF GetBRDFPremultiplyAlpha(Surface surface)
{
    BRDF brdf;

    half oneMinusReflectivity = OneMinusReflectivity(surface.metallic);

    brdf.diffuse = surface.albedo * oneMinusReflectivity;
    brdf.diffuse *= surface.alpha;
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.albedo, surface.metallic);
    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}


half SpecularStrenght(Surface surface, BRDF brdf, Light light)
{
    float3 halfDir = SafeNormalize(light.direction + surface.viewDir);
    float NoH = saturate(dot(surface.normal, halfDir));
    half NoH2 = NoH * NoH;
    half LoH = saturate(dot(light.direction, halfDir));
    half LoH2 = LoH * LoH;
    half roughtness2 = max(brdf.roughness * brdf.roughness, 1e-4);
    float roughtness2MinusOne = roughtness2 - 1.0;
    float microfacetDistrib = mad(NoH2, roughtness2MinusOne, 1.001);
    float microfasetDistrib2 = microfacetDistrib * microfacetDistrib;
    float roughtnessNormalization = mad(brdf.roughness, 4.0, 2.0);
    float specularTerm = roughtness2 / (microfasetDistrib2 * max(0.1, LoH2) * roughtnessNormalization);
    specularTerm -= HALF_MIN;
    specularTerm = clamp(specularTerm, 0.0, 100.0);
    return specularTerm;
}

half3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrenght(surface, brdf, light) * brdf.specular + brdf.diffuse;
}



//SpecOps approach
half3 EnvironmentBRDF(Surface surface, BRDF brdf, half3 specularTerm )
{
    half g = 1.0h - surface.smoothness;
    half4 t = half4(1.042h, 0.475h, 0.0182h, 0.25h);
    t *= half4(g, g, g, g);
    t += half4(0, 0, -0.0156h, 0.75h);
    half NoV = saturate(dot(surface.normal, surface.viewDir));
    half a0 = t.x * min(t.y, exp2(-9.28h * NoV)) + t.z;
    half a1 = t.w;
    half3 tempC =  saturate(lerp(a0, a1, brdf.specular ));
    return specularTerm + ((brdf.diffuse) + tempC * (specularTerm * brdf.specular));
}

///STYLIZED
half3 EnvironmentBRDF(Surface surface, BRDF brdf, half3 indirectDiffuse, half3 specularTerm, half3 radiance )
{
    half g = 1.0h - surface.smoothness;
    half4 t = half4(1.042h, 0.475h, 0.0182h, 0.25h);
    t *= half4(g, g, g, g);
    t += half4(0, 0, -0.0156h, 0.75h);
    half NoV = saturate(dot(surface.normal, surface.viewDir));
    half a0 = t.x * min(t.y, exp2(-9.28h * NoV)) + t.z;
    half a1 = t.w;
    half3 tempC =  saturate(lerp(a0, a1, brdf.specular ));
    return (specularTerm  + (indirectDiffuse * brdf.diffuse  + tempC * (specularTerm * brdf.specular) ))* radiance;
}

half3 EnvironmentBRDF(Surface surface, BRDF brdf, half3 specularTerm, half3 radiance )
{
    half g = 1.0h - surface.smoothness;
    half4 t = half4(1.042h, 0.475h, 0.0182h, 0.25h);
    t *= half4(g, g, g, g);
    t += half4(0, 0, -0.0156h, 0.75h);
    half NoV = saturate(dot(surface.normal, surface.viewDir));
    half a0 = t.x * min(t.y, exp2(-9.28h * NoV)) + t.z;
    half a1 = t.w;
    half3 tempC =  saturate(lerp(a0, a1, brdf.specular ));
    return (specularTerm  + (brdf.diffuse* radiance  + tempC * (specularTerm * brdf.specular) ));
}

#endif