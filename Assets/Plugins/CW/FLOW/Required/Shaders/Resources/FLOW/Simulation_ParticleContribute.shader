Shader "Hidden/FLOW/Simulation_ParticleContribute"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // Write to buffer
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Simulation_ParticleContribute.cginc"

			FluidData frag(v2f i)
			{
				float2 columnCoord   = SnapCoord(i.uv.xy, _FlowCountXZ);
				float2 particleCoord = i.uv.zw;
				Fluid  particleFluid = GetParticleFluid(particleCoord);
				Fluid  columnFluid   = GetColumnFluid(columnCoord);

				ContributeFluid(columnFluid, particleFluid);

				return EncodeFluid(columnFluid);
			}
			ENDCG
		}

		Pass // Copy back
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Simulation_ParticleContribute.cginc"

			FluidData frag(v2f i)
			{
				float2 columnCoord   = SnapCoord(i.uv.xy, _FlowCountXZ);
				float2 particleCoord = i.uv.zw;
				Fluid  particleFluid = GetParticleFluid(particleCoord);
				Fluid  columnFluid   = GetColumnFluid(columnCoord);

				return EncodeFluid(columnFluid);
			}
			ENDCG
		}
	}
}