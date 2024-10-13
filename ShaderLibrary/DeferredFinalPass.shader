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
            #pragma multi_compile _ACESTONEMAP _GTTONEMAP
            
            
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

            //GT TONEMAP

            static const float e = 2.71828;

            float W_f(float x, float e0, float e1)
            {
                if (x <= e0)
                    return 0;
                if (x >= e1)
                    return 1;
                float a = (x - e0) / (e1 - e0);
                return a * a * (3 - 2 * a);
            }

            float H_f(float x, float e0, float e1)
            {
                if (x <= e0)
                    return 0;
                if (x >= e1)
                    return 1;
                return (x - e0) / (e1 - e0);
            }

            float GTTonemap(float x)
            {
                float m = 0.22; // linear section start
                float a = 1.0; // contrast
                float c = 1.33; // black brightness
                float P = 1.0; // maximum brightness
                float l = 0.4; // linear section length
                float l0 = ((P - m) * l) / a; // 0.312
                float S0 = m + l0; // 0.532
                float S1 = m + a * l0; // 0.532
                float C2 = (a * P) / (P - S1); // 2.13675213675
                float L = m + a * (x - m);
                float T = m * pow(x / m, c);
                float S = P - (P - S1) * exp(-C2 * (x - S0) / P);
                float w0 = 1 - smoothstep(0.0, m, x);
                float w2 = (x < m + l) ? 0 : 1;
                float w1 = 1 - w0 - w2;
                return float(T * w0 + L * w1 + S * w2);
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

                #if defined (_GTTONEMAP)
                result.r = GTTonemap(result.r);
                result.g = GTTonemap(result.g);
                result.b = GTTonemap(result.b);
                #endif

                #if defined (_ACESTONEMAP)
                result.rgb = ACESFilmTonemapping(result.rgb);
                #endif
                
                return half4(result.rgb, 1);
            }
            ENDHLSL
        }
    }
}