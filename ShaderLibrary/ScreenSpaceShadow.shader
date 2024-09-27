Shader "Hidden/ScreenSpaceShadow"
{
    SubShader
    {
        Blend DstColor Zero
        ZTest Always ZWrite Off Cull Off
     
        Stencil
        {
            Ref 8
            Comp Equal
        }
        
        Pass
        {
            Name "Screen space shadow"
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment ScreenSpaceShadowFragment

            #pragma multi_compile _ _SOFT_SHADOWS_LOW _SOFT_SHADOWS_MEDIUM _SOFT_SHADOWS_HIGH
            
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/UnityInput.hlsl"
            
            TEXTURE2D_HALF(_CameraDepth);
            TEXTURE2D_HALF(_GBuffer2);
         
            half4 ScreenSpaceShadowFragment(Varyings IN) : SV_Target
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_PointClamp, IN.uv).r;
                float4 positionNDC = float4(IN.uv * 2 - 1, depth, 1);
                float4 positionWS = mul(unity_MatrixIVP, positionNDC);
                positionWS *= rcp(positionWS.w);

           //     half2 encodeNormal = SAMPLE_TEXTURE2D(_GBuffer2, sampler_point_clamp, IN.uv).xy;
              //  half3 decodeNormal =SafeNormalize(mul(UNITY_MATRIX_I_V,(SpheremapDecodeNormal(encodeNormal))));
               // half NoL = saturate(dot(MainLightDirectionaAndMask.xyz, decodeNormal));
                

                float4 shadowCoord = TransformWorldToShadowCoord(positionWS.xyz);
                return  SampleFilteredShadowMap(positionWS, shadowCoord, MainLightShadowsData);
            }
            ENDHLSL
        }
    }
}