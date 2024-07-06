#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"




#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_I_VP unity_MatrixIVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
	#define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

bool IsOrthographicCamera()
{
	return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear(float rawDepth)
{
	#if UNITY_REVERSED_Z
		rawDepth = 1.0 - rawDepth;
	#endif
	return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth +
		_ProjectionParams.y;
}

//#include "Fragment.hlsl"

float Square(float x)
{
	return x * x;
}

float DistanceSquared(float3 pA, float3 pB)
{
	return dot(pA - pB, pA - pB);
}


float3 DecodeNormal(float4 sample, float scale)
{
	#if defined(UNITY_NO_DXT5nm)
	    return normalize(UnpackNormalRGB(sample, scale));
	#else
	    return normalize(UnpackNormalmapRGorAG(sample, scale));
	#endif
}

float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
	float3x3 tangentToWorld =
		CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
	return TransformTangentToWorld(normalTS, tangentToWorld);
}

//CORE

#if UNITY_REVERSED_Z
// TODO: workaround. There's a bug where SHADER_API_GL_CORE gets erroneously defined on switch.
#if (defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
//GL with reversed z => z clip range is [near, -far] -> remapping to [0, far]
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max((coord - _ProjectionParams.y)/(-_ProjectionParams.z-_ProjectionParams.y)*_ProjectionParams.z, 0)
#else
//D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
//max is required to protect ourselves from near plane not being correct/meaningful in case of oblique matrices.
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
#endif
#elif UNITY_UV_STARTS_AT_TOP
//D3d without reversed z => z clip range is [0, far] -> nothing to do
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
//Opengl => z clip range is [-near, far] -> remapping to [0, far]
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((coord + _ProjectionParams.y)/(_ProjectionParams.z+_ProjectionParams.y))*_ProjectionParams.z, 0)
#endif


//VARIABLES



//TEXTURECUBE(unity_SpecCube0);
//SAMPLER(sampler_unity_SpecCube0);
//TEXTURECUBE(unity_SpecCube1);

struct VertexPositionInputs
{
	float3 positionWS; // World space position
	float3 positionVS; // View space position
	float4 positionCS; // Homogeneous clip space position
	float4 positionNDC;// Homogeneous normalized device coordinates
};

struct VertexNormalInputs
{
	real3 tangentWS;
	real3 bitangentWS;
	float3 normalWS;
};

VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
{
	VertexPositionInputs input;
	input.positionWS = TransformObjectToWorld(positionOS);
	input.positionVS = TransformWorldToView(input.positionWS);
	input.positionCS = TransformWorldToHClip(input.positionWS);

	float4 ndc = input.positionCS * 0.5f;
	input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
	input.positionNDC.zw = input.positionCS.zw;

	return input;
}


VertexNormalInputs GetVertexNormalInputs(float3 normalOS)
{
	VertexNormalInputs tbn;
	tbn.tangentWS = real3(1.0, 0.0, 0.0);
	tbn.bitangentWS = real3(0.0, 1.0, 0.0);
	tbn.normalWS = TransformObjectToWorldNormal(normalOS);
	return tbn;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS, float4 tangentOS)
{
	VertexNormalInputs tbn;

	// mikkts space compliant. only normalize when extracting normal at frag.
	real sign = real(tangentOS.w) * GetOddNegativeScale();
	tbn.normalWS = TransformObjectToWorldNormal(normalOS);
	tbn.tangentWS = real3(TransformObjectToWorldDir(tangentOS.xyz));
	tbn.bitangentWS = real3(cross(tbn.normalWS, float3(tbn.tangentWS))) * sign;
	return tbn;
}


real ComputeFogFactorZ0ToFar(float z)
{
	#if defined(FOG_LINEAR)
	// factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
	float fogFactor = saturate(z * unity_FogParams.z + unity_FogParams.w);
	return real(fogFactor);
	#elif defined(FOG_EXP) || defined(FOG_EXP2)
	// factor = exp(-(density*z)^2)
	// -density * z computed at vertex
	return real(unity_FogParams.x * z);
	#else
	return real(0.0);
	#endif
}

real ComputeFogFactor(float zPositionCS)
{
	float clipZ_0Far = UNITY_Z_0_FAR_FROM_CLIPSPACE(zPositionCS);
	return ComputeFogFactorZ0ToFar(clipZ_0Far);
}

half ComputeFogIntensity(half fogFactor)
{
	half fogIntensity = half(0.0);
	#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
	#if defined(FOG_EXP)
	// factor = exp(-density*z)
	// fogFactor = density*z compute at vertex
	fogIntensity = saturate(exp2(-fogFactor));
	#elif defined(FOG_EXP2)
	// factor = exp(-(density*z)^2)
	// fogFactor = density*z compute at vertex
	fogIntensity = saturate(exp2(-fogFactor * fogFactor));
	#elif defined(FOG_LINEAR)
	fogIntensity = fogFactor;
	#endif
	#endif
	return fogIntensity;
}


#endif
