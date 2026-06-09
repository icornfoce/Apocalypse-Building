Shader "Hidden/FLOW/Simulation_FluidTransport"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ PHYSICS_OVERFLOW

			#include "Shared.cginc"
			#include "UnityCG.cginc"

			float _FlowFoamClearRate;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv     : TEXCOORD0;
			};

			void vert(appdata v, out v2f o)
			{
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv     = v.uv;
			}

			Fluid GetInflowFluid(float2 uv, float4 edge, float height0)
			{
				float4 inflow;
				inflow.x = GetLimitedOutflow(uv - _FlowCoordU000).y;
				inflow.y = GetLimitedOutflow(uv + _FlowCoordU000).x;
				inflow.z = GetLimitedOutflow(uv - _FlowCoord0V00).w;
				inflow.w = GetLimitedOutflow(uv + _FlowCoord0V00).z;
				inflow *= edge;

				Fluid fluidL = GetColumnFluid(uv - _FlowCoordU000);
				Fluid fluidR = GetColumnFluid(uv + _FlowCoordU000);
				Fluid fluidB = GetColumnFluid(uv - _FlowCoord0V00);
				Fluid fluidT = GetColumnFluid(uv + _FlowCoord0V00);

				Column columnL = GetColumn(uv - _FlowCoordU000);
				Column columnR = GetColumn(uv + _FlowCoordU000);
				Column columnB = GetColumn(uv - _FlowCoord0V00);
				Column columnT = GetColumn(uv + _FlowCoord0V00);

				Fluid total;

				total.Depth = GetTotalFlow(inflow);
				total.RGBA  = fluidL.RGBA * inflow.x + fluidR.RGBA * inflow.y + fluidB.RGBA * inflow.z + fluidT.RGBA * inflow.w;
				total.ESMV  = fluidL.ESMV * inflow.x + fluidR.ESMV * inflow.y + fluidB.ESMV * inflow.z + fluidT.ESMV * inflow.w;
				total.F123  = fluidL.F123 * inflow.x + fluidR.F123 * inflow.y + fluidB.F123 * inflow.z + fluidT.F123 * inflow.w;

				// Increase foam if the inflow is 'falling' into this column
				float fallL = columnL.GroundHeight + fluidL.Depth;
				float fallR = columnR.GroundHeight + fluidR.Depth;
				float fallB = columnB.GroundHeight + fluidB.Depth;
				float fallT = columnT.GroundHeight + fluidT.Depth;
				float fallf = GetTotalFlow(abs(float4(fallL, fallR, fallB, fallT) - height0) * inflow);

				total.F123.x += fallf * 0.2f;

				total.F123.x = saturate(total.F123.x);

				return total;
			}

			FluidData frag(v2f i)
			{
				float2 columnCoord = SnapCoord(i.uv, _FlowCountXZ);
				Fluid  fluid       = GetColumnFluid(columnCoord);
				Column column      = GetColumn(columnCoord);
				float4 edge        = Boundary(columnCoord);
				float4 outflow     = GetLimitedOutflow(columnCoord) * edge;
				float  height0     = column.GroundHeight + fluid.Depth;

				// Reduce depth from outflow
				fluid.Depth -= GetTotalFlow(outflow);
				
				// May exceed limit due to floating point precision
				fluid.Depth = max(0.0f, fluid.Depth);

				// Mix in new fluid?
				Fluid inflowFluid = GetInflowFluid(columnCoord, edge, height0);

				if (inflowFluid.Depth > 0.0f)
				{
					ContributeFluid(fluid, inflowFluid, fluid.Depth + inflowFluid.Depth);

					fluid.Depth += inflowFluid.Depth;
				}

				// Fade out foam
				fluid.F123.x -= _FlowFoamClearRate;

				fluid.F123.x = saturate(fluid.F123.x);

				return EncodeFluid(fluid);
			}
			ENDCG
		}
	}
}