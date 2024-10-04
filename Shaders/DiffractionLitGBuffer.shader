Shader "BSRP/DiffractionLitGBUFFER"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white"{}
        _BaseColor ("Color", Color) = (1,1,1,1)

        _AdditionalMap ("Additional Map", 2D) = "white"{} //nml.xy, rough, metallic
        [Toggle(_NORMALMAP)] _UsingNormalMap("Using Normal Map", Float) = 0
        _NormalMapScale("Normal Map Scale", Range(0,3)) = 1

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Roughness( "Roughness", Range(0,1)) = 0.0

        [Toggle(_RIM)] _UsingRim("Using RIM", Float) = 0
        _RimThreshold("Rim Threshold", Range(0,1)) = 0
        _RimSmooth("Rim Smooth", Range(0,1)) = 1
        _RimColor("Rim Color", Color) = (1,1,1,1)


        [HDR] _EmissionColor ("Emission", Color) = (1,1,1,1)
        _EmissionMap ("EmissionMap", 2D) = "black"{}

        _Brightness("Brightness", Range(0,2)) = 1

        // Clausen BRDF
        //temp
        [Toggle(_DIRECT_LIGHT_BRDF_DIFFRACTION)] _DirectDiffractionToggle("Direct diffraction", Float) = 1
        [Toggle(_DIRECT_LIGHT_BRDF_DIFFRACTION_PATTERN)] _DiffractionPatternToggle("Diffraction Pattern", Float) = 1
        [Toggle(_DIFFRACTION_PATTERN_OPEN_SIMPLEX_2)] _OpenSimpex2Toggle ("Open simplex2", Float) = 1

        _DiffractionWidth("Diffraction Width", Range(0.0, 7.0)) = 2.3394
        _DiffractionHeight("Diffraction Height", Range(0.0, 0.0045)) = 0.0015
        [ToggleOff] _DiffractionPatternToggle("Diffraction Pattern", Float) = 1.0
        _NoiseImplementationDiffraction("Noise Implementation", Float) = 1.0
        _DiffractionStereoSpecularity("Diffraction Stereoscopic Specularity", Range(0.0, 1.0)) = 0.5
        _DiffractionZW_Scale("Spatio-Temporal Pattern-Shift factor", Range(0.0, 50.0)) = 7
        _DiffractionUV_ScaleX("Diffraction Scaling factor for UVs", Float) = 1.0
        _DiffractionUV_ScaleY("Diffraction Scaling factor for UVs", Float) = 1.0
        
        // Used only in ShaderGUI. Not sent to shader
        _DiffractionVar_R("var_r", Float)       = 0.00429577
        _DiffractionVar_G("var_g", Float)       = 0.00396111
        _DiffractionVar_B("var_b", Float)       = 0.00326149
        _DiffractionCovar_RG("covar_rg", Float) = 0.00377288
        _DiffractionCovar_RB("covar_rb", Float) = 0.00305712
        _DiffractionCovar_GB("covar_gb", Float) = 0.00316949
        _DiffractionCovInit_Row_1("covar_init_r1" , Vector) = (1, 1, 1, 0)
        _DiffractionCovInit_Row_2("covar_init_r2" , Vector) = (1, 1, 1, 0)
        _DiffractionCovInit_Row_3("covar_init_r3" , Vector) = (1, 1, 1, 0)
        // Clausen BRDF

        [Toggle(_USEALPHACLIP)] _UseAlphaClip ("Use Alpha Clip", Float) = 0
        _AlphaClip ("ClipAlha", Range(0,1)) = 0


        [Space(40)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _Blend1 ("Blend mode", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _Blend2 ("Blend mode", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="BSRP" "Queue"="Geometry"
        }

        Cull [_Cull]
        Blend [_Blend1] [_Blend2]
        ZWrite [_ZWrite]

        Pass
        {
            Tags
            {
                "LightMode" = "BSRPGBuffer"
            }

            HLSLPROGRAM
            #pragma vertex BSRPStylizedVertex
            #pragma fragment BSRPStylizedFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ADDITIONALMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _USEALPHACLIP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature_local _DIRECT_LIGHT_BRDF_DIFFRACTION
            #pragma shader_feature_local _DIRECT_LIGHT_BRDF_DIFFRACTION_PATTERN
            #pragma shader_feature_local _DIFFRACTION_PATTERN_OPEN_SIMPLEX_2

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"

            //to inputs include

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
                half _NoiseImplementationDiffraction;
                //simpex noise
            CBUFFER_END


            struct Attributes
            {
                float3 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 addUv : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : NORMAL;
                half3 tangentWS : TEXCOORD3;
                half3 bitangentWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                half3 SH : TEXCOORD6;
                half4 color : COLOR;
            };

            //TODO to gbuffer hlsl
            struct GBuffer
            {
                half4 GBUFFER0 : SV_Target0;
                half4 GBUFFER1 : SV_Target1;
                half4 GBUFFER2 : SV_Target2;
                half4 GBUFFER3 : SV_Target3;
            };


            Varyings BSRPStylizedVertex(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionWS.xyz = positionInputs.positionWS;
                OUT.positionCS = positionInputs.positionCS;
                OUT.normalWS = normalInputs.normalWS;

                OUT.SH = SampleSH(OUT.normalWS);
                half sign = IN.tangentOS.w;
                half3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                half3 bitangentWS = cross(normalInputs.normalWS.xyz, normalInputs.tangentWS.xyz) * sign;
                OUT.tangentWS = half3(tangentWS);
                OUT.bitangentWS = half3(bitangentWS);

                #if defined(_NORMALMAP)
                OUT.SH = SHEvalLinearL2(OUT.normalWS, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC);
                #endif

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.addUv = TRANSFORM_TEX(IN.uv, _AdditionalMap);
                OUT.color = IN.color;
                OUT.shadowCoord = GetShadowCoord(positionInputs);

                return OUT;
            }

            GBuffer BSRPStylizedFragment(Varyings IN): SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                albedo *= _BaseColor;
                albedo *= IN.color;
                albedo *= _Brightness;

                half3 indirectDiffuse = IN.SH;

                CustomSurfaceData surfaceData;
                surfaceData.metallic = _Metallic;
                surfaceData.roughness = _Roughness;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.occlusion = 1.0;
                surfaceData.normalTS = SafeNormalize(IN.normalWS);

                half3 normalWS = IN.normalWS;
                CustomLitData litData;

                litData.T = IN.tangentWS;
                litData.V = SafeNormalize(_WorldSpaceCameraPos - IN.positionWS);
                litData.positionWS = IN.positionWS;
                litData.B = IN.bitangentWS;

                //additional map
                #if defined (_ADDITIONALMAP)
                half4 additionalMaps = SAMPLE_TEXTURE2D(_AdditionalMap, sampler_AdditionalMap, IN.addUv);
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
                indirectDiffuse += SHEvalLinearL0L1(IN.normalWS, unity_SHAr, unity_SHAg, unity_SHAb);
                indirectDiffuse = LinearToSRGB(indirectDiffuse);
                #endif
                #endif


                surfaceData.albedo = lerp(surfaceData.albedo, float3(0.0, 0.0, 0.0), surfaceData.metallic);
                surfaceData.specular = lerp(kDielectricSpec.rgb, albedo, surfaceData.metallic);
                litData.N = normalWS;

                //alpha
                //  #if defined (_USEALPHACLIP)
                // surface.alpha = step(_AlphaClip, surface.alpha);
                //  #endif
                Light l = GetMainLight(IN.positionWS);
half3 L = l.direction;
                surfaceData.uvs = IN.uv;
                half3 envPbr = EnvBRDF(litData, surfaceData, 0, IN.positionWS, indirectDiffuse);
                half3 brdf = StandardBRDFDiffraction(litData, surfaceData, L, half3(1,1,1), 1.0, _DiffractionWidth, _DiffractionHeight);
                //Emission
                half3 emissionColor = _EmissionColor.rgb;

                #if defined(_EMISSION)
                half3 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb;
                emissionColor *= emissionMap;
                #endif

                GBuffer gbo;
                gbo.GBUFFER0 = half4(saturate(albedo * brdf), surfaceData.roughness);
                gbo.GBUFFER1 = half4(half3(1.0, 1.0, 1.0), surfaceData.metallic); //AO
                gbo.GBUFFER2 = half4((SpheremapEncodeNormal(mul(unity_MatrixV, litData.N))), 0.0, 0.0);
                gbo.GBUFFER3 = float4(envPbr + emissionColor, 1.0);

                return gbo;
            }
            ENDHLSL
        }

        //to shadowcaster hlsl

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster"
            }

            ColorMask 0
            ZTest LEqual

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                half3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);

                //apply bias
                half invNdotL = 1.0 - saturate(dot(MainLightDirectionaAndMask.xyz, normalWS.xyz));
                half scale = invNdotL * MainLightShadowsData.y;

                positionWS = MainLightDirectionaAndMask.xyz * MainLightShadowsData.yyy + positionWS.xyz;
                positionWS = +positionWS + normalWS * scale.xxx;
                //
                float4 positionCS = TransformWorldToHClip(positionWS);

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

      CustomEditor "Barkar.BSRP.Editor.ShaderEditor.BSRPDiffractionLitShaderEditor"
}