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
                        IN.uv + half2(_DualFilterOffset.x, _DualFilterOffset.y
                        )).rgb * 2.0;

        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                                                   IN.uv + half2(_DualFilterOffset.x * 2.0, 0.0)).rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                      IN.uv + half2(_DualFilterOffset.x, -_DualFilterOffset.y)).rgb * 2.0;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                        IN.uv + half2(0.0, -_DualFilterOffset.y * 2.0)).rgb;
        sum += SAMPLE_TEXTURE2D(_SourceTexture, sampler_linear_clamp,
                  IN.uv + half2(-_DualFilterOffset.x, -_DualFilterOffset.y)).rgb * 2.0;

        sum = sum * 0.0833;

        return half4(sum, 1);
    }

    //GTAo///////////

    half4 _GTAOParams; //intens, radius, sampleCount;
    half4 _AOUVToViewCoef;
    
    TEXTURE2D_HALF(_CameraDepth);
    TEXTURE2D_HALF(_GBuffer2);
    static half GTAORandomUV[40] =
    {
        0.00000000, // 00
        0.33984375, // 01
        0.75390625, // 02
        0.56640625, // 03
        0.98437500, // 04
        0.07421875, // 05
        0.23828125, // 06
        0.64062500, // 07
        0.35937500, // 08
        0.50781250, // 09
        0.38281250, // 10
        0.98437500, // 11
        0.17578125, // 12
        0.53906250, // 13
        0.28515625, // 14
        0.23137260, // 15
        0.45882360, // 16
        0.54117650, // 17
        0.12941180, // 18
        0.64313730, // 19

        0.92968750, // 20
        0.76171875, // 21
        0.13333330, // 22
        0.01562500, // 23
        0.00000000, // 24
        0.10546875, // 25
        0.64062500, // 26
        0.74609375, // 27
        0.67968750, // 28
        0.35156250, // 29
        0.49218750, // 30
        0.12500000, // 31
        0.26562500, // 32
        0.62500000, // 33
        0.44531250, // 34
        0.17647060, // 35
        0.44705890, // 36
        0.93333340, // 37
        0.87058830, // 38
        0.56862750, // 39
    };

    // SSAO Settings
    #define INTENSITY _GTAOParams.x
    #define RADIUS _GTAOParams.y
    #define SAMPLES _GTAOParams.z

    // Constants
    // kContrast determines the contrast of occlusion. This allows users to control over/under
    // occlusion. At the moment, this is not exposed to the editor because it's rarely useful.
    // The range is between 0 and 1.
    static const half kContrast = half(0.5);

    // The constant below controls the geometry-awareness of the bilateral
    // filter. The higher value, the more sensitive it is.
    static const half kGeometryCoeff = half(0.8);

    // The constants below are used in the AO estimator. Beta is mainly used for suppressing
    // self-shadowing noise, and Epsilon is used to prevent calculation underflow. See the paper
    // (Morgan 2011 https://casual-effects.com/research/McGuire2011AlchemyAO/index.html)
    // for further details of these constants.
    static const half kBeta = half(0.002);
    static const half kEpsilon = half(0.0001);

    // Trigonometric function utility
half2 CosSin(half theta)
{
    half sn, cs;
    sincos(theta, sn, cs);
    return half2(cs, sn);
}

// Pseudo random number generator with 2D coordinates
half GetRandomUVForSSAO(float u, int sampleIndex)
{
    return GTAORandomUV[u * 20 + sampleIndex];
}

    float2 GetScreenSpacePosition(float2 uv)
{
    return float2(uv * _RenderSizeParams.xy);
}

    inline half3 GetPosition2(half2 uv)
{
   
    return half3((uv * _AOUVToViewCoef.xy + _AOUVToViewCoef.zw) * linearDepth, linearDepth);
}
    
    half4 FragGTAO(Varyings IN):SV_Target
    {
       

         half rawDwpth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_point_clamp, IN.uv);
    half linearDepth = LinearEyeDepth(rawDwpth, _ZBufferParams);


        //viewp
        float zScale = linearDepth * (1/_ProjectionParams.y); // near
float3 viewPos = float3(IN.uv.x, IN.uv.y, zScale); // x и y из uv
viewPos = mul(unity_MatrixIVP, float4(viewPos, 1)).xyz; // Обратная проекционная матрица

        //pos
        half3 vp = half3((IN.uv * _AOUVToViewCoef.xy + _AOUVToViewCoef.zw) * linearDepth, linearDepth)
        half3 viewDir = normalize(0 - vp);

        //N
        half3 normal;// sample and encode normal

        //radius
        half fade =  saturate(max(0, vp.z - 0.5) * 0);
        half2 radius_thickness = lerp(half2(RADIUS, 1), half2(0, 0), fade.xx);
    half radius = radius_thickness.x;
    half thickness = radius_thickness.y;

        //noise
        half2 pos = IN.uv * _RenderSizeParams.xy;
        half noiseDir = frac(52.9829189 * frac(dot(pos, half2(0.06711056, 0.00583715))));
        half offset = frac(0.25 * (half) ((int2)(pos.y - pos.x) & 3));
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
            Blend One One
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment FragBlurUpsample
            ENDHLSL
        }

        Pass
        {
            Name "GTAO"
            Blend One One
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment FragGTAO
            ENDHLSL
        }
    }
}