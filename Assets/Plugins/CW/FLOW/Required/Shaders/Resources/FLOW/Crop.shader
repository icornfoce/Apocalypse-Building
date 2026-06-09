Shader "Hidden/FLOW/Crop"
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
			#pragma vertex   Vert
			#pragma fragment Frag

			float2    _CropOffset;
			float2    _CropScale;
			float2    _CropSize;
			sampler2D _CropTexture;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			float2 SnapToPixel(float2 coord, float2 size)
			{
				float2 pixel = floor(coord * size);
#ifndef UNITY_HALF_TEXEL_OFFSET
				pixel += 0.5f;
#endif
				return pixel / size;
			}

			float4 SampleMip0(sampler2D s, float2 coord)
			{
				return tex2Dbias(s, float4(coord.x, coord.y, 0, -15.0));
			}

			void Vert(appdata v, out v2f o)
			{
				float2 pos = _CropOffset + _CropScale * v.uv;

				o.vertex   = float4(pos * 2.0f - 1.0f, 0.5f, 1.0f);
				o.texcoord = pos;
#if UNITY_UV_STARTS_AT_TOP
				o.vertex.y = -o.vertex.y;
#endif
			}

			float4 Frag(v2f i) : SV_Target
			{
				return SampleMip0(_CropTexture, SnapToPixel(i.texcoord, _CropSize));
			}
			ENDCG
		}
	}
}