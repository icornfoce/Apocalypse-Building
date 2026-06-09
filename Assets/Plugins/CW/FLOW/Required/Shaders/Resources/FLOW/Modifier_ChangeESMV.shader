Shader "Hidden/FLOW/Modifier_ChangeESMV"
{
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // ChangeESMV
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

				fluid.ESMV = Lerp255(fluid.ESMV, _ModifierESMV, saturate(shape * _ModifierStrength) * _ModifierChannels);

				return fluid.ESMV;
			}
			ENDCG
		}

		Pass // RangeChangeESMV
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

				fluid.ESMV = Lerp255(fluid.ESMV, _ModifierESMV, saturate(shape * _ModifierStrength) * _ModifierChannels);

				return fluid.ESMV;
			}
			ENDCG
		}
	}
}