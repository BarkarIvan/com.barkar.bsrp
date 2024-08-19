#ifndef OIT_UTILS_INCLUDED
#define OIT_UTILS_INCLUDED

//TODO include
struct Fragment
{
    uint colour;
    uint transmissionAndDepth;
    uint next;
};

RWStructuredBuffer<Fragment> _FragmentLinksBuffer : register(u1);
RWByteAddressBuffer _StartOffsetBuffer : register(u2);
////


#endif
