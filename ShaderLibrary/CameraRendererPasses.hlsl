#ifndef CUSTOM_CAMERA_RENDERER_PASSES_INCLUDED
#define CUSTOM_CAMERA_RENDERER_PASSES_INCLUDED

TEXTURE2D(_SourceTexture);



 struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
};

static const float4 positions[3] = {
	float4(-1.0, -1.0, 0.0, 1.0),
	float4(-1.0,  3.0, 0.0, 1.0),
	float4( 3.0, -1.0, 0.0, 1.0)
};

static const float2 uvs[3] = {
	float2(0.0, 0.0),
	float2(0.0, 2.0),
	float2(2.0, 0.0)
};

Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
	Varyings OUT;
	OUT.positionCS = positions[vertexID];
	OUT.uv = uvs[vertexID];
	
	
	if (_ProjectionParams.x < 0.0)
	{
		OUT.uv.y = 1.0 - OUT.uv.y;
	}
	
	return OUT;
}

float4 CopyPassFragment(Varyings IN) : SV_TARGET
{
	return SAMPLE_TEXTURE2D(
		_SourceTexture, sampler_linear_clamp, IN.uv);
}

float CopyDepthPassFragment(Varyings IN) : SV_DEPTH
{
	return SAMPLE_DEPTH_TEXTURE(
		_SourceTexture, sampler_point_clamp, IN.uv);
}

#endif
