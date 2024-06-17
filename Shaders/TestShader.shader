Shader "BSRP/TestShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [Space(40)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Int) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _Blend1 ("Blend mode", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _Blend2 ("Blend mode", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Blend [_Blend1][_Blend2]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Tags
            {
                "LightMode" = "BSRPLightMode"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"

           

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            ///LIGHT HLSL

             TEXTURE2D_SHADOW(_MainLightShadowMap);
            SAMPLER_CMP(sampler_LinearClampCompare);

            CBUFFER_START(MainLightDataBuffer)
                half4 MainLightColor;
                half4 MainLightDirectionaAndMask;
                half4 MainLightShadowsData; //light strength, shadowBias, normalBias
            CBUFFER_END

            float4x4 _MainLightMatrix;
            half4 _MainLightShadowDistanceFade; //max distance, fadeWidth, 0, 0,


            
            ///

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 shadowCoord: TEXCOORD1;
                half3 normalWS : NORMAL;
            };



            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS).xyz;
                OUT.shadowCoord = mul(_MainLightMatrix, float4(OUT.positionWS.xyz + MainLightShadowsData.y, 1.0));
               
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                //get dirLight LIGHT HLSL
                half3 dir = MainLightDirectionaAndMask.xyz;
                half4 loghtColor = MainLightColor;
                //

                half NoL = max(dot(dir, IN.normalWS), 0);
                half4 result = saturate(_Color * NoL * loghtColor);
                result.a = _Color.a;

                half shadow = SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowMap, sampler_LinearClampCompare, IN.shadowCoord);
                half fade =  1.0 - saturate ((distance (IN.positionWS, _WorldSpaceCameraPos.xyz) - _MainLightShadowDistanceFade.x) / _MainLightShadowDistanceFade.y );
                shadow = LerpWhiteTo(shadow, MainLightShadowsData.x * fade);
                //shadow = IN.shadowCoord.z <= 0.0 || IN.shadowCoord.z >= 1.0 ? 1.0 : shadow;

                result.rgb *= saturate(shadow);
                return result;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster"
            }
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"


            TEXTURE2D(_ShadowMap);
            SAMPLER(sampler_ShadowMap);

            half4 _Color;


            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            ////LIGHT HLSLS
            CBUFFER_START(DirectionalLightDataBuffer)
                half4 directionalLightColor;
                half4 directionalLightDirectionaAndMask;
                half4 directionalShadowsData; //str, shadowBias, normalBias
            CBUFFER_END
            
            float4x4 _DirectionalLightMatrix;
            half4 _ShadowDistanceFade;
/////
///
             float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                half3 normalWS = TransformObjectToWorldDir(input.normalOS.xyz);

                //apply bias
               // half invNdotL = 1.0 - saturate(dot(directionalLightDirectionaAndMask.xyz, normalWS.xyz));
               // half scale = invNdotL * directionalShadowsData.z;

               // positionWS = directionalLightDirectionaAndMask.xyz * directionalShadowsData.yyy + positionWS.xyz;
               // positionWS = normalWS * scale.xxx + positionWS;
                //
                float4 positionCS = TransformWorldToHClip(positionWS);

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = GetShadowPositionHClip(IN);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}