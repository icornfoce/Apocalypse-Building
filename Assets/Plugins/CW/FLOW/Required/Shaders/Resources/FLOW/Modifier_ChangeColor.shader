Shader "Hidden/FLOW/Modifier_ChangeColor"
{
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // ChangeColor
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

				fluid.RGBA = Lerp255(fluid.RGBA, _ModifierRGBA, saturate(shape * _ModifierStrength) * _ModifierChannels);

				return fluid.RGBA;
			}
			ENDCG
		}

		Pass // RangeChangeColor
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

				fluid.RGBA = Lerp255(fluid.RGBA, _ModifierRGBA, saturate(shape * _ModifierStrength) * _ModifierChannels);

				return fluid.RGBA;
			}
			ENDCG
		}
	}
}