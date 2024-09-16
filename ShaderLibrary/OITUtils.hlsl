#ifndef OIT_UTILS_INCLUDED
#define OIT_UTILS_INCLUDED

#define MAX_SORTED_PIXELS 8

struct Fragment
{
    uint color;
    uint depth;
    uint next;
};


RWStructuredBuffer<Fragment> _FragmentLinksBuffer : register(u1);
RWByteAddressBuffer _StartOffsetBuffer : register(u2);


#endif
