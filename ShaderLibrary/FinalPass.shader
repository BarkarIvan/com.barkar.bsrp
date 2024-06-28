Shader "Hidden/FinalPass"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white"{}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" 
        }

        ZTest Always ZWrite Off Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment CompositingFrag
            #pragma multi_compile _ _USE_CUSTOM_LENSDIRT

            #include "../ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            // #include "Packages/com.barkar.bsrp/ShaderLibrary/CustomBlit.hlsl"
            #include "CameraRendererPasses.hlsl"

            TEXTURE2D (_CameraOpaque);
            TEXTURE2D(_BloomTexture);
            TEXTURE2D(_CustomBloomLensDirtTexture);
            half4 _CustomBloomParams;

            #define LensDirtIntesity _CustomBloomParams.x
            #define LensDirtScale  _CustomBloomParams.yz
            #define BloomIntensity _CustomBloomParams.w
            
            half3 ACESFilmTonemapping(half3 col)
            {
                half a = 2.51;
                half b = 0.03;
                half c = 2.43;
                half d = 0.59;
                half e = 0.14;
                return saturate((col * (a * col + b)) / (col * (c * col + d) + e));
            }
            
            half4 CompositingFrag(Varyings IN) : SV_Target
            {
                half3 cameraOpaque = SAMPLE_TEXTURE2D(_CameraOpaque, sampler_linear_clamp, IN.uv).rgb;
                half3 bloom = SAMPLE_TEXTURE2D(_BloomTexture, sampler_linear_clamp, IN.uv).rgb;
                half4 result = 1.0;
                
                bloom.rgb *=  BloomIntensity;
                result.rgb = cameraOpaque.rgb + bloom.rgb;
                
                #if defined (_USE_CUSTOM_LENSDIRT)
                half3 dirt = SAMPLE_TEXTURE2D(_CustomBloomLensDirtTexture, sampler_linear_clamp, IN.uv * LensDirtScale).rgb;
                dirt *= LensDirtIntesity;
                dirt *= bloom;
                result.rgb = dirt;
                #endif
                
                result.rgb = ACESFilmTonemapping(result.rgb);
                
                return result;
            }
            
            ENDHLSL
        }
    }
}