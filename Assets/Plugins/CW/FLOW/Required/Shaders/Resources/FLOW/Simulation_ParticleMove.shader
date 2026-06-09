Shader "Hidden/FLOW/Simulation_ParticleMove"
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
				float2   particleCoord = SnapCoord(i.uv, _PartCountXY);
				Particle particle      = GetParticle(particleCoord);
				float2   columnPixel   = mul(_FlowMatrix, float4(particle.Position, 1.0f));
				float2   columnCoord   = CoordFromPixel(columnPixel, _FlowCountXZ);
				float    fluidHeight   = GetColumnFluidHeight(columnCoord);

				if (particle.Age >= particle.Life || particle.Position.y < fluidHeight)
				{
					float2 center = abs(columnCoord - 0.5f);

					if (max(center.x, center.y) <= 0.5f)
					{
						particle.Life = 0.0f;
					}
				}

				particle.Position += particle.Velocity * _FlowDelta;

				return EncodeParticleB(particle);
			}
			ENDCG
		}
	}
}