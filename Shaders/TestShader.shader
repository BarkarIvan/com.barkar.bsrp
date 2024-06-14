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

             TEXTURE2D_SHADOW(_ShadowMap);
            SAMPLER_CMP(sampler_linear_clamp_compare);
           // SAMPLER(sampler_linear_clamp_compare);

            CBUFFER_START(DirectionalLightDataBuffer)
                half4 directionalLightColor;
                half4 directionalLightDirectionaAndMask;
                half4 directionalShadowsData;
            CBUFFER_END

            float4x4 _DirectionalLightVPMatrix;


            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 shadowCoord : TEXCOORD0;
                half3 normalWS : NORMAL;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS).xyz;
                OUT.shadowCoord = mul(_DirectionalLightVPMatrix, float4(positionWS + directionalShadowsData.x, 1.0));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                //get dirLight LIGHT HLSL
                half3 dir = directionalLightDirectionaAndMask.xyz;
                half4 loghtColor = directionalLightColor;
                //

                half NoL = max(dot(dir, IN.normalWS), 0);
                half4 result = saturate(_Color * NoL * loghtColor);
                result.a = _Color.a;

                half shadow = SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_linear_clamp_compare, IN.shadowCoord);
                result.rgb *= shadow;
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
            #include "Packages/com.barkar.bsrp/ShaderLibrary/UnityInput.hlsl"


            TEXTURE2D(_ShadowMap);
            SAMPLER(sampler_ShadowMap);

            half4 _Color;


            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
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