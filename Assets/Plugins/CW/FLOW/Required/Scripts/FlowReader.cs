using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using CW.Common;

namespace FLOW
{
	/// <summary>This class allows you to read pixels from RenderTextures asynchronously.</summary>
	public class FlowReader
	{
		public List<Vector2Int> Pixels = new List<Vector2Int>();

		private FlowSimulation simulation;

		private ISampleHandler handler;

		private static List<FlowReader> addedReaders = new List<FlowReader>();

		private static List<FlowReader> waitingReaders = new List<FlowReader>();

		private static Stack<FlowReader> pool = new Stack<FlowReader>();

		private static RenderTexture addedBuffer;

		private static RenderTexture sampleBuffer;

		private static Vector2Int sampleSize;

		private static int index;

		private static Texture2D readBuffer;
		
		private static bool cachedSampleSet;
		private static Material cachedSampleMaterial;

		private static bool cachedSampleAreaSet;
		private static Material cachedSampleAreaMaterial;

		private static List<Color> tempColors = new List<Color>();

		private static AsyncGPUReadbackRequest request;

		private static bool requestSent;

		private const int BUFFER_WIDTH = 256;
		private const int BUFFER_DEPTH = 256;

		private static readonly int _ReaderRangeY       = Shader.PropertyToID("_ReaderRangeY");
		private static readonly int _ReaderMatrix       = Shader.PropertyToID("_ReaderMatrix");
		private static readonly int _ReaderShapeMatrix  = Shader.PropertyToID("_ReaderShapeMatrix");
		private static readonly int _ReaderShapePlane   = Shader.PropertyToID("_ReaderShapePlane");
		private static readonly int _ReaderShapeTexture = Shader.PropertyToID("_ReaderShapeTexture");
		private static readonly int _ReaderShapeChannel = Shader.PropertyToID("_ReaderShapeChannel");
		private static readonly int _ReaderBufferSize   = Shader.PropertyToID("_ReaderBufferSize");
		private static readonly int _ReaderPixel        = Shader.PropertyToID("_ReaderPixel");
		private static readonly int _ReaderBufferPixel  = Shader.PropertyToID("_ReaderBufferPixel");

		public static bool Ready
		{
			get
			{
				return waitingReaders.Count == 0;
			}
		}

		public Vector2Int AllocatePixel()
		{
			var bufferPixel = new Vector2Int(index % BUFFER_WIDTH, index / BUFFER_WIDTH);

			Pixels.Add(bufferPixel);

			index++;

			//if (SystemInfo.graphicsUVStartsAtTop == true)
			//{
			//	bufferPixel.y = BUFFER_DEPTH - bufferPixel.y - 1;
			//}

			return bufferPixel;
		}

		public static void Update()
		{
			// Request new samples?
			if (addedReaders.Count > 0 && waitingReaders.Count == 0)
			{
				waitingReaders.AddRange(addedReaders);

				sampleBuffer = CwRenderTextureManager.GetTemporary(addedBuffer.descriptor, "FlowReader");
				//sampleSize.x = BUFFER_WIDTH;
				//sampleSize.y = Mathf.Max(1, index / BUFFER_WIDTH - 1);
				sampleSize.x = BUFFER_WIDTH;
				sampleSize.y = BUFFER_DEPTH;

				Graphics.Blit(addedBuffer, sampleBuffer);

				addedReaders.Clear();

				index = 0;

				if (SystemInfo.supportsAsyncGPUReadback == true)
				{
					request     = AsyncGPUReadback.Request(sampleBuffer, 0, 0, sampleSize.x, 0, sampleSize.y, 0, 1);
					requestSent = true;
				}
			}

			// Process samples?
			if (waitingReaders.Count > 0)
			{
				if (sampleBuffer != null)
				{
					var pixels = default(NativeArray<Color>);

					if (requestSent == true)
					{
						if (request.hasError == true)
						{
							pixels = GetPixels(sampleBuffer);
						}
						else
						{
							if (request.done == false)
							{
								return;
							}

							pixels      = request.GetData<Color>();
							requestSent = false;
						}
					}
					else
					{
						pixels = GetPixels(sampleBuffer);
					}

					foreach (var reader in waitingReaders)
					{
						tempColors.Clear();

						foreach (var pixel in reader.Pixels)
						{
							tempColors.Add(pixels[pixel.x + pixel.y * sampleBuffer.width]);
						}

						reader.Complete(tempColors);
					}

					CwRenderTextureManager.ReleaseTemporary(sampleBuffer);

					sampleBuffer = null;
				}
				else
				{
					foreach (var reader in waitingReaders)
					{
						tempColors.Clear();

						foreach (var pixel in reader.Pixels)
						{
							tempColors.Add(default(Color));
						}

						reader.Complete(tempColors);
					}
				}

				waitingReaders.Clear();
			}
		}

		private static NativeArray<Color> GetPixels(RenderTexture buffer)
		{
			if (readBuffer == null)
			{
				readBuffer = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBAFloat, false, true);
			}

			readBuffer.Reinitialize(buffer.width, buffer.height);

			//if (SystemInfo.graphicsUVStartsAtTop == true)
			//{
			//	y = rt.height - y;
			//}

			CwHelper.BeginActive(buffer);
				readBuffer.ReadPixels(new Rect(0, 0, sampleSize.x, sampleSize.y), 0, 0);
			CwHelper.EndActive();

			readBuffer.Apply();

			return readBuffer.GetRawTextureData<Color>();
		}

		public void Complete(List<Color> value)
		{
			handler.HandleSamples(simulation, value);

			Pixels.Clear();

			pool.Push(this);
		}

		/*
		public static void Sample(Texture texture, Vector2Int pixel, ISampleHandler handler)
		{
			if (texture != null && handler != null)
			{
				var bufferPixel = InitStart(handler);
			}
		}
		*/

		public static RenderTexture AddedBuffer
		{
			get
			{
				return addedBuffer;
			}
		}

		public static FlowReader SampleFluidAreaDepth(FlowSimulation simulation, RectInt pixels, ISampleHandler handler, FlowReader reader)
		{
			if (simulation != null && simulation.Activated == true && pixels.width * pixels.height > 0 && handler != null)
			{
				BeginAreaSamples(simulation);
					if (reader == null)
					{
						reader = InitStart(simulation, handler);
					}

					cachedSampleAreaMaterial.SetMatrix(_ReaderMatrix, simulation.GetCoordToWorldMatrix());
					cachedSampleAreaMaterial.SetVector(_ReaderBufferSize, new Vector2(addedBuffer.width, addedBuffer.height));
					cachedSampleAreaMaterial.SetVector(_ReaderRangeY, new Vector2(pixels.yMin, pixels.yMax));

					//for (var y = pixels.yMin; y < pixels.yMax; y++)
					{
						for (var x = pixels.xMin; x < pixels.xMax; x++)
						{
							// Depth Total
							AddAreaSample(reader, 0, x);
						}
					}
				EndAreaSamples();

				return reader;
			}

			return null;
		}

		public static FlowReader SampleFluidArea(FlowSimulation simulation, RectInt pixels, Texture shapeTexture, FlowChannel shapeChannel, Matrix4x4 shapeMatrix, Plane shapePlane, ISampleHandler handler)
		{
			if (simulation != null && simulation.Activated == true && pixels.width * pixels.height > 0 && handler != null)
			{
				BeginAreaSamples(simulation);
					var reader = InitStart(simulation, handler);

					cachedSampleAreaMaterial.SetMatrix(_ReaderMatrix, simulation.GetCoordToWorldMatrix());
					cachedSampleAreaMaterial.SetVector(_ReaderBufferSize, new Vector2(addedBuffer.width, addedBuffer.height));
					cachedSampleAreaMaterial.SetVector(_ReaderRangeY, new Vector2(pixels.yMin, pixels.yMax));

					cachedSampleAreaMaterial.SetTexture(_ReaderShapeTexture, shapeTexture != null ? shapeTexture : Texture2D.whiteTexture);
					cachedSampleAreaMaterial.SetVector(_ReaderShapeChannel, FlowCommon.ChannelToVector(shapeChannel));
					cachedSampleAreaMaterial.SetMatrix(_ReaderShapeMatrix, shapeMatrix);
					cachedSampleAreaMaterial.SetVector(_ReaderShapePlane, new Vector4(shapePlane.normal.x, shapePlane.normal.y, shapePlane.normal.z, shapePlane.distance));

					//for (var y = pixels.yMin; y < pixels.yMax; y++)
					{
						for (var x = pixels.xMin; x < pixels.xMax; x++)
						{
							// Depth Total, Deepest Depth, Deepest X, Deepest Y
							AddAreaSample(reader, 1, x);
						}
					}
				EndAreaSamples();

				return reader;
			}

			return null;
		}

		public static void SampleFluid(FlowSimulation simulation, Vector3 worldPosition, ISampleHandler handler)
		{
			if (simulation != null && simulation.Activated == true && handler != null)
			{
				BeginSamples(simulation);
					var reader = InitStart(simulation, handler);

					var samplePixel = simulation.GetWorldToPixelMatrix().MultiplyPoint(worldPosition);

					cachedSampleMaterial.SetVector(_ReaderPixel, (Vector2)samplePixel);
					cachedSampleMaterial.SetVector(_ReaderBufferSize, new Vector2(addedBuffer.width, addedBuffer.height));

					// VelocityXZ, GroundHeight, WetHeight
					AddSample(reader, 0);

					// NormalXYZ, Depth
					AddSample(reader, 1);

					// RGBA
					AddSample(reader, 2);

					// ESMV
					AddSample(reader, 3);

					// F123
					AddSample(reader, 4);
				EndSamples();
			}
		}

		private static void BeginSamples(FlowSimulation simulation)
		{
			CwHelper.BeginActive(AddedBuffer);

			if (cachedSampleSet == false)
			{
				cachedSampleMaterial = CwHelper.CreateTempMaterial("Reader_Sample", Resources.Load<Shader>("FLOW/Reader_Sample"));
				cachedSampleSet      = true;
			}

			simulation.SetVariables(cachedSampleMaterial);
		}

		private static void BeginAreaSamples(FlowSimulation simulation)
		{
			CwHelper.BeginActive(AddedBuffer);

			if (cachedSampleAreaSet == false)
			{
				cachedSampleAreaMaterial = CwHelper.CreateTempMaterial("Reader_SampleArea", Resources.Load<Shader>("FLOW/Reader_SampleArea"));
				cachedSampleAreaSet      = true;
			}

			simulation.SetVariables(cachedSampleAreaMaterial);
		}

		private static void AddSample(FlowReader reader, int pass)
		{
			cachedSampleMaterial.SetVector(_ReaderBufferPixel, (Vector2)reader.AllocatePixel());

			FlowCommon.Draw(cachedSampleMaterial, pass);
		}

		private static void AddAreaSample(FlowReader reader, int pass, int x)
		{
			cachedSampleAreaMaterial.SetVector(_ReaderPixel, new Vector2(x, 0));
			cachedSampleAreaMaterial.SetVector(_ReaderBufferPixel, (Vector2)reader.AllocatePixel());

			FlowCommon.Draw(cachedSampleAreaMaterial, pass);
		}

		private static void EndSamples()
		{
			CwHelper.EndActive();
		}

		private static void EndAreaSamples()
		{
			CwHelper.EndActive();
		}

		private static FlowReader InitStart(FlowSimulation simulation, ISampleHandler handler)
		{
			var reader = pool.Count > 0 ? pool.Pop() : new FlowReader();

			if (addedBuffer == null)
			{
				addedBuffer = new RenderTexture(BUFFER_WIDTH, BUFFER_DEPTH, 0, RenderTextureFormat.ARGBFloat, 0);
			}

			reader.simulation = simulation;
			reader.handler    = handler;

			addedReaders.Add(reader);

			return reader;
		}
	}
}