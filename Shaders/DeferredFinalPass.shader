Shader "Hidden/DeferredFinalPass"
{

    SubShader
    {
        ZTest Always ZWrite Off Cull Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment FinalPassFragment

            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"
            
            
            struct Attributes
            {
                float3 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            
            TEXTURE2D_HALF(_GBuffer3); //emission

            half3 ACESFilmTonemapping(half3 col)
            {
                half a = 2.51;
                half b = 0.03;
                half c = 2.43;
                half d = 0.59;
                half e = 0.14;
                return saturate((col * (a * col + b)) / (col * (c * col + d) + e));
            }
            
            half4 FinalPassFragment(Varyings IN): SV_Target
            {
                half3 g3 = SAMPLE_TEXTURE2D(_GBuffer3, sampler_linear_clamp, IN.uv);
                half4 result = half4(g3, 1.0);
                result.rgb = ACESFilmTonemapping(result.rgb);
                return half4(result.rgb, 1);
            }
            ENDHLSL
        }
    }
}