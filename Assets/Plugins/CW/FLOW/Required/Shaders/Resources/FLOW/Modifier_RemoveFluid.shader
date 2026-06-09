Shader "Hidden/FLOW/Modifier_RemoveFluid"
{
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // RemoveFluid
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

				fluid.Depth = max(fluid.Depth - shape * _ModifierStrength, 0.0f);

				return float4(fluid.Depth, 0.0f, 0.0f, 0.0f);
			}
			ENDCG
		}

		Pass // RemoveFluidClip
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#include "Modifier.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 columnCoord = SnapCoord(i.uv.zw, _FlowCountXZ);
				Column column = GetColumn(columnCoord);
				Fluid  fluid = GetColumnFluid(columnCoord);
				float  shape = GetShape(i.uv.xy);

				fluid.Depth = max(fluid.Depth - shape * _ModifierStrength * (i.wpos.y > column.GroundHeight), 0.0f);

				return float4(fluid.Depth, 0.0f, 0.0f, 0.0f);
			}
			ENDCG
		}

		Pass // RemoveFluidAbove
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
				float  shape       = GetShape(i.uv.xy);
				float  maxRemoval  = clamp(fluidHeight - i.wpos.y, 0.0f, fluid.Depth);
				float  removal     = min(shape * _ModifierStrength, maxRemoval);

				fluid.Depth = max(fluid.Depth - removal, 0.0f);

				return float4(fluid.Depth, 0.0f, 0.0f, 0.0f);
			}
			ENDCG
		}

		Pass // RemoveFluidAboveClip
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
				float  shape       = GetShape(i.uv.xy);
				float  maxRemoval  = clamp((fluidHeight - i.wpos.y) * (i.wpos.y > column.GroundHeight), 0.0f, fluid.Depth);
				float  removal     = min(shape * _ModifierStrength, maxRemoval);

				fluid.Depth = max(fluid.Depth - removal, 0.0f);

				return float4(fluid.Depth, 0.0f, 0.0f, 0.0f);
			}
			ENDCG
		}
	}
}