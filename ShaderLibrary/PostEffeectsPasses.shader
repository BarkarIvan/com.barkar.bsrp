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

    //GTAo///////////

    half4 _GTAOParams; //intens, radius, sampleCount, thickness;
    half4 _AOUVToViewCoef;
    half _AO_HalfProjScale;

    TEXTURE2D_HALF(_CameraDepth);
    TEXTURE2D_HALF(_GBuffer2);

    // SSAO Settings
    #define INTENSITY _GTAOParams.x
    #define RADIUS _GTAOParams.y
    #define SAMPLE_COUNT _GTAOParams.z
    #define THICKNESS _GTAOParams.w
    #define SLICE_COUNT 3


    float2 GetScreenSpacePosition(float2 uv)
    {
        return float2(uv * _RenderSizeParams.xy);
    }

    inline half3 GetPosition2(half2 uv)
    {
        half rawDwpth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_point_clamp, uv).r;
        half linearDepth = LinearEyeDepth(rawDwpth, _ZBufferParams);
        return half3((uv * _AOUVToViewCoef.xy + _AOUVToViewCoef.zw) * linearDepth, linearDepth);;
    }

    inline half GTAO_Noise(half2 position)
    {
        return frac(52.9829189 * frac(dot(position, half2(0.06711056, 0.00583715))));
    }


    half IntegrateArc_CosWeight(half2 h, half n)
    {
        half2 Arc = -cos(2 * h - n) + cos(n) + 2 * h * sin(n);
        return 0.25 * (Arc.x + Arc.y);
    }

    inline half GTAO_Offsets(half2 uv)
    {
        int2 position = (int2)(uv * _RenderSizeParams.xy);
        return 0.25 * (half)((position.y - position.x) & 3);
    }

    half4 FragGTAO(Varyings IN):SV_Target
    {
        half rawDepth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_point_clamp, IN.uv);
        if (rawDepth <= 1e-7)
        {
            return 1;
        }

        //pos
        half3 viewPos = GetPosition2(IN.uv);
        half3 viewDir = SafeNormalize(0 - viewPos);

        //N
        half2 normal = SAMPLE_TEXTURE2D(_GBuffer2, sampler_linear_clamp, IN.uv).rg;
        half3 normalVS = SafeNormalize(SpheremapDecodeNormal(normal));
        normalVS.z = -normalVS.z;


        half stepRadius = max(min((RADIUS * _AO_HalfProjScale) / viewPos.b, 512), (half)SLICE_COUNT);
        stepRadius /= ((half)SLICE_COUNT + 1);

        //noise
        half noiseDir = GTAO_Noise(IN.uv * _RenderSizeParams.xy);
        half noiseOffset = GTAO_Offsets(IN.uv);
        half initialRayStep = frac(noiseOffset + 0); // _AO_TemporalOffsets
       


        half ao, angle, sliceLength, n, cos_n, bentAngle, wallDarkeningCorrection;
        half2 h, H, falloff, uvOffset, h1h2, h1h2Length, slideDir_TexelSize;
        half3 sliceDir, h1, h2, planeNormal, planeTangent, sliceNormal, bentNormal;
        half4 uvSlice;


        UNITY_LOOP
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            angle = (i + noiseDir + 0) * (PI / (half)SAMPLE_COUNT); // 0 - temporal dire
            sliceDir = half3(half2(cos(angle), sin(angle)), 0);
            slideDir_TexelSize = (sliceDir.xy * _RenderSizeParams.zw);
            h = -1;

            for (int j = 0; j < SLICE_COUNT; j++)
            {
                uvOffset = slideDir_TexelSize * max(stepRadius * (j + initialRayStep), 1 + j);
                uvSlice = IN.uv.xyxy + float4(uvOffset.xy, -uvOffset);
                h1 = GetPosition2(uvSlice.xy) - viewPos;
                h2 = GetPosition2(uvSlice.zw) - viewPos;

                //enghts and falloff
                h1h2 = half2(dot(h1, h1), dot(h2, h2)); //sqrt lenght?
                h1h2Length = rsqrt(h1h2);
                falloff = saturate(h1h2.xy / (RADIUS * RADIUS));

                H = half2(dot(h1, viewDir), dot(h2, viewDir)) * h1h2Length;
                h.xy = (H.xy > h.xy) ? lerp(H, h, falloff) : lerp(H.xy, h.xy, THICKNESS); //0.1 - thickness
            }

            planeNormal = SafeNormalize(cross(sliceDir, viewDir));
            planeTangent = cross(viewDir, planeNormal);
            sliceNormal = normalVS - planeNormal * dot(normalVS, planeNormal);
            sliceLength = length(sliceNormal);

            cos_n = clamp(dot(SafeNormalize(sliceNormal), viewDir), -1, 1);
            n = -sign(dot(sliceNormal, planeTangent)) * acos(cos_n);


            h = acos(clamp(h, -1, 1));
            h.x = n + max(-h.x - n, -HALF_PI);
            h.y = n + min(h.y - n, HALF_PI);

            
            ao += sliceLength * IntegrateArc_CosWeight(h, n);

            bentAngle = (h.x + h.y) * 0.5;
            bentNormal += viewDir * cos(angle) - planeTangent * sin(bentAngle);
        }
       bentNormal = SafeNormalize(SafeNormalize(bentNormal) - viewDir * 0.5);
        ao = saturate(pow(ao / ((half)SAMPLE_COUNT), INTENSITY));

        return half4(bentNormal, ao);
    }

    //GTAO Geometry Aware separable bilateral filter
    half4 BlurFilter(float2 uv, float2 delta)
    {
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

        Pass
        {
            Name "GTAO" //3
            Blend One One
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment FragGTAO
            ENDHLSL
        }
    }
}