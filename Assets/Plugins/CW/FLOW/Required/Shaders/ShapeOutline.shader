Shader "Hidden/FLOW/ShapeOutline"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex   Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv     : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _FlowShapeTex;
			float4    _FlowShapeChannel;

			void Vert (in appdata i, out v2f o)
			{
				o.vertex = UnityObjectToClipPos(i.vertex);
				o.uv     = i.uv;
			}

			fixed4 Frag (v2f i) : SV_Target
			{
				float shape = dot(tex2D(_FlowShapeTex, i.uv), _FlowShapeChannel);
				float side  = shape + abs(ddx(shape)) + abs(ddy(shape));

				if (shape >= 0.5f || side < 0.5f)
				{
					discard;
				}

				return 1.0f;
			}
			ENDCG
		}
	}
}
