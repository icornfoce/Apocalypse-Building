Shader "Hidden/FLOW/Modifier_AddForce"
{
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // AddForce
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
				float2 flow        = UnpackNormal(tex2D(_ModifierNormal, i.uv.xy)) * shape * _ModifierStrength;

				outflow.r += max(0, -flow.x);
				outflow.g += max(0,  flow.x);
				outflow.b += max(0, -flow.y);
				outflow.a += max(0,  flow.y);

				return outflow;
				//return LimitOutflow(outflow, fluid.Depth);
			}
			ENDCG
		}

		Pass // AddForceUniform
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
				float2 flow        = float2(sin(_ModifierAngle), cos(_ModifierAngle)) * shape * _ModifierStrength;

				outflow.r += max(0, -flow.x);
				outflow.g += max(0,  flow.x);
				outflow.b += max(0, -flow.y);
				outflow.a += max(0,  flow.y);

				return outflow;
				//return LimitOutflow(outflow, fluid.Depth);
			}
			ENDCG
		}

		Pass // RangeAddForce
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
				float2 flow        = UnpackNormal(tex2D(_ModifierNormal, i.uv.xy)) * shape * _ModifierStrength;

				outflow.r += max(0, -flow.x);
				outflow.g += max(0,  flow.x);
				outflow.b += max(0, -flow.y);
				outflow.a += max(0,  flow.y);

				return outflow;
				//return LimitOutflow(outflow, fluid.Depth);
			}
			ENDCG
		}

		Pass // RangeAddForceUniform
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
				float2 flow        = float2(sin(_ModifierAngle), cos(_ModifierAngle)) * shape * _ModifierStrength;

				outflow.r += max(0, -flow.x);
				outflow.g += max(0,  flow.x);
				outflow.b += max(0, -flow.y);
				outflow.a += max(0,  flow.y);

				return outflow;
				//return LimitOutflow(outflow, fluid.Depth);
			}
			ENDCG
		}
	}
}