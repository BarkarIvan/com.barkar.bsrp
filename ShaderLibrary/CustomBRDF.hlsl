#ifndef  CUSTOM_BRDF_INCLUDED
#define  CUSTOM_BRDF_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#define MIN_REFLECTIVITY 0.04

struct BRDF
{
    half3 diffuse;
    half3 specular;
    half roughness;
};

half OneMinusReflectivity(half metallic)
{
    half range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}

BRDF GetBRDF(Surface surface)
{
    BRDF brdf;

    half oneMinusReflectivity = OneMinusReflectivity(surface.metallic);

    brdf.diffuse = surface.color * oneMinusReflectivity;
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}

BRDF GetBRDFPremultiplyAlpha(Surface surface)
{
    BRDF brdf;

    half oneMinusReflectivity = OneMinusReflectivity(surface.metallic);

    brdf.diffuse = surface.color * oneMinusReflectivity;
    brdf.diffuse *= surface.alpha;
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
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
half3 EnvironmentBRDF(Surface surface, BRDF brdf, half3 indirectDiffuse, half3 specularTerm )
{
    half g = 1.0h - surface.smoothness;
    half4 t = half4(1.042h, 0.475h, 0.0182h, 0.25h);
    t *= half4(g, g, g, g);
    t += half4(0, 0, -0.0156h, 0.75h);
    half NoV = saturate(dot(surface.normal, surface.viewDir));
    half a0 = t.x * min(t.y, exp2(-9.28h * NoV)) + t.z;
    half a1 = t.w;
    half3 tempC =  saturate(lerp(a0, a1, brdf.specular ));
    return specularTerm + ((indirectDiffuse * brdf.diffuse) + tempC * (specularTerm * brdf.specular));
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
    return (specularTerm  + (indirectDiffuse * brdf.diffuse  + tempC * (specularTerm * brdf.specular) )) * radiance;
}

#endif