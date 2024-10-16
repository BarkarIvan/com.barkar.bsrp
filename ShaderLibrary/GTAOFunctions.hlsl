#ifndef GTAO_FUNCTIONS
#define GTAO_FUNCTIONS

TEXTURE2D(_GTAOTexture);
TEXTURE2D(_CameraDepth);
TEXTURE2D(_BentNormalTexture);
TEXTURE2D(_GBuffer0);

uniform half _GTAO_Sharpness;
#define KERNEL_RADIUS 3


inline float ApproximateConeConeIntersection(float ArcLength0, float ArcLength1, float AngleBetweenCones)
{
    float AngleDifference = abs(ArcLength0 - ArcLength1);

    float Intersection = smoothstep(
        0, 1, 1 - saturate((AngleBetweenCones - AngleDifference) / (ArcLength0 + ArcLength1 - AngleDifference)));

    return Intersection;
}

inline half3 MultiBounce(half AO, half3 Albedo)
{
    half3 A = 2 * Albedo - 0.33;
    half3 B = -4.8 * Albedo + 0.64;
    half3 C = 2.75 * Albedo + 0.69;
    return max(AO, ((AO * A + B) * AO + C) * AO);
}

inline half ReflectionOcclusion(half3 BentNormal, half3 ReflectionVector, half Roughness, half OcclusionStrength)
{
    half BentNormalLength = length(BentNormal);
    half ReflectionConeAngle = max(Roughness, 0.1) * PI;
    half UnoccludedAngle = BentNormalLength * PI * OcclusionStrength;

    half AngleBetween = acos(dot(BentNormal, ReflectionVector) / max(BentNormalLength, 0.001));
    half ReflectionOcclusion = ApproximateConeConeIntersection(ReflectionConeAngle, UnoccludedAngle, AngleBetween);
    ReflectionOcclusion = lerp(0, ReflectionOcclusion, saturate((UnoccludedAngle - 0.1) / 0.2));
    return ReflectionOcclusion;
}


inline void FetchAoAndDepth(float2 uv, inout float4 ao, inout float depth)
{
    float4 aod = SAMPLE_TEXTURE2D(_GTAOTexture, sampler_linear_clamp, uv);
    ao = aod;
    depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepth, sampler_linear_clamp, uv), _ZBufferParams);
}

inline float CrossBilateralWeight(float r, float d, float d0)
{
    const float BlurSigma = (float)KERNEL_RADIUS * 0.5;
    const float BlurFalloff = 1 / (2 * BlurSigma * BlurSigma);

    float dz = (d0 - d) * _ProjectionParams.z * _GTAO_Sharpness;
    return exp2(-r * r * BlurFalloff - dz * dz);
}

inline void ProcessSample(float4 ao, float z, float r, float d0, inout float4 totalAO, inout float totalW)
{
    float w = CrossBilateralWeight(r, d0, z);
    totalW += w;
    totalAO += w * ao;
}

inline void ProcessRadius(float2 uv0, float2 deltaUV, float d0, inout float4 totalAO, inout float totalW)
{
    float4 ao;
    float z;
    float2 uv;
    float r = 1;

    UNITY_UNROLL
    for (; r <= KERNEL_RADIUS / 2; r += 1)
    {
        uv = uv0 + r * deltaUV;
        FetchAoAndDepth(uv, ao, z);
        ProcessSample(ao, z, r, d0, totalAO, totalW);
    }

    UNITY_UNROLL
    for (; r <= KERNEL_RADIUS; r += 2)
    {
        uv = uv0 + (r + 0.5) * deltaUV;
        FetchAoAndDepth(uv, ao, z);
        ProcessSample(ao, z, r, d0, totalAO, totalW);
    }
}



inline float4 BilateralBlur(half4 ao, float depth, float2 uv0, float2 deltaUV)
{
    float totalW = 1;
    ProcessRadius(uv0, -deltaUV, depth, ao, totalW);
    ProcessRadius(uv0, deltaUV, depth, ao, totalW);

    ao /= totalW;
    return ao;
}


uniform half4 _GTAOParams; //pow, radius, sampleCount, thickness;
uniform half4 _AOUVToViewCoef;
uniform half _AO_HalfProjScale;
uniform half _GTAO_Intencity;

TEXTURE2D_HALF(_GBuffer2);

#define GTAO_POW _GTAOParams.x
#define GTAO_RADIUS _GTAOParams.y
#define SAMPLE_COUNT _GTAOParams.z
#define GTAO_THICKNESS _GTAOParams.w
#define SLICE_COUNT 3 //1-8


float2 GetScreenSpacePosition(float2 uv)
{
    return float2(uv * _RenderSizeParams.xy);
}

inline half3 GetPosition2(half2 uv)
{
    half rawDwpth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_point_clamp, uv).r;
    half linearDepth = LinearEyeDepth(rawDwpth, _ZBufferParams);
    return half3((uv * _AOUVToViewCoef.xy + _AOUVToViewCoef.zw) * linearDepth, linearDepth);
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

half4 GTAO(half2 uv)
{
    half rawDepth = SAMPLE_TEXTURE2D(_CameraDepth, sampler_point_clamp, uv);
    if (rawDepth <= 1e-7)
    {
        return 1;
    }

    //pos
    half3 viewPos = GetPosition2(uv);
    half3 viewDir = SafeNormalize(0 - viewPos);

    //N
    half2 normal = SAMPLE_TEXTURE2D(_GBuffer2, sampler_linear_clamp, uv).rg;
    half3 normalVS = SafeNormalize(SpheremapDecodeNormal(normal));
    normalVS.z = -normalVS.z;


    half stepRadius = max(min((GTAO_RADIUS * _AO_HalfProjScale) / viewPos.b, 512), (half)SLICE_COUNT);
    stepRadius /= ((half)SLICE_COUNT + 1);

    //noise
    half noiseDir = GTAO_Noise(uv * _RenderSizeParams.xy);
    half noiseOffset = GTAO_Offsets(uv);
    half initialRayStep = frac(noiseOffset + 0); //0 - _GTAOTemporalOffsets


    half ao, angle, sliceLength, n, cos_n, bentAngle, wallDarkeningCorrection;
    half2 h, H, falloff, uvOffset, h1h2, h1h2Length, slideDir_TexelSize;
    half3 sliceDir, h1, h2, planeNormal, planeTangent, sliceNormal, bentNormal;
    half4 uvSlice;


    UNITY_LOOP
    for (int i = 0; i < SAMPLE_COUNT; i++)
    {
        angle = (i + noiseDir + 0) * (PI / SAMPLE_COUNT); // 0 - _GTAOTemporalDirection
        sliceDir = half3(half2(cos(angle), sin(angle)), 0);
        slideDir_TexelSize = (sliceDir.xy * _RenderSizeParams.zw);
        h = -1;

        for (int j = 0; j < SLICE_COUNT; j++)
        {
            uvOffset = slideDir_TexelSize * max(stepRadius * (j + initialRayStep), 1 + j);
            uvSlice = uv.xyxy + float4(uvOffset.xy, -uvOffset);
            h1 = GetPosition2(uvSlice.xy) - viewPos;
            h2 = GetPosition2(uvSlice.zw) - viewPos;

            //enghts and falloff
            h1h2 = half2(dot(h1, h1), dot(h2, h2)); //sqrt lenght?
            h1h2Length = rsqrt(h1h2);
            falloff = saturate(h1h2.xy / (GTAO_RADIUS * GTAO_RADIUS));

            H = half2(dot(h1, viewDir), dot(h2, viewDir)) * h1h2Length;
            h.xy = (H.xy > h.xy) ? lerp(H, h, falloff) : lerp(H.xy, h.xy, GTAO_THICKNESS);
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
    bentNormal = normalize(normalize(half3(bentNormal.xy, bentNormal.z) - viewDir * 0.5));
    bentNormal =  mul(UNITY_MATRIX_V, half3(bentNormal));
    ao = saturate(pow(ao / SAMPLE_COUNT, GTAO_POW));
    return half4(bentNormal, ao);
}

#endif
