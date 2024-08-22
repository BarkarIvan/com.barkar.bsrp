#ifndef OIT_UTILS_INCLUDED
#define OIT_UTILS_INCLUDED

#define MAX_SORTED_PIXELS 8

struct Fragment
{
   // uint colour;
    float4 testColor;
   // uint transmissionAndDepth;
    float testdepth;
    float testtransmission;
    uint next;
};
RWStructuredBuffer<Fragment> _FragmentLinksBuffer : register(u1);
RWByteAddressBuffer _StartOffsetBuffer : register(u2);

#endif
