#ifndef CUSTOM_GBUFFER_PASS_INCLUDED
#define CUSTOM_GBUFFER_PASS_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
struct GBuffer
{
    half4 GBUFFER0 : SV_Target0;
    half4 GBUFFER1 : SV_Target1;
    half4 GBUFFER3 : SV_Target2;
};

Varyings GBufferVertex(Attributes IN)
{
    Varyings OUT;
    VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

    OUT.positionWS.xyz = positionInputs.positionWS;
    OUT.positionCS = positionInputs.positionCS;
    OUT.normalWS = normalInputs.normalWS;

    //OUT.SH = SampleSH(OUT.normalWS);
    half sign = IN.tangentOS.w;
    half3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
    half3 bitangentWS = cross(normalInputs.normalWS.xyz, normalInputs.tangentWS.xyz) * sign;
    OUT.tangentWS = half3(tangentWS);
    OUT.bitangentWS = half3(bitangentWS);

 //  #if defined(_NORMALMAP)
    OUT.SH = SHEvalLinearL2(OUT.normalWS, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC);
  // #endif

    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
    OUT.color = IN.color;
    OUT.shadowCoord = GetShadowCoord(positionInputs);
    OUT.screenPos = positionInputs.positionNDC;
    OUT.lightmapUV = IN.lightmapUV;

    return OUT;
}

GBuffer GBufferFragment(Varyings IN)
{
    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
    albedo *= _BaseColor;
    //albedo *= IN.color;
    albedo *= _Brightness;


    CustomSurfaceData surfaceData;
    surfaceData.metallic = _Metallic;
    surfaceData.roughness = _Roughness;
    surfaceData.albedo = albedo.rgb;
    surfaceData.alpha = albedo.a;
   // surfaceData.occlusion = 1.0;
    surfaceData.normalTS = SafeNormalize(IN.normalWS);

    half3 normalWS = IN.normalWS;
    CustomLitData litData;

    litData.T = IN.tangentWS;
    litData.V = SafeNormalize(_WorldSpaceCameraPos - IN.positionWS);
    litData.positionWS = IN.positionWS;
    litData.B = IN.bitangentWS;

    
    //additional map
    #if defined (_ADDITIONALMAP)
                                           half4 additionalMaps = SAMPLE_TEXTURE2D(_AdditionalMap, sampler_AdditionalMap, IN.uv);
                                           half smoothnessMask = additionalMaps.b;
                                           half metallicMask = additionalMaps.a;
                                           surfaceData.metallic = metallicMask;
                                           surfaceData.roughness = smoothnessMask;
                           
                                           //normal map
    #if defined (_NORMALMAP)
    half3 normalTS;
    normalTS.xy = additionalMaps.rg * 2.0 - 1.0;
    normalTS.xy *= _NormalMapScale;
    normalTS.z = sqrt(1 - (normalTS.x * normalTS.x) - (normalTS.y * normalTS.y));
    half3x3 tangentToWorld = half3x3(IN.tangentWS.xyz, IN.bitangentWS.xyz, IN.normalWS.xyz);
    normalWS = SafeNormalize(mul(normalTS, tangentToWorld));
   // indirectDiffuse += SHEvalLinearL0L1(IN.normalWS, unity_SHAr, unity_SHAg, unity_SHAb);
    #endif
    #endif
    
    half4 GTAO_BN = SAMPLE_TEXTURE2D(_GTAOBentNormalTexture, sampler_GTAOBentNormalTexture, IN.screenPos.xy/IN.screenPos.w);
    surfaceData.occlusion = GTAO_BN.a;

    surfaceData.albedo = lerp(surfaceData.albedo, float3(0.0, 0.0, 0.0), surfaceData.metallic);
    surfaceData.specular = lerp(kDielectricSpec.rgb, albedo.rgb, surfaceData.metallic);
    litData.N =(normalWS);
    
    half3 indirectDiffuse = SAMPLE_GI(IN.lightmapUV, IN.SH, litData.N);


    // TODO alpha
    //  #if defined (_USEALPHACLIP)
    // surface.alpha = step(_AlphaClip, surface.alpha);
    //  #endif
    
    half3 bn =mul(UNITY_MATRIX_I_V,GTAO_BN.rgb);
    half3 envPbr = EnvBRDF(litData, surfaceData, 0, IN.positionWS, indirectDiffuse,bn);


    //experimental diffraction
    half3 H = normalize(litData.V + MainLightDirectionaAndMask.xyz);
    half NoH = saturate(dot(litData.N, H));
    half3 diffractionShift = shift_function(NoH, _DiffractionWidth, _DiffractionHeight);
    diffractionShift = lerp(1.0, diffractionShift, surfaceData.metallic);
    diffractionShift = lerp(1.0, diffractionShift, surfaceData.metallic);


    //Emission
    half3 emissionColor = _EmissionColor.rgb;
    #if defined(_EMISSION)
     half3 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb;
     emissionColor *= emissionMap;
    #endif

    GBuffer gbo;
    gbo.GBUFFER0 = half4(albedo.rgb * diffractionShift, surfaceData.roughness);
    gbo.GBUFFER1 = half4(indirectDiffuse, surfaceData.metallic); //AO
    gbo.GBUFFER3 = float4(envPbr + emissionColor, 1.0);

    return gbo;
}

#endif
