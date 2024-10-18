#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED

#include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
#include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"


TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_AdditionalMap);
SAMPLER(sampler_AdditionalMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half4 _BaseMap_ST;
    half4 _AdditionalMap_ST;
    half3 _EmissionColor;
    half _Brightness;
    half _Metallic;
    half _Roughness;
    half _AlphaClip;
    half _NormalMapScale;
    half _DiffractionWidth;
    half _DiffractionHeight;
CBUFFER_END

#endif
