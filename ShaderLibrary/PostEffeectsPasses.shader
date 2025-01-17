Shader "Hidden/PostEffectPasses"
{
    HLSLINCLUDE
    #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
    #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"

    half4 _Filter;
    half4 _DualFilterOffset;


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
        half3 col = SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp, IN.uv).xyz;
        result.rgb = Prefilter(col.rgb);
        result = max(result, 0);
        return half4(result, 1.0);
    }


    half4 FragBlurDownSample(Varyings IN): SV_Target
    {
        half3 sum = SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp, IN.uv).rgb * 4.0;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp, IN.uv - _DualFilterOffset.xy).rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp, IN.uv + _DualFilterOffset.xy).rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                                IN.uv + half2(_DualFilterOffset.x, -_DualFilterOffset.y)).rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                                IN.uv - half2(_DualFilterOffset.x, -_DualFilterOffset.y)).rgb;
        sum *= 0.125;

        return (half4(sum, 1));
    }

    half4 FragBlurUpsample(Varyings IN): SV_Target
    {
        half3 sum = 0.0;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                                IN.uv + half2(-_DualFilterOffset.x * 2.0, 0.0)).rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                                IN.uv + half2(-_DualFilterOffset.x, _DualFilterOffset.y)).
            rgb * 2.0;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                                IN.uv + half2(0.0, _DualFilterOffset.y * 2.0)).rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                                IN.uv + half2(_DualFilterOffset.x, _DualFilterOffset
                                    .y
                                )).rgb * 2.0;

        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                                         IN.uv + half2(_DualFilterOffset.x * 2.0, 0.0)).
            rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                        IN.uv + half2(_DualFilterOffset.x, -_DualFilterOffset.y)).rgb * 2.0;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
             IN.uv + half2(0.0, -_DualFilterOffset.y * 2.0
             )).rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
            IN.uv + half2(-_DualFilterOffset.x, -_DualFilterOffset.y)).rgb * 2.0;

        sum = sum * 0.0833;

        return half4(sum, 1);
    }
    
    ENDHLSL

    SubShader
    {

        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Bloom Prefilter" //0
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment FragPrefilter
            ENDHLSL
        }

        Pass
        {
            Name "Dual Filter Downsample" //1
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment FragBlurDownSample
            ENDHLSL
        }

        Pass
        {
            Name "Dual Filter Upsample" //2
            Blend One One
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment FragBlurUpsample
            ENDHLSL
        }
    }
}