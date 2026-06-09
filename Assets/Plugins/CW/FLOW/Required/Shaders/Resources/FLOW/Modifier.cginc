#include "Shared.cginc"
#include "UnityCG.cginc"

float4x4  _ModifierMatrix;
float4x4  _ModifierInverse;
float4    _ModifierRGBA;
float4    _ModifierChannels;
float4    _ModifierChannel;
float4    _ModifierESMV;
float4    _ModifierF123;
float     _ModifierStrength;
float     _ModifierAngle;
sampler2D _ModifierShape;
sampler2D _ModifierBuffer;
sampler2D _ModifierNormal;

struct appdata
{
	float4 vertex : POSITION;
	float2 uv     : TEXCOORD0;
};

struct v2f
{
	float4 vertex : SV_POSITION;
	float4 uv     : TEXCOORD0;
	float3 wpos   : TEXCOORD1;
};

float3 RotateNormal(float3 v, float a)
{
	float s = sin(a); float c = cos(a); return float3(v.x * c - v.y * s, v.x * s + v.y * c, v.z);
}

float GetShape(float2 uv)
{
	float4 shape = tex2D(_ModifierShape, uv);

	return dot(shape, _ModifierChannel);
}

float GetShape(float2 uv, float3 wpos)
{
	float  shape = GetShape(uv);
	float4 lpos  = mul(_ModifierInverse, float4(wpos, 1.0f));

	shape *= saturate((1.0f - abs(lpos.y)) * 10.0f);

	return shape;
}

void vert(appdata v, out v2f o)
{
	float2 shapeCoord  = v.uv * 1.00003f;
	float4 worldPos    = mul(_ModifierMatrix, float4(shapeCoord.x, 0.0f, shapeCoord.y, 1.0f));
	float4 columnPixel = mul(_FlowMatrix, worldPos);
	float2 columnCoord = CoordFromPixel(columnPixel, _FlowCountXZ);

	o.vertex = float4(columnCoord * 2.0f - 1.0f, 0.5f, 1.0f);
	o.uv     = float4(shapeCoord, columnCoord);
	o.wpos   = worldPos.xyz;

#if UNITY_UV_STARTS_AT_TOP
	o.vertex.y = -o.vertex.y;
#endif
}

void ContributeFluidModifier(inout Fluid fluid, Fluid inflowFluid)
{
	if (inflowFluid.Depth > 0.0f)
	{
		inflowFluid.RGBA = inflowFluid.Depth * _ModifierRGBA;
		inflowFluid.ESMV = inflowFluid.Depth * _ModifierESMV;
		inflowFluid.F123 = inflowFluid.Depth * _ModifierF123;

		ContributeFluid(fluid, inflowFluid, fluid.Depth + inflowFluid.Depth);

		fluid.Depth += inflowFluid.Depth;
	}
}

float4 Lerp255(float4 a, float4 b, float4 strength)
{
	float4 change  = (b - a) * abs(sign(strength));
	float4 maximum = abs(change);

	return a + sign(change) * clamp(maximum * strength, 1.0f / 255.0f, maximum);
}