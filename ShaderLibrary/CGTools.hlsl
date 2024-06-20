#ifndef  CG_TOOLS
#define  CG_TOOLS

float2 rotate(float2 v, float angle)
{
    float sina, cosa;
    sincos(angle, sina, cosa);
    return mul(float2x2(float2(cosa, -sina), float2(sina, cosa)), v);
}

float3 rotateByAxis(float3 axis, float cosa, float sina, float3 vec)
{
    half3x3 rot = half3x3(half3(axis.x * axis.x * (1-cosa) + cosa,
        axis.y * axis.x * (1-cosa) - axis.z * sina,
        axis.z * axis.x * (1-cosa) + axis.y * sina),
        half3(axis.x * axis.y * (1-cosa) + axis.z * sina,
        axis.y * axis.y * (1-cosa) + cosa,
        axis.z * axis.y * (1-cosa) - axis.x * sina),
        half3(axis.x * axis.z * (1-cosa) - axis.y * sina,
        axis.y * axis.z * (1-cosa) + axis.x * sina,
        axis.z * axis.z * (1-cosa) + cosa));
    return mul(rot, vec);
}

float3 scale(float scale, float3 vec)
{
    half3x3(half3(scale,0,0),
    half3(0,scale,0),
    half3(0,0,scale));
    return mul(scale, vec);
}

/// inverse lerp and remap
float invLerp(float from, float to, float value)
{
    return (value - from) / (to - from);
}

float4 invLerp(float4 from, float4 to, float4 value)
{
    return (value - from) / (to - from);
}

float remap(float origFrom, float origTo, float targetFrom, float targetTo, float value)
{
    float rel = invLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}

float4 remap(float4 origFrom, float4 origTo, float4 targetFrom, float4 targetTo, float4 value)
{
    float4 rel = invLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}


half brightness(half3 color)
{
    return saturate(dot(color, half3(0.299, 0.587, 0.114)));
}

half4 grayscaleColor(half4 color, half amount)
{
    half gray_color = brightness(color);
    half4 grayscale = half4(gray_color, gray_color, gray_color, half(1.0));
    return lerp(color, grayscale, amount);
}



#endif