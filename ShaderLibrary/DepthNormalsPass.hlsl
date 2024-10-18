#ifndef CUSTOM_DEPTH_NORMALS_PASS_INCLUDED
#define CUSTOM_DEPTH_NORMALS_PASS_INCLUDED


Varyings DepthNormalsVertex(Attributes IN)
{
    Varyings OUT;
    VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

    OUT.positionWS.xyz = positionInputs.positionWS;
    OUT.positionCS = positionInputs.positionCS;
    OUT.normalWS = normalInputs.normalWS;

    half sign = IN.tangentOS.w;
    half3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
    half3 bitangentWS = cross(normalInputs.normalWS.xyz, normalInputs.tangentWS.xyz) * sign;
    OUT.tangentWS = half3(tangentWS);
    OUT.bitangentWS = half3(bitangentWS);

    OUT.addUv = TRANSFORM_TEX(IN.uv, _AdditionalMap);

    return OUT;
}

half4 DepthNormalsFragment(Varyings IN): SV_Target
{
    half3 normalWS = IN.normalWS;
    #if defined (_ADDITIONALMAP)
    #if defined (_NORMALMAP)
    half4 additionalMaps = SAMPLE_TEXTURE2D(_AdditionalMap, sampler_AdditionalMap, IN.addUv);
    half3 normalTS;
    normalTS.xy = additionalMaps.rg * 2.0 - 1.0;
    normalTS.xy *= _NormalMapScale;
    normalTS.z = sqrt(1 - (normalTS.x * normalTS.x) - (normalTS.y * normalTS.y));
    half3x3 tangentToWorld = half3x3(IN.tangentWS.xyz, IN.bitangentWS.xyz, IN.normalWS.xyz);
    normalWS = SafeNormalize(mul(normalTS, tangentToWorld));
     #endif
#endif
    return half4(SpheremapEncodeNormal(mul(unity_MatrixV, normalWS)).xy, 0.0, 0.0);
}

#endif
