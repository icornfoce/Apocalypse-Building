#include "../../Flow.cginc"

sampler2D_float _FlowDataA;
sampler2D_float _FlowDataB;
sampler2D_float _FlowDataC;
sampler2D_float _FlowDataD;
sampler2D_float _FlowDataE;
sampler2D_float _FlowDataF;

float    _FlowResolution;
float    _FlowSpeed;
float4   _FlowCoordU000;
float4   _FlowCoord0V00;
float4   _FlowCoordUV00;
float2   _FlowCountXZ;
float4x4 _FlowMatrix;
float2   _FlowSeparationXZ;

float GetTotalFlow(float4 flow)
{
	return dot(flow, 1);
}

float4 LimitOutflow(float4 outflow, float limit)
{
	outflow = max(0.0f, outflow);
	outflow *= min(1.0f, limit / (GetTotalFlow(outflow) + 0.0001f));

	return outflow;
}

Column GetColumn(float2 uv)
{
	return DecodeColumn(tex2Dlod(_FlowDataA, float4(uv, 0, 0)), tex2Dlod(_FlowDataB, float4(uv, 0, 0)));
}

float GetHeight(float2 uv)
{
	return tex2Dlod(_FlowDataA, float4(uv, 0, 0)).r;
}

float2 GetHeightAndDepth(float2 uv)
{
	float  groundHeight = tex2Dlod(_FlowDataA, float4(uv, 0, 0)).r;
	float  fluidDepth   = tex2Dlod(_FlowDataC, float4(uv, 0, 0)).r;

	return float2(groundHeight, fluidDepth);
}

float GetColumnFluidDepth(float2 uv)
{
	return tex2Dlod(_FlowDataC, float4(uv, 0, 0)).r;
}

float GetColumnFluidHeight(float2 uv)
{
	float  groundHeight = tex2Dlod(_FlowDataA, float4(uv, 0, 0)).r;
	float  fluidDepth   = tex2Dlod(_FlowDataC, float4(uv, 0, 0)).r;

	return groundHeight + fluidDepth;
}

Fluid GetColumnFluid(float2 uv)
{
	float4 c = tex2Dlod(_FlowDataC, float4(uv, 0, 0));
	float4 d = tex2Dlod(_FlowDataD, float4(uv, 0, 0));
	float4 e = tex2Dlod(_FlowDataE, float4(uv, 0, 0));
	float4 f = tex2Dlod(_FlowDataF, float4(uv, 0, 0));
	return DecodeFluid(c, d, e, f);
}

float4 GetOutflow(float2 uv)
{
	return tex2Dlod(_FlowDataB, float4(uv, 0, 0));
}

float4 GetLimitedOutflow(float2 uv)
{
	float4 outflow = GetOutflow(uv);

	#if PHYSICS_OVERFLOW
		float fluidDepth = tex2Dlod(_FlowDataC, float4(uv, 0, 0)).r;

		outflow = LimitOutflow(outflow, fluidDepth);
	#endif

	return outflow;
}

float4 Boundary(float2 uv)
{
	float4 flow;

	flow.xz = (uv - _FlowCoordUV00) > 0.0f;
	flow.yw = (uv + _FlowCoordUV00) < 1.0f;

	return flow;
}

/*
float4 LimitOutflow(float4 outflow, float limit)
{
	outflow = max(0.0f, outflow);

	float total = GetTotalFlow(outflow);

	if (total > limit)
	{
		outflow *= limit / total;
	}

	return outflow;
}
*/

// Particles
sampler2D_float _PartDataA;
sampler2D_float _PartDataB;
sampler2D_float _PartDataC;
sampler2D_float _PartDataD;
sampler2D_float _PartDataE;
sampler2D_float _PartDataF;

float2 _PartCoordUV;
float2 _PartCountXY;

Particle GetParticle(float2 uv)
{
	return DecodeParticle(tex2Dlod(_PartDataA, float4(uv, 0, 0)), tex2Dlod(_PartDataB, float4(uv, 0, 0)));
}

Fluid GetParticleFluid(float2 uv)
{
	float4 c = tex2Dlod(_PartDataC, float4(uv, 0, 0));
	float4 d = tex2Dlod(_PartDataD, float4(uv, 0, 0));
	float4 e = tex2Dlod(_PartDataE, float4(uv, 0, 0));
	float4 f = tex2Dlod(_PartDataF, float4(uv, 0, 0));
	return DecodeFluid(c, d, e, f);
}