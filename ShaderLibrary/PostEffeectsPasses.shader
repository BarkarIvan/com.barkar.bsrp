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

    half Weight2D(float2 uv, float s)
    {
        float E = 2.71828182846;
        float s2 = s * s;
        float a = -(uv.x * uv.x + uv.y * uv.y) / (2.0 * s2);
        return pow(E, a) / (2.0 * PI * s2);
    }

    half4 FragBlurDownSample(Varyings IN): SV_Target
{
    half weight = 0.0;
    half s = 0.2;

    half3 sum = SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp, IN.uv).rgb;
    half centerWeight = Weight2D(float2(0.0, 0.0), s);
    sum *= centerWeight;
    weight += centerWeight;

    float2 offsets[4] = {
        _DualFilterOffset.xy,
        half2(_DualFilterOffset.x, -_DualFilterOffset.y),
        -_DualFilterOffset.xy,
        half2(-_DualFilterOffset.x, _DualFilterOffset.y)
    };

    for (int i = 0; i < 4; i++)
    {
        float2 offset = offsets[i];
        half w = Weight2D(offset, s);
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp, IN.uv + offset).rgb * w;
        weight += w;
    }

    sum *= rcp(weight);
    return half4(sum, 1);
}


   half4 FragBlurUpsample(Varyings IN): SV_Target
{
    half weight = 0.0;
    half s = 0.2;
    half3 sum = 0.0;

    // Смещения для upsample с увеличенным радиусом выборки
    float2 offsets[4] = {
        half2(-_DualFilterOffset.x * 2.0, 0.0),
        half2(-_DualFilterOffset.x, _DualFilterOffset.y),
        half2(0.0, _DualFilterOffset.y * 2.0),
        half2(_DualFilterOffset.x, _DualFilterOffset.y)
    };

   
    for (int i = 0; i < 4; i++)
    {
        float2 offset = offsets[i];
        half w = Weight2D(offset, s);

        // Увеличиваем вес для центральных направлений
        if (i == 1 || i == 3)
            w *= 2.0;

        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp, IN.uv + offset).rgb * w;
        weight += w;
    }

    sum *= rcp(weight);
    return half4(sum, 1);
}
    ENDHLSL

    SubShader
    {

        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Bloom Prefilter"

            HLSLPROGRAM
                #pragma vertex DefaultPassVertex
                #pragma fragment FragPrefilter
            ENDHLSL
        }

        Pass
        {
            Name "Dual Filter Downsample"

            HLSLPROGRAM
                #pragma vertex DefaultPassVertex
                #pragma fragment FragBlurDownSample
            ENDHLSL
        }

        Pass
        {
            Name "Dual Filter Upsample"

            HLSLPROGRAM
                #pragma vertex DefaultPassVertex
                #pragma fragment FragBlurUpsample
            ENDHLSL
        }
    }
}