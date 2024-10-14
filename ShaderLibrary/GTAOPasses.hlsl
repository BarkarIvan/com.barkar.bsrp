#ifndef GTAO_PASSES
#define GTAO_PASSES

#include "Packages/com.barkar.bsrp/ShaderLibrary/GTAOFunctions.hlsl"

half4 FragGTAO(Varyings IN):SV_Target
{
    half2 uv = IN.uv;
    half depth = 0;
    
    half4 bn_ao = GTAO(uv, depth);
    half2 bentNormal = SpheremapEncodeNormal(mul((half3x3)UNITY_MATRIX_V, half3(bn_ao.rg, -bn_ao.b)));
    return half4(bentNormal,bn_ao.a, depth);
    
}

#endif
