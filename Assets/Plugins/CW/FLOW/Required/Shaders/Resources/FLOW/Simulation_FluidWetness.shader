Shader "Hidden/FLOW/Simulation_FluidWetness"
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

			#include "Shared.cginc"
			#include "UnityCG.cginc"

			float _FlowTableDepth;
			float _FlowDryRate;
			float _FlowDelta;

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

			float4 frag(v2f i) : SV_Target
			{
				float2 columnCoord = SnapCoord(i.uv, _FlowCountXZ);
				Column column      = GetColumn(columnCoord);
				Fluid  fluid       = GetColumnFluid(columnCoord);
				//float  waterHeight = lerp(column.GroundHeight - 1.0f, column.GroundHeight + fluid.Depth, saturate(fluid.Depth * 10.0f));

				column.WetHeight -= _FlowDryRate * _FlowDelta;

				float waterTable = fluid.Depth - _FlowTableDepth * saturate(1.0f - fluid.Depth * 10.0f);

				column.WetHeight = max(column.WetHeight, waterTable);

				return EncodeColumnA(column);
			}
			ENDCG
		}
	}
}