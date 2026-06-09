Shader "Hidden/FLOW/Modifier_AddFoam"
{
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // AddFoam
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

				fluid.F123.x += shape * _ModifierStrength;

				return fluid.F123;
			}
			ENDCG
		}

		Pass // AddFoamMax
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

				fluid.F123.x = max(fluid.F123.x, shape * _ModifierStrength);

				return fluid.F123;
			}
			ENDCG
		}

		Pass // RangeAddFoam
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

				fluid.F123.x += shape * _ModifierStrength;

				return fluid.F123;
			}
			ENDCG
		}

		Pass // RangeAddFoamMax
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

				fluid.F123.x = max(fluid.F123.x, shape * _ModifierStrength);

				return fluid.F123;
			}
			ENDCG
		}
	}
}