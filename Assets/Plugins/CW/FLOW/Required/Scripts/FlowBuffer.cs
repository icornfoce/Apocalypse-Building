using UnityEngine;
using CW.Common;

namespace FLOW
{
	public class FlowBuffer
	{
		public RenderTexture[] SourceTextures;

		public RenderTexture[] TempTextures;

		public RenderBuffer[] ColorBuffers;

		public readonly int Count;

		public static FlowBuffer Size1 = new FlowBuffer(1);

		public static FlowBuffer Size4 = new FlowBuffer(4);

		public FlowBuffer(int size)
		{
			Count          = size;
			SourceTextures = new RenderTexture[size];
			TempTextures   = new RenderTexture[size];
			ColorBuffers   = new RenderBuffer[size];
		}

		public void Set(int index, RenderTexture source)
		{
			var temp = CwRenderTextureManager.GetTemporary(source.descriptor, "FlowBuffer");

			SourceTextures[index] = source;

			TempTextures[index] = temp;

			ColorBuffers[index] = temp.colorBuffer;
		}

		public void SetRenderTargets()
		{
			Graphics.SetRenderTarget(ColorBuffers, TempTextures[0].depthBuffer);
		}

		public void InvertAndSetRenderTargets()
		{
			for (var i = 0; i < Count; i++)
			{
				ColorBuffers[i] = SourceTextures[i].colorBuffer;
			}

			Graphics.SetRenderTarget(ColorBuffers, SourceTextures[0].depthBuffer);
		}

		public void ReleaseAll()
		{
			for (var i = 0; i < Count; i++)
			{
				CwRenderTextureManager.ReleaseTemporary(TempTextures[i]);

				SourceTextures[i] = null;
				TempTextures[i] = null;
				ColorBuffers[i] = default(RenderBuffer);
			}
		}
	}
}