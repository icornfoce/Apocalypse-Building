Shader "Hidden/FLOW/Modifier_AddFluid"
{
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // AddFluid
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#include "Modifier.cginc"

			FluidData frag(v2f i)
			{
				float2 columnCoord = SnapCoord(i.uv.zw, _FlowCountXZ);
				float  shape       = GetShape(i.uv.xy);
				Fluid  fluid       = GetColumnFluid(columnCoord);

				// Contribute
				Fluid inflowFluid;
				inflowFluid.Depth = _ModifierStrength * shape;
				ContributeFluidModifier(fluid, inflowFluid);

				return EncodeFluid(fluid);
			}
			ENDCG
		}

		Pass // AddFluidClip
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#include "Modifier.cginc"

			FluidData frag(v2f i)
			{
				float2 columnCoord = SnapCoord(i.uv.zw, _FlowCountXZ);
				float  shape       = GetShape(i.uv.xy);
				Column column      = GetColumn(columnCoord);
				Fluid  fluid       = GetColumnFluid(columnCoord);

				// Contribute
				Fluid inflowFluid;
				inflowFluid.Depth = _ModifierStrength * shape * (i.wpos.y > column.GroundHeight);
				ContributeFluidModifier(fluid, inflowFluid);

				return EncodeFluid(fluid);
			}
			ENDCG
		}

		Pass // AddFluidClipInv
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#include "Modifier.cginc"

			FluidData frag(v2f i)
			{
				float2 columnCoord = SnapCoord(i.uv.zw, _FlowCountXZ);
				float  shape       = GetShape(i.uv.xy);
				Column column      = GetColumn(columnCoord);
				Fluid  fluid       = GetColumnFluid(columnCoord);

				// Contribute
				Fluid inflowFluid;
				inflowFluid.Depth = _ModifierStrength * shape * (i.wpos.y < column.GroundHeight);
				ContributeFluidModifier(fluid, inflowFluid);

				return EncodeFluid(fluid);
			}
			ENDCG
		}

		// AddFluidBelow
		Pass
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#include "Modifier.cginc"

			FluidData frag(v2f i)
			{
				float2 columnCoord = SnapCoord(i.uv.zw, _FlowCountXZ);
				float  shape       = GetShape(i.uv.xy);
				Column column      = GetColumn(columnCoord);
				Fluid  fluid       = GetColumnFluid(columnCoord);
				float  fluidHeight = column.GroundHeight + fluid.Depth;
				float  maxContrib  = max(0.0f, i.wpos.y - fluidHeight);

				// Contribute
				Fluid  inflowFluid;
				inflowFluid.Depth = min(shape * _ModifierStrength, maxContrib);
				ContributeFluidModifier(fluid, inflowFluid);

				return EncodeFluid(fluid);
			}
			ENDCG
		}
	}
}