#include "Shared.cginc"
#include "UnityCG.cginc"

float2    _ReaderBufferSize;
float2    _ReaderBufferPixel;
float2    _ReaderPixel;
float2    _ReaderRangeY; // Min, Max
float4    _ReaderShapeChannel;
float4x4  _ReaderMatrix;
float4x4  _ReaderShapeMatrix;
float4    _ReaderShapePlane;
sampler2D _ReaderShapeTexture;

struct appdata
{
	float4 vertex : POSITION;
	float2 uv     : TEXCOORD0;
};

struct v2f
{
	float4 vertex : SV_POSITION;
};

float GetShape(float3 wpos)
{
	// Vertically intercept wpos against shape plane
	float ray = 0.0f - dot(wpos.xyz, _ReaderShapePlane.xyz) - _ReaderShapePlane.w;
	wpos.y += ray / _ReaderShapePlane.y;

	float2 shapeUV = mul(_ReaderShapeMatrix, float4(wpos, 1.0f)).xz;
	float4 shape   = tex2D(_ReaderShapeTexture, shapeUV);
	float2 edges   = abs(shapeUV - 0.5f);

	shape *= max(edges.x, edges.y) <= 0.5f;

	return dot(shape, _ReaderShapeChannel);
}

void vert(appdata v, out v2f o)
{
	float2 bufferCoord = (_ReaderBufferPixel + v.uv.xy) / _ReaderBufferSize;

	o.vertex = float4(bufferCoord * 2.0f - 1.0f, 0.5f, 1.0f);

#if UNITY_UV_STARTS_AT_TOP
	o.vertex.y = -o.vertex.y;
#endif
}