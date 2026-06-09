using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This class stores a snapshot of a fluid simulation.
	/// The snapshot can be stored in three different formats: Raw (RenderTexture), Readable (Texture2D), and Serializable (byte[]).
	/// The fluid simulation itself is stored using RenderTextures, so the raw format is the fastest storage method to use. However, it's not possible to directly read/write RenderTextures, and they cannot be directly saved/loaded.
	/// The readable data is stored using Texture2D, which you can directly read/write, but it cannot be directly saved/loaded.
	/// The serializable data is stored using PNG encoded bytes, which you cannot directly read/write, but you can directly save/load them.</summary>
	[System.Serializable]
	public class FlowSnapshotData
	{
		[System.Serializable]
		public class TextureData
		{
			[System.NonSerialized]
			public RenderTexture RawData;

			[System.NonSerialized]
			public Texture2D ReadableData;

			[SerializeField]
			public byte[] SerializableData;

			[SerializeField]
			public RenderTextureFormat SerializableFormat;

			[SerializeField]
			public Vector2Int SerializableSize;

			public void ReleaseAll()
			{
				ReleaseReadable();
				ReleaseRaw();
			}

			public void ReleaseReadable()
			{
				if (ReadableData != null)
				{
					Object.DestroyImmediate(ReadableData);

					ReadableData = null;
				}
			}

			public void ReleaseRaw()
			{
				if (RawData != null)
				{
					CwRenderTextureManager.ReleaseTemporary(RawData);

					RawData = null;
				}
			}

			public void ConvertRawToReadable()
			{
				if (RawData != null)
				{
					UpdateReadable(new Vector2Int(RawData.width, RawData.height), GetFormat(RawData.format));

					CwHelper.BeginActive(RawData);
						ReadableData.ReadPixels(new Rect(0, 0, RawData.width, RawData.height), 0, 0);
					CwHelper.EndActive();

					ReadableData.Apply();
				}
			}

			public void ConvertReadableToSerializable()
			{
				if (ReadableData != null)
				{
					SerializableData   = ReadableData.GetRawTextureData();
					SerializableSize   = new Vector2Int(ReadableData.width, ReadableData.height);
					SerializableFormat = GetFormat(ReadableData.format);
				}
			}

			public void ConvertSerializableToReadable()
			{
				if (SerializableData != null && SerializableData.Length > 0)
				{
					UpdateReadable(SerializableSize, GetFormat(SerializableFormat));

					ReadableData.LoadRawTextureData(SerializableData);

					ReadableData.Apply();
				}
			}

			public void ConvertReadableToRaw()
			{
				if (ReadableData != null)
				{
					var format = GetFormat(ReadableData.format);

					if (RawData != null)
					{
						if (RawData.width != ReadableData.width || RawData.height != ReadableData.height || RawData.format != format)
						{
							ReleaseRaw();
						}
					}

					if (RawData == null)
					{
						var desc = new RenderTextureDescriptor(ReadableData.width, ReadableData.height, format, 0);

						RawData = CwRenderTextureManager.GetTemporary(desc, "FlowSnapshotData");
					}

					Graphics.Blit(ReadableData, RawData);
				}
			}

			private void UpdateReadable(Vector2Int size, TextureFormat format)
			{
				if (ReadableData != null)
				{
					if (ReadableData.format != format || ReadableData.width != size.x || ReadableData.height != size.y)
					{
						ReleaseReadable();
					}
				}

				if (ReadableData == null)
				{
					ReadableData = new Texture2D(size.x, size.y, format, false, true);
				}
			}
		}

        public TextureData CurrentFlowDataA;
		public TextureData CurrentFlowDataB;
		public TextureData CurrentFlowDataC;
		public TextureData CurrentFlowDataD;
		public TextureData CurrentFlowDataE;
		public TextureData CurrentFlowDataF;

		public TextureData CurrentParticleDataA;
		public TextureData CurrentParticleDataB;
		public TextureData CurrentParticleDataC;
		public TextureData CurrentParticleDataD;
		public TextureData CurrentParticleDataE;
		public TextureData CurrentParticleDataF;

		public void ConvertCompressedToReadable()
		{
			for (var i = 0; i < 11; i++)
			{
				GetData(i).ConvertSerializableToReadable();
			}
		}

		public void ConvertRawToReadable()
		{
			for (var i = 0; i < 11; i++)
			{
				GetData(i).ConvertRawToReadable();
			}
		}

		public void ConvertReadableToPng()
		{
			for (var i = 0; i < 11; i++)
			{
				GetData(i).ConvertReadableToSerializable();
			}
		}

		public void ConvertReadableToRaw()
		{
			for (var i = 0; i < 11; i++)
			{
				GetData(i).ConvertReadableToRaw();
			}
		}

		public void Release()
		{
			for (var i = 0; i < 11; i++)
			{
				GetData(i).ReleaseAll();
			}
		}

		public void ReleaseRaw()
		{
			for (var i = 0; i < 11; i++)
			{
				GetData(i).ReleaseRaw();
			}
		}

		public void ReleaseReadable()
		{
			for (var i = 0; i < 11; i++)
			{
				GetData(i).ReleaseReadable();
			}
		}

		private TextureData GetData(ref TextureData textureData)
		{
			if (textureData == null)
			{
				textureData = new TextureData();
			}

			return textureData;
		}
		
		private static TextureFormat GetFormat(RenderTextureFormat format)
		{
			switch (format)
			{
				case RenderTextureFormat.R8: return TextureFormat.R8;
				case RenderTextureFormat.RHalf: return TextureFormat.RHalf;
				case RenderTextureFormat.RFloat: return TextureFormat.RFloat;
				case RenderTextureFormat.RG16: return TextureFormat.RG16;
				case RenderTextureFormat.RGHalf: return TextureFormat.RGHalf;
				case RenderTextureFormat.RGFloat: return TextureFormat.RGFloat;
				case RenderTextureFormat.ARGB32: return TextureFormat.ARGB32;
				case RenderTextureFormat.ARGBHalf: return TextureFormat.RGBAHalf;
				case RenderTextureFormat.ARGBFloat: return TextureFormat.RGBAFloat;
			}

			return TextureFormat.RGBAFloat;
		}

		private static RenderTextureFormat GetFormat(TextureFormat format)
		{
			switch (format)
			{
				case TextureFormat.R8: return RenderTextureFormat.R8;
				case TextureFormat.RHalf: return RenderTextureFormat.RHalf;
				case TextureFormat.RFloat: return RenderTextureFormat.RFloat;
				case TextureFormat.RG16: return RenderTextureFormat.RG16;
				case TextureFormat.RGHalf: return RenderTextureFormat.RGHalf;
				case TextureFormat.RGFloat: return RenderTextureFormat.RGFloat;
				case TextureFormat.ARGB32: return RenderTextureFormat.ARGB32;
				case TextureFormat.RGBAHalf: return RenderTextureFormat.ARGBHalf;
				case TextureFormat.RGBAFloat: return RenderTextureFormat.ARGBFloat;
			}

			return RenderTextureFormat.ARGBFloat;
		}

		/// <summary>This method copies all fluid and particle data from the specified simulation into this class as raw data.</summary>
		public void SimulationToRaw(FlowSimulation simulation)
		{
			if (simulation != null)
			{
				Read(simulation.CurrentFlowDataA, ref CurrentFlowDataA);
				Read(simulation.CurrentFlowDataB, ref CurrentFlowDataB);
				Read(simulation.CurrentFlowDataC, ref CurrentFlowDataC);
				Read(simulation.CurrentFlowDataD, ref CurrentFlowDataD);
				Read(simulation.CurrentFlowDataE, ref CurrentFlowDataE);
				Read(simulation.CurrentFlowDataF, ref CurrentFlowDataF);

				Read(simulation.CurrentParticleDataA, ref CurrentParticleDataA);
				Read(simulation.CurrentParticleDataB, ref CurrentParticleDataB);
				Read(simulation.CurrentParticleDataC, ref CurrentParticleDataC);
				Read(simulation.CurrentParticleDataD, ref CurrentParticleDataD);
				Read(simulation.CurrentParticleDataE, ref CurrentParticleDataE);
				Read(simulation.CurrentParticleDataF, ref CurrentParticleDataF);
			}
		}

		/// <summary>This method copies all raw fluid and particle data from this class into the specified simulation.</summary>
		public void RawToSimulation(FlowSimulation simulation)
		{
			if (simulation != null)
			{
				Write(CurrentFlowDataA, simulation.CurrentFlowDataA);
				Write(CurrentFlowDataB, simulation.CurrentFlowDataB);
				Write(CurrentFlowDataC, simulation.CurrentFlowDataC);
				Write(CurrentFlowDataD, simulation.CurrentFlowDataD);
				Write(CurrentFlowDataE, simulation.CurrentFlowDataE);
				Write(CurrentFlowDataF, simulation.CurrentFlowDataF);

				Write(CurrentParticleDataA, simulation.CurrentParticleDataA);
				Write(CurrentParticleDataB, simulation.CurrentParticleDataB);
				Write(CurrentParticleDataC, simulation.CurrentParticleDataC);
				Write(CurrentParticleDataD, simulation.CurrentParticleDataD);
				Write(CurrentParticleDataE, simulation.CurrentParticleDataE);
				Write(CurrentParticleDataF, simulation.CurrentParticleDataF);
			}
		}

		private static void Read(RenderTexture src, ref TextureData dst)
		{
			if (src != null)
			{
				var format = src.format;
				var size   = new Vector2Int(src.width, src.height);

				if (format == RenderTextureFormat.RGFloat)
				{
					format = RenderTextureFormat.ARGBFloat;
				}

				if (dst == null)
				{
					dst = new TextureData();
				}

				if (dst.RawData != null)
				{
					if (dst.RawData.format != format || dst.RawData.width != size.x || dst.RawData.height != size.y)
					{
						dst.ReleaseRaw();
					}
				}

				if (dst.RawData == null)
				{
					var desc = src.descriptor;

					desc.colorFormat = format;

					dst.RawData = CwRenderTextureManager.GetTemporary(desc, "FlowSnapshotData2");
				}

				Graphics.Blit(src, dst.RawData);
			}
			else if (dst != null)
			{
				dst.ReleaseRaw();
			}
		}

		private static void Write(TextureData src, RenderTexture dst)
		{
			if (dst != null)
			{
				if (src != null && src.RawData != null)
				{
					Graphics.Blit(src.RawData, dst);
				}
			}
		}

		private TextureData GetData(int index)
		{
			switch (index)
			{
				case 0 : return GetData(ref CurrentFlowDataA);
				case 1 : return GetData(ref CurrentFlowDataB);
				case 2 : return GetData(ref CurrentFlowDataC);
				case 3 : return GetData(ref CurrentFlowDataD);
				case 4 : return GetData(ref CurrentFlowDataE);
				case 5 : return GetData(ref CurrentFlowDataF);
				case 6 : return GetData(ref CurrentParticleDataA);
				case 7 : return GetData(ref CurrentParticleDataB);
				case 8 : return GetData(ref CurrentParticleDataC);
				case 9 : return GetData(ref CurrentParticleDataD);
				case 10: return GetData(ref CurrentParticleDataE);
				case 11: return GetData(ref CurrentParticleDataF);
			}

			return null;
		}
    }
}