Shader "Hidden/FLOW/Modifier_Copy"
{
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#include "Modifier.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float2 columnCoord = SnapCoord(i.uv.zw, _FlowCountXZ);

				return tex2D(_ModifierBuffer, columnCoord);
			}
			ENDCG
		}
	}
}