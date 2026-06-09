Shader "Hidden/FLOW/Reader_Sample"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // VelocityXZ, GroundHeight, WetHeight
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Reader.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 coordOffset = (_ReaderPixel % 1.0f) / _FlowCountXZ;
				float2 columnCoord = SnapCoordFromPixel(_ReaderPixel, _FlowCountXZ);
				float2 smoothCoord = columnCoord + coordOffset;
				Column column      = GetColumn(smoothCoord);
				float4 outflow     = GetOutflow(smoothCoord);
				float2 velocity    = (outflow.yw - outflow.xz) * _FlowSpeed;

				return float4(velocity, column.GroundHeight, column.WetHeight);
			}
			ENDCG
		}

		Pass // NormalXYZ, Depth
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Reader.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 coordOffset = (_ReaderPixel % 1.0f) / _FlowCountXZ;
				float2 columnCoord = SnapCoordFromPixel(_ReaderPixel, _FlowCountXZ);
				float2 smoothCoord = columnCoord + coordOffset;
				float  depth       = GetColumnFluidDepth(smoothCoord);
				float  hpdL        = GetColumnFluidHeight(smoothCoord - _FlowCoordU000);
				float  hpdR        = GetColumnFluidHeight(smoothCoord + _FlowCoordU000);
				float  hpdB        = GetColumnFluidHeight(smoothCoord - _FlowCoord0V00);
				float  hpdT        = GetColumnFluidHeight(smoothCoord + _FlowCoord0V00);
				float3 normal      = normalize(float3(hpdL - hpdR, 2.0f, hpdB - hpdT));

				return float4(normal, depth);
			}
			ENDCG
		}

		Pass // RGBA
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Reader.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 coordOffset = (_ReaderPixel % 1.0f) / _FlowCountXZ;
				float2 columnCoord = SnapCoordFromPixel(_ReaderPixel, _FlowCountXZ);
				float2 smoothCoord = columnCoord + coordOffset;
				Fluid  fluid       = GetColumnFluid(smoothCoord);

				return fluid.RGBA;
			}
			ENDCG
		}

		Pass // ESMV
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Reader.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 coordOffset = (_ReaderPixel % 1.0f) / _FlowCountXZ;
				float2 columnCoord = SnapCoordFromPixel(_ReaderPixel, _FlowCountXZ);
				float2 smoothCoord = columnCoord + coordOffset;
				Fluid  fluid       = GetColumnFluid(smoothCoord);

				return fluid.ESMV;
			}
			ENDCG
		}

		Pass // F123
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Reader.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 coordOffset = (_ReaderPixel % 1.0f) / _FlowCountXZ;
				float2 columnCoord = SnapCoordFromPixel(_ReaderPixel, _FlowCountXZ);
				float2 smoothCoord = columnCoord + coordOffset;
				Fluid  fluid       = GetColumnFluid(smoothCoord);

				return fluid.F123;
			}
			ENDCG
		}
	}
}