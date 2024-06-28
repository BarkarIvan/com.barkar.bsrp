Shader "Hidden/BSRPCustomBloom"
{
    HLSLINCLUDE

    #include "Packages/com.barkar.bsrp/ShaderLibrary/CustomBlit.hlsl"

    half3 Prefilter(half3 c)
    {
        half brightness = Max3(c.r, c.g, c.b);
        half soft = brightness - _Filter.y;
        soft = clamp(soft, 0, _Filter.z);
        soft = soft * soft * _Filter.w;
        half contribution = max(soft, brightness - _Filter.x);
        contribution /= max(brightness, 1e-4);
        return c * contribution;
    }

    half4 FragPrefilter(Varyings IN) : SV_Target
    {
        half3 result = 1.0;
        half3 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord).xyz;
        result.rgb = Prefilter(col.rgb);
        result = max(result, 0);
        return half4(result, 1.0);
    }

    half4 FragBlurDownSample(Varyings IN): SV_Target
    {
        half3 sum = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord).rgb * 4.0;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord - _DualFilterOffset.xy).rgb;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + _DualFilterOffset.xy).rgb;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + half2(_DualFilterOffset.x, -_DualFilterOffset.y)).rgb;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord - half2(_DualFilterOffset.x, -_DualFilterOffset.y)).rgb;
        return (half4(sum * 0.125, 1));
    }

    half4 FragBlurUpsample(Varyings IN): SV_Target
    {
        half3 sum = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + half2(-_DualFilterOffset.x * 2.0, 0.0)).rgb;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + half2(-_DualFilterOffset.x,_DualFilterOffset.y)).rgb * 2.0;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + half2(0.0, _DualFilterOffset.y * 2.0
              )).rgb;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + half2(_DualFilterOffset.x, _DualFilterOffset.y)).rgb * 2.0;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + half2(_DualFilterOffset.x * 2.0, 0.0)).rgb;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + half2(_DualFilterOffset.x, _DualFilterOffset.y)).rgb * 2.0;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord + half2(0.0, -_DualFilterOffset.y * 2.0)).rgb;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord - half2(_DualFilterOffset.x, _DualFilterOffset.y)).rgb * 2.0;
        sum *= 0.0833;
        
        return half4(sum, 1);
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Bloom Prefilter"

            HLSLPROGRAM
                #pragma vertex Vert
                 #pragma fragment FragPrefilter
            ENDHLSL
        }

        Pass
        {
            Name "Dual Filter Downsample"

            HLSLPROGRAM
                 #pragma vertex Vert
                 #pragma fragment FragBlurDownSample
            ENDHLSL
        }

        Pass
        {
            Name "Dual Filter Upsample"

            HLSLPROGRAM
                 #pragma vertex Vert
                 #pragma fragment FragBlurUpsample
            ENDHLSL
        }
    }
}