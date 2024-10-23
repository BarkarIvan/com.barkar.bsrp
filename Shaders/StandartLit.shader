Shader "BSRP/StandartLit"
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

        _DiffractionWidth("Diffraction Width", Range(0,7)) = 0
        _DiffractionHeight("Diffraction Height", Range(0,0.0045)) = 0

        [HDR] _EmissionColor ("Emission", Color) = (1,1,1,1)
        _EmissionMap ("EmissionMap", 2D) = "black"{}

        _Brightness("Brightness", Range(0,2)) = 1

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

        Pass
        {
            Name "LitGbuffer"
            Tags
            {
                "LightMode" = "BSRPGBuffer"
            }

            Cull [_Cull]
            Blend [_Blend1] [_Blend2]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex GBufferVertex
            #pragma fragment GBufferFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ADDITIONALMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _USEALPHACLIP
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x


          

            #include "Packages/com.barkar.bsrp/ShaderLibrary/LitInput.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/GBufferPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]


            HLSLPROGRAM
            #pragma shader_feature_local _USEALPHACLIP
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            #include "Packages/com.barkar.bsrp/ShaderLibrary/ShadowCaterPass.hlsl"
            ENDHLSL
        }

 Pass
        {
            Name "Depth Normals"
            Tags
            {
                "LightMode"="DepthNormalsOnly"
            }

            ZWrite On
            ZTest LEqual
            Cull[_Cull]


            HLSLPROGRAM
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ADDITIONALMAP
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

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
                float2 addUv : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : NORMAL;
                half3 tangentWS : TEXCOORD3;
                half3 bitangentWS : TEXCOORD4;
            };
            
            #include "Packages/com.barkar.bsrp/ShaderLibrary/LitInput.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/DepthNormalsPass.hlsl"
            ENDHLSL
        }

Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode"="Meta"
            }

           // ZWrite On
            //ZTest LEqual
            Cull Off


            HLSLPROGRAM
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ADDITIONALMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _USEALPHACLIP
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            
            #include "Packages/com.barkar.bsrp/ShaderLibrary/LitInput.hlsl"
           #include "Packages/com.barkar.bsrp/ShaderLibrary/MetaPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "Barkar.BSRP.Editor.ShaderEditor.BSRPStandartLitShaderEditor"
}