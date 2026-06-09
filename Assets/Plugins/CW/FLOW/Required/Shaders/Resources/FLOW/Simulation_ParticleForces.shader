Shader "Hidden/FLOW/Simulation_ParticleForces"
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

			float  _FlowDelta;
			float3 _PartGravity;
			float  _PartDrag;

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

				if (particle.Life > 0.0f)
				{
					particle.Age += _FlowDelta;
				}
				else
				{
					particle.Age = 0.0f;
				}

				particle.Velocity += _PartGravity;
				particle.Velocity *= _PartDrag;

				return EncodeParticleA(particle);
			}
			ENDCG
		}
	}
}