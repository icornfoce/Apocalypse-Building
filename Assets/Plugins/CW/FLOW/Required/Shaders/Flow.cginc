float2 CoordFromPixel(float2 pixel, float2 size)
{
#ifndef UNITY_HALF_TEXEL_OFFSET
	pixel += 0.5f;
#endif
	return pixel / size;
}

float2 SnapCoordFromPixel(float2 pixel, float2 size)
{
	return CoordFromPixel(floor(pixel), size);
}

float2 SnapCoord(float2 coord, float2 size)
{
	return SnapCoordFromPixel(coord * size, size);
}

// Fluid
struct Fluid
{
	float  Depth;
	float4 RGBA;
	float4 ESMV;
	float4 F123;
};

struct FluidData
{
	float  c : SV_Target0;
	float4 d : SV_Target1;
	float4 e : SV_Target2;
	float4 f : SV_Target3;
};

Fluid DecodeFluid(float4 c, float4 d, float4 e, float4 f)
{
	Fluid o;

	o.Depth = c.x;
	o.RGBA  = d;
	o.ESMV  = e;
	o.F123  = f;

	return o;
}

FluidData EncodeFluid(Fluid f)
{
	FluidData o;

	o.c = f.Depth;
	o.d = f.RGBA;
	o.e = f.ESMV;
	o.f = f.F123;

	return o;
}

void ContributeFluid(inout Fluid fluid, Fluid inflowFluid)
{
	float finalDepth = fluid.Depth + inflowFluid.Depth;
	float scaleDepth = max(finalDepth, 0.00001f);

	fluid.RGBA = (fluid.RGBA * fluid.Depth + inflowFluid.RGBA * inflowFluid.Depth) / scaleDepth;
	fluid.ESMV = (fluid.ESMV * fluid.Depth + inflowFluid.ESMV * inflowFluid.Depth) / scaleDepth;
	fluid.F123 = (fluid.F123 * fluid.Depth + inflowFluid.F123 * inflowFluid.Depth) / scaleDepth;

	fluid.Depth = finalDepth;
}

void ContributeFluid(inout Fluid fluid, Fluid inflowFluid, float totalFluid)
{
	fluid.RGBA = (fluid.RGBA * fluid.Depth + inflowFluid.RGBA) / totalFluid;
	fluid.ESMV = (fluid.ESMV * fluid.Depth + inflowFluid.ESMV) / totalFluid;
	fluid.F123 = (fluid.F123 * fluid.Depth + inflowFluid.F123) / totalFluid;
}

// Column
struct Column
{
	float  GroundHeight;
	float  WetHeight;
	float4 Outflow;
};

Column DecodeColumn(float4 packedA, float4 packedB)
{
	Column o;

	o.GroundHeight = packedA.x;
	o.WetHeight    = packedA.y;
	o.Outflow      = packedB;

	return o;
}

float4 EncodeColumnA(Column unpacked)
{
	return float4(unpacked.GroundHeight, unpacked.WetHeight, 0, 0);
}

// Particles
struct Particle
{
	float3 Velocity;
	float3 Position;
	float  Age;
	float  Life;
};

Particle DecodeParticle(float4 packedA, float4 packedB)
{
	Particle o;

	o.Velocity = packedA.xyz;
	o.Age      = packedA.w;
	o.Position = packedB.xyz;
	o.Life     = packedB.w;

	return o;
}

float4 EncodeParticleA(Particle unpacked)
{
	return float4(unpacked.Velocity, unpacked.Age);
}

float4 EncodeParticleB(Particle unpacked)
{
	return float4(unpacked.Position, unpacked.Life);
}