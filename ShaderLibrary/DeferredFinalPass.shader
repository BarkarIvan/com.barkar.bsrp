Shader "Hidden/DeferredFinalPass"
{

    SubShader
    {
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile _ _USE_LENSDIRT
            #pragma multi_compile _ _USE_BLOOM
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
            TEXTURE2D_HALF(_BloomTexture);
            TEXTURE2D_HALF(_CustomBloomLensDirtTexture);
            half4 _CustomBloomParams; //dirtIntensity, dirtaspect.xy, bloomIntensity

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


                #if defined (_USE_BLOOM)
                half3 bloom = SAMPLE_TEXTURE2D(_BloomTexture, sampler_linear_clamp, IN.uv).rgb;
                bloom.rgb *= _CustomBloomParams.w;

                #if defined (_USE_LENSDIRT)
                half3 lensDirt = SAMPLE_TEXTURE2D(_CustomBloomLensDirtTexture, sampler_linear_clamp,
                                 IN.uv * _CustomBloomParams.yz).r;

                lensDirt *= _CustomBloomParams.x;
                lensDirt *= bloom.rgb;
                bloom.rgb += lensDirt;
                # endif

                result.rgb += bloom.rgb;
                #endif

                result.rgb = ACESFilmTonemapping(result.rgb);

                return half4(result.rgb, 1);
            }
            ENDHLSL
        }
    }
}