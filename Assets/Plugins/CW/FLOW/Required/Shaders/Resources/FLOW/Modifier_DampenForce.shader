Shader "Hidden/FLOW/Modifier_DampenForce"
{
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // DampenForce
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#include "Modifier.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 columnCoord = SnapCoord(i.uv.zw, _FlowCountXZ);
				Column column      = GetColumn(columnCoord);
				Fluid  fluid       = GetColumnFluid(columnCoord);
				float  shape       = GetShape(i.uv.xy);
				float4 outflow     = GetOutflow(columnCoord);

				outflow *= saturate((1.0f - shape) * _ModifierStrength);

				return outflow;
			}
			ENDCG
		}

		Pass // RangeDampenForce
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#include "Modifier.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 columnCoord = SnapCoord(i.uv.zw, _FlowCountXZ);
				Column column      = GetColumn(columnCoord);
				Fluid  fluid       = GetColumnFluid(columnCoord);
				float  fluidHeight = column.GroundHeight + fluid.Depth;
				float3 fluidPos    = float3(i.wpos.x, fluidHeight, i.wpos.z);
				float  shape       = GetShape(i.uv.xy, fluidPos);
				float4 outflow     = GetOutflow(columnCoord);

				outflow *= saturate((1.0f - shape) * _ModifierStrength);

				return outflow;
			}
			ENDCG
		}
	}
}