Shader "Hidden/ScreenSpaceShadow"
{
    SubShader
    {
        Blend DstColor Zero
        ZTest Always ZWrite Off Cull Off


        Pass
        {
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment ScreenSpaceShadowFragment

            #pragma multi_compile _ _SOFT_SHADOWS_LOW _SOFT_SHADOWS_MEDIUM _SOFT_SHADOWS_HIGH


            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/UnityInput.hlsl"
            
            struct Attributes
            {
                float3 positionOS : POSITION;
              //  half3 normalOS : NORMAL;
               // half4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
              //  half4 color : COLOR;
            };

            TEXTURE2D_HALF(_GBuffer3); //emission
            TEXTURE2D_HALF(_CameraDepth);


            half4 ScreenSpaceShadowFragment(Varyings IN) : SV_Target
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_PointClamp, IN.uv).r;
                float4 positionNDC = float4(IN.uv * 2 - 1, depth, 1);
                float4 positionWS = mul(unity_MatrixIVP, positionNDC);
                positionWS *= rcp(positionWS.w);

                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                return SampleFilteredShadowMap(positionWS, shadowCoord, MainLightShadowsData);
            }
            ENDHLSL
        }
    }
}