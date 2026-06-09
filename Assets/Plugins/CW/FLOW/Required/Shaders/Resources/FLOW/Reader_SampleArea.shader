Shader "Hidden/FLOW/Reader_SampleArea"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // Depth Total
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Reader.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float total = 0.0f;

				for (int y = _ReaderRangeY.x; y < _ReaderRangeY.y; y++)
				{
					float2 samplePixel = float2(_ReaderPixel.x, y);
					float2 coordOffset = (samplePixel % 1.0f) / _FlowCountXZ;
					float2 columnCoord = SnapCoordFromPixel(samplePixel, _FlowCountXZ);
					float2 smoothCoord = columnCoord + coordOffset;

					total += GetColumnFluidDepth(smoothCoord);
				}

				return total;
			}
			ENDCG
		}

		Pass // Depth Total, Deepest Depth, Deepest Y, Deepest Height
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Reader.cginc"

			float4 frag(v2f i) : SV_Target
			{
				float4 result             = 0.0f;
				float2 deepestSmoothCoord = 0.0f;

				for (int y = _ReaderRangeY.x; y < _ReaderRangeY.y; y++)
				{
					float2 samplePixel = float2(_ReaderPixel.x, y);
					float2 coordOffset = (samplePixel % 1.0f) / _FlowCountXZ;
					float2 columnCoord = SnapCoordFromPixel(samplePixel, _FlowCountXZ);
					float2 smoothCoord = columnCoord + coordOffset;
					float3 wpos        = mul(_ReaderMatrix, float4(smoothCoord, 0.0f, 1.0f)).xyz;

					if (GetShape(wpos) >= 0.5f)
					{
						float depth = GetColumnFluidDepth(smoothCoord);

						result.x += depth;

						if (depth > result.y)
						{
							result.y = depth;
							result.z = y;

							deepestSmoothCoord = smoothCoord;
						}
					}
				}

				Column deepestColumn = GetColumn(deepestSmoothCoord);

				result.w = deepestColumn.GroundHeight;

				return result;
			}
			ENDCG
		}
	}
}