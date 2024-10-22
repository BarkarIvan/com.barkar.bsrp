#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
#include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"



Varyings MetaPassVertex(Attributes input)
{
    Varyings output;
    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

half4 MetaPassFragment(Varyings input) : SV_TARGET
{
    return 0;
}


#endif
