#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface
{
    half3 normal;
    half3 viewDir;
    half3 albedo;
    half alpha;
    half metallic;
    half smoothness;
};

#endif