Shader "BSRP/StylizedLit_Transparent"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white"{}
        _BaseColor ("Color", Color) = (1,1,1,1)

        _AdditionalMap ("Additional Map", 2D) = "white"{} //nml.xy, rough, metallic
        [Toggle(_NORMALMAP)] _UsingNormalMap("Using Normal Map", Float) = 0
        _NormalMapScale("Normal Map Scale", Range(0,3)) = 1

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness( "Smoothness", Range(0,1)) = 0.0

        [Toggle(_RIM)] _UsingRim("Using RIM", Float) = 0
        _RimThreshold("Rim Threshold", Range(0,1)) = 0
        _RimSmooth("Rim Smooth", Range(0,1)) = 1
        _RimColor("Rim Color", Color) = (1,1,1,1)


        [HDR] _EmissionColor ("Emission", Color) = (1,1,1,1)
        _EmissionMap ("EmissionMap", 2D) = "black"{}

        _Brightness("Brightness", Range(0,2)) = 1

        [Toggle(_USEALPHACLIP)] _UseAlphaClip ("Use Alpha Clip", Float) = 0
        _AlphaClip ("ClipAlha", Range(0,1)) = 0

        [Space(40)]
        _MediumThreshold ("Medium Threshold", Range(0,1)) = 0.5
        _MediumSmooth ("Medium Smooth", Range(0,0.5)) = 0.25
        _MediumColor("MediumColor", Color) = (1,1,1,1)

        _ShadowThreshold("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSmooth("Shadow Smooth", Range(0,0.5)) = 0.25
        _ShadowColor("ShadowColor", Color) = (1,1,1,1)

        _ReflectThreshold("Reflection Threshold", Range(0,1)) = 0.5
        _ReflectSmooth("Reflection Smooth", Range(0,0.5)) = 0.25
        _ReflectColor("Reflection Color", Color) = (1,1,1,1)

        [Toggle(_BRUSHTEX)] _UseBrush ("Use Brush Texture", Float) = 0
        _BrushTexture("Brushtexture", 2D) = "white"{}
        _MedBrushStrength("Brush strength on Medium", Range(0,1)) = 0
        _ShadowBrushStrength("Brush strength on Shadows", Range(0,1)) = 0
        _ReflectBrushStrength("Brush strength on Reflection", Range(0,1)) = 0
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
                "LightMode" = "BSRPTransparent"
            }
            Name "Create Linked List"

            ZTest LEqual
            ZWrite Off
            ColorMask 0
            Cull Off

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
                half _Smoothness;
                half _AlphaClip;
                half _NormalMapScale;

                half3 _RimColor;
                half _RimSmooth;

                half3 _MediumColor;
                half _MediumThreshold;

                half3 _ShadowColor;
                half _ShadowThreshold;

                half3 _ReflectColor;
                half _ReflectThreshold;

                half _MediumSmooth;
                half _ShadowSmooth;
                half _ReflectSmooth;
                half _RimThreshold;

                half4 _BrushTexture_ST;
                half _MedBrushStrength;
                half _ShadowBrushStrength;
                half _ReflectBrushStrength;
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
                #if defined(_NORMALMAP)
                half3 tangentWS : TEXCOORD3;
                half3 bitangentWS : TEXCOORD4;
                #endif
                half3 SH : TEXCOORD6;
                half4 color : COLOR;
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

                return OUT;
            }

            [earlydepthstencil]
            half4 StylizedTransparentFragment(Varyings IN) : SV_Target
            {
                half4 result;
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                albedo *= _BaseColor;
                albedo *= IN.color;
                albedo *= _Brightness;

                Surface surface;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normal = SafeNormalize(IN.normalWS.xyz);
                surface.albedo = albedo.rgb;
                surface.alpha = albedo.a;
                surface.viewDir = SafeNormalize(_WorldSpaceCameraPos - IN.positionWS);

                half3 indirectDiffuse = IN.SH;

                //additional map
                #if defined (_ADDITIONALMAP)
                half4 additionalMaps = SAMPLE_TEXTURE2D(_AdditionalMap, sampler_AdditionalMap, IN.addUv);
                half smoothnessMask = additionalMaps.b;
                half metallicMask = additionalMaps.a;
                surface.metallic = metallicMask;
                surface.smoothness = smoothnessMask;

                //normals
                #if defined (_NORMALMAP)
                half3 normalTS;
                normalTS.xy = additionalMaps.rg * 2.0 - 1.0;
                normalTS.z = sqrt(1 - (normalTS.x * normalTS.x) - (normalTS.y * normalTS.y));
                normalTS.xy *= _NormalMapScale;
                half3x3 tangentToWorld = half3x3(IN.tangentWS.xyz, IN.bitangentWS.xyz, IN.normalWS.xyz);
                surface.normal = SafeNormalize(mul(normalTS, tangentToWorld));
                indirectDiffuse += SHEvalLinearL0L1(IN.normalWS, unity_SHAr, unity_SHAg, unity_SHAb);
                #endif

                #endif

                //alpha
                #if defined (_USEALPHACLIP)
                surface.alpha = step(_AlphaClip, surface.alpha);
                #endif

                Light light = GetMainLight(IN.positionWS);
                half NoL = dot(surface.normal, light.direction);

                Ramp ramp;
                ramp.MediumThreshold = _MediumThreshold;
                ramp.MediumSmooth = _MediumSmooth;
                ramp.MediumColor = _MediumColor;
                ramp.ShadowThreshold = _ShadowThreshold;
                ramp.ShadowSmooth = _ShadowSmooth;
                ramp.ShadowColor = _ShadowColor;
                ramp.ReflectThreshold = _ReflectThreshold;
                ramp.ReflectSmooth = _ReflectSmooth;
                ramp.ReflectColor = _ReflectColor;
                half3 lightColor = light.color;

                //radiance
                #if defined (_BRUSHTEX)
                half3 brush = SAMPLE_TEXTURE2D(_BrushTexture, sampler_BrushTexture,
                                   IN.uv * _BrushTexture_ST.xy + _BrushTexture_ST.zw).rgb;
                half3 radiance = CalculateStylizedRadiance(light.shadowAttenuation, ramp,
                                   NoL, brush,half3(_MedBrushStrength, _ShadowBrushStrength, _ReflectBrushStrength));
                #else
                half3 radiance = CalculateStylizedRadiance(ramp, NoL, 0, 0);
                #endif

                //brdf
                BRDF brdf = GetBRDF(surface);
                lightColor *= DirectBRDF(surface, brdf, light) * radiance;
                half3 go = EnvironmentBRDF(surface, brdf, indirectDiffuse, lightColor, radiance);

                //reflectionProbe
                half3 envirReflection = GetReflectionProbe(surface);
                envirReflection *= surface.metallic + MIN_REFLECTIVITY;
                envirReflection *= albedo.rgb;
                result.rgb = saturate(go + envirReflection);
                result.a = surface.alpha;

                //rim
                #if defined (_RIM)
                half NoV = dot(surface.viewDir, surface.normal);
                half3 rim = smoothstep(1 - _RimThreshold, 1 - _RimThreshold - _RimSmooth, saturate(NoV)) * _RimColor;
                result.rgb += rim;
                result.rgb = saturate(result.rgb);
                #endif

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

                //to vertex
                float3 ndcPos = IN.positionCS.xyz / IN.positionCS.w;
                uint2 screenPos;
                screenPos.x = (uint)((ndcPos.x * 0.5 + 0.5) * _RenderSizeParams.x);
                screenPos.y = (uint)((ndcPos.y * 0.5 + 0.5) * _RenderSizeParams.y);
screenPos = IN.positionCS;
                float depth = Linear01Depth(IN.positionCS.z, _ZBufferParams);
                float transmission = 1.0 - result.a;
                uint transmissionInt = (uint)(transmission * 255.0); //  0 - 255
                uint depthInt = (uint)(depth * 16777215.0); //  0 - 2^24-1


                //TODO to oit utils
                //oit
                uint fragCount = _FragmentLinksBuffer.IncrementCounter();
                //buffer adress
                uint startOffsetAddress = 4 * (_RenderSizeParams.x * screenPos.y + screenPos.x);

                uint startOffsetOld;
                _StartOffsetBuffer.InterlockedExchange(startOffsetAddress, fragCount, startOffsetOld);
                Fragment fragment;
              //  fragment.colour = PackRGBA(ToRGBE(result));
                fragment.testColor = result;
                fragment.testdepth = depth;
                fragment.testtransmission = transmission;
              //  fragment.transmissionAndDepth = (depthInt << 8) | transmissionInt;
                fragment.next = startOffsetOld;
                _FragmentLinksBuffer[fragCount] = fragment;
                return result;
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

    // CustomEditor "Barkar.BSRP.Editor.ShaderEditor.BSRPStylizedLitShaderEditor"
}