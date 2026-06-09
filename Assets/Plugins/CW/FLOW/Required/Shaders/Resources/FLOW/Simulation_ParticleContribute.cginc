#include "Shared.cginc"
#include "UnityCG.cginc"

struct appdata
{
	float4 vertex : POSITION;
	float4 uv     : TEXCOORD0;
};

struct v2f
{
	float4 vertex : SV_POSITION;
	float4 uv     : TEXCOORD0;
};

void vert(appdata v, out v2f o)
{
	float2   particleCoord = CoordFromPixel(v.uv.xy, _PartCountXY);
	Particle particle = GetParticle(particleCoord);
	float2   columnPixel = mul(_FlowMatrix, float4(particle.Position, 1.0f));
	float2   columnCoord = SnapCoordFromPixel(columnPixel, _FlowCountXZ) + _PartCoordUV * v.uv.zw;

	columnCoord *= particle.Age > particle.Life;

	o.vertex = float4(columnCoord * 2.0f - 1.0f, 0.5f, 1.0f);
	o.uv = float4(columnCoord, particleCoord);
#if UNITY_UV_STARTS_AT_TOP
	o.vertex.y = -o.vertex.y;
#endif
}