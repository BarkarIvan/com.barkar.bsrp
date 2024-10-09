Shader "BSRP/StandartLit_PPLL"
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

        [Toggle(_USEALPHACLIP)] _UseAlphaClip ("Use Alpha Clip", Float) = 0
        _AlphaClip ("ClipAlha", Range(0,1)) = 0
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 2
       
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="BSRP"
        }

        Pass
        {
            Tags
            {
                "LightMode" = "BSRPPPLL"
            }
            Name "Create Linked List"

            ZTest LEqual
            ColorMask 0
            Cull [_Cull]
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex StylizedTransparentVertex
            #pragma fragment StylizedTransparentFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ADDITIONALMAP
            #pragma shader_feature_local _RIM
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _BRUSHTEX
            #pragma shader_feature_local _USEALPHACLIP

            #pragma multi_compile_fog

            #pragma prefer_hlslcc gles
            #pragma target 5.0
            // #pragma enable_d3d11_debug_symbols
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Surface.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/CustomBRDF.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"

            #include "Packages/com.barkar.bsrp/ShaderLibrary/OITUtils.hlsl"
            //to inputs include

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AdditionalMap);
            SAMPLER(sampler_AdditionalMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_BrushTexture);
            SAMPLER(sampler_BrushTexture);


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
                half3 SH : TEXCOORD6;
                half4 color : COLOR;
                float4 screenPos : TEXCOORD7;
            };

            Varyings StylizedTransparentVertex(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionWS.xyz = positionInputs.positionWS;
                OUT.positionCS = positionInputs.positionCS;
                OUT.normalWS = normalInputs.normalWS;

                #if defined(_NORMALMAP)
                half sign = IN.tangentOS.w;
                half3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                half3 bitangentWS = cross(normalInputs.normalWS.xyz, normalInputs.tangentWS.xyz) * sign;
                OUT.tangentWS = half3(tangentWS);
                OUT.bitangentWS = half3(bitangentWS);
                OUT.SH = SHEvalLinearL2(OUT.normalWS, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC);
                #else
                OUT.SH = SampleSH(OUT.normalWS);
                #endif

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.addUv = TRANSFORM_TEX(IN.uv, _AdditionalMap);
                OUT.color = IN.color;
                float4 ndc = OUT.positionCS * 0.5f;
                OUT.screenPos.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                OUT.screenPos.zw = OUT.positionCS.zw;
                return OUT;
            }

            [earlydepthstencil]
            half4 StylizedTransparentFragment(Varyings IN) : SV_Target
            {
               
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                albedo *= _BaseColor;
                albedo *= IN.color;
                albedo *= _Brightness;

                half3 indirectDiffuse = IN.SH;
                half3 normalWS = SafeNormalize(IN.normalWS);

                CustomSurfaceData surfaceData;
                surfaceData.metallic = _Metallic;
                surfaceData.roughness = _Roughness;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.occlusion = 1.0;
                surfaceData.normalTS = SafeNormalize(IN.normalWS);
               

                CustomLitData litData;
                litData.T  = IN.tangentWS;
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

                //normals
                #if defined (_NORMALMAP)
                half3 normalTS;
                normalTS.xy = additionalMaps.rg * 2.0 - 1.0;
                normalTS.xy *= _NormalMapScale;
                normalTS.z = sqrt(1 - (normalTS.x * normalTS.x) - (normalTS.y * normalTS.y));
                half3x3 tangentToWorld = half3x3(IN.tangentWS.xyz, IN.bitangentWS.xyz, IN.normalWS.xyz);
                normalWS = SafeNormalize(mul(normalTS, tangentToWorld));
                indirectDiffuse += SHEvalLinearL0L1(IN.normalWS, unity_SHAr, unity_SHAg, unity_SHAb);
                #endif
                #endif
              
                surfaceData.albedo =  lerp(surfaceData.albedo, float3(0.0,0.0,0.0), surfaceData.metallic);
                surfaceData.specular =  lerp(kDielectricSpec.rgb, albedo, surfaceData.metallic);
                indirectDiffuse = LinearToSRGB(indirectDiffuse);
                litData.N = normalWS;
                
                //alpha
               /// #if defined (_USEALPHACLIP)
               // surface.alpha = step(_AlphaClip, surface.alpha);
               // #endif
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light light = GetMainLight(shadowCoord, IN.positionWS);
                half3 directpbr = StandardBRDF(litData, surfaceData, light.direction, light.color, light.shadowAttenuation );
                half3 envPbr = EnvBRDF(litData, surfaceData,0, IN.positionWS, indirectDiffuse);
                
                half4 result = half4(directpbr + envPbr, albedo.a);
                
                //Emission
                half3 emissionColor = _EmissionColor.rgb;

                #if defined(_EMISSION)
                half3 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb;
                emissionColor *= emissionMap;
                #endif
                result.rgb += emissionColor;

                //LOD
                //  #ifdef LOD_FADE_CROSSFADE
                //  LODFadeCrossFade(IN.positionCS);
                // #endif

                //FOG
                #if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
                result.rgb = CalculateFog(result, IN.positionWS);
                #endif

                // return result;

                uint2 pos = uint2(IN.positionCS.xy);
                uint depth = (uint)(Linear01Depth(IN.positionCS.z, _ZBufferParams) * (pow(2, 24) - 1));
                depth = depth << 8UL;
             

                //TODO to oit utils
                //oit
                uint fragCount = _FragmentLinksBuffer.IncrementCounter();
                //buffer adress
                uint startOffsetAddress = 4 * ((_RenderSizeParams.x * (pos.y)) + (pos.x));

                uint startOffsetOld;
                _StartOffsetBuffer.InterlockedExchange(startOffsetAddress, fragCount, startOffsetOld);


                Fragment fragment;
                fragment.color = PackRGBA(float4(result.rgb, 1 - result.a));
                fragment.depth = depth;
                fragment.next = startOffsetOld;
                _FragmentLinksBuffer[fragCount] = fragment;
                return 0;
            }
            ENDHLSL
        }

        //to shadowcaster include

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
                half3 normalWS = TransformObjectToWorldDir(input.normalOS.xyz);

                //apply bias
                half invNdotL = 1.0 - saturate(dot(MainLightDirectionaAndMask.xyz, normalWS.xyz));
                half scale = invNdotL * MainLightShadowsData.y;

                positionWS = MainLightDirectionaAndMask.xyz * MainLightShadowsData.yyy + positionWS.xyz;
                positionWS = normalWS * scale.xxx + positionWS;
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
                //dItHERED?
                return 0;
            }
            ENDHLSL
        }
    }

    CustomEditor "Barkar.BSRP.Editor.ShaderEditor.BSRPStandartLitPPLLShaderEditor"
}