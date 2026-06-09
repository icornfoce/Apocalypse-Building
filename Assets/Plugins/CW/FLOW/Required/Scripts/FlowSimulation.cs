using UnityEngine;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you to create a region where the fluid simulation occurs.
	/// NOTE: The <b>FlowSurface</b> component should be added on a child GameObject to render this fluid when the camera is above water.
	/// NOTE: The <b>FlowUnderwater</b> component should be added on a child GameObject to render this fluid when the camera is under water.</summary>
	[DefaultExecutionOrder(-100)]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSimulation")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Simulation")]
	public class FlowSimulation : MonoBehaviour
	{
		public enum FloatPrecision
		{
			Half,
			Full
		}

		public enum Precision01
		{
			Byte,
			Half,
			Full
		}

		public enum CustomDataType
		{
			None,
			One = 1,
			Three = 3
		}

		public enum PhysicsType
		{
			Standard,
			Alive,
			InversePeaks,
			Simple
		}

		/// <summary>This allows you to set the size of the fluid simulation along the X and Z axes in local space.
		/// NOTE: The Y value is ignored.</summary>
		public Vector3 Size { set { size = value; } get { return size; } } [SerializeField] private Vector3 size = new Vector3(100.0f, 0.0f, 100.0f);

		/// <summary>This allows you to set the distance between each fluid column on the X and Z axes in local space. The lower you set this, the higher detail the simulation will be, and the lower the performance.</summary>
		public float Separation { set { separation = value; } get { return separation; } } [SerializeField] private float separation = 1.0f;

		/// <summary>The amount of fluid columns used in the simulation will be multiplied by this, giving higher detail.
		/// NOTE: Increasing this will slow down performance.
		/// NOTE: The way the fluid physics behave will slightly change when changing this value, so higher isn't necessarily better depending on the look you want.</summary>
		public float Resolution { set { resolution = value; } get { return resolution; } } [SerializeField] private float resolution = 1.0f;

		/// <summary>Should the fluid boundary be centered, or start from the corner at 0,0?</summary>
		public bool Center { set { center = value; } get { return center; } } [SerializeField] private bool center;

		/// <summary>If the <b>Size</b> setting isn't perfectly divisible by the <b>Separation</b> setting, should the separation automatically be adjusted so the fluid volume matches the size?</summary>
		public bool Stretch { set { stretch = value; } get { return stretch; } } [SerializeField] private bool stretch = true;

		/// <summary>If you disable this, then the simulation will stop/freeze in time.</summary>
		public bool Simulating { set { simulating = value; } get { return simulating; } } [SerializeField] private bool simulating = true;

		/// <summary>Simulate ground wetness?
		/// NOTE: This requires the objects in your scene to use a shader/material that implements FLOW wetness. This can be done with the provided FLOW wetness shaders, manually implemented using a custom shader, or added on top using <b>BetterShaders</b> with the <b>Wetness</b> stacked shader that comes with FLOW.</summary>
		public bool Wetness { set { wetness = value; } get { return wetness; } } [SerializeField] private bool wetness;

		/// <summary>When the ground is dry, the water table will be this many meters below ground.</summary>
		public float TableDepth { set { tableDepth = value; } get { return tableDepth; } } [SerializeField] private float tableDepth = 1.0f;

		/// <summary>The wet areas of the scene will dry at this speed in meters per second.</summary>
		public float DryRate { set { dryRate = value; } get { return dryRate; } } [SerializeField] private float dryRate = 1.0f;

		/// <summary>This allows you to define which layers will be raycast to find the ground heights.</summary>
		public LayerMask HeightLayers { set { heightLayers = value; } get { return heightLayers; } } [SerializeField] private LayerMask heightLayers = 1;

		/// <summary>This allows you to specify the minimum ground height.</summary>
		public float HeightMin { set { heightMin = value; } get { return heightMin; } } [SerializeField] private float heightMin = -100.0f;

		/// <summary>This allows you to specify the maximum ground height.</summary>
		public float HeightMax { set { heightMax = value; } get { return heightMax; } } [SerializeField] private float heightMax = 100.0f;

		/// <summary>If you want the height sampling to have radius, specify it here. This is useful if your scene contains many thin objects that are normally missed by the ground height raycasts.
		/// NOTE: The higher you set this, the higher the disparity between the fluid and ground rendering can become.
		/// 0 = No radius.
		/// 1 = Radius matches column separation.</summary>
		public float HeightRadius { set { heightRadius = value; } get { return heightRadius; } } [SerializeField] private float heightRadius;

		/// <summary>When the surface is flowing, how fast should it be considered to be moving relative to the change in fluid level? This is used when calculating the buoyancy drag.</summary>
		public float Speed { set { speed = value; } get { return speed; } } [SerializeField] private float speed = 10.0f;

		/// <summary>The speed of the foam removal where 1 means it takes 1 second.</summary>
		public float FoamClearRate { set { foamClearRate = value; } get { return foamClearRate; } } [SerializeField] private float foamClearRate = 0.5f;

		/// <summary>If you enable this then you will be able to spawn particles into this fluid simulation.</summary>
		public bool Particles { set { particles = value; } get { return particles; } } [SerializeField] private bool particles;

		/// <summary>This allows you to set the maximum amount of particles that can be simulated.</summary>
		public int ParticleLimit { set { particleLimit = value; } get { return particleLimit; } } [SerializeField] private int particleLimit = 1024;

		/// <summary>The particles will be slowed down by this amount of atmospheric drag.</summary>
		public float ParticleDrag { set { particleDrag = value; } get { return particleDrag; } } [SerializeField] private float particleDrag = 0.9f;

		/// <summary>If you enable this then the fluid simulation will store up to three additional custom fluid properties.</summary>
		public CustomDataType CustomData { set { customData = value; } get { return customData; } } [SerializeField] private CustomDataType customData;

		/// <summary>This allows you to control the precision of the fluid mixing of the A texture (ground height, wet height). This should normally be kept at full, but it can be lowered if runtime memory usage is a concern.
		/// Half = 16 bits of precision.
		/// Full = 32 bits of precision.</summary>
		public FloatPrecision PrecisionA { set { precisionA = value; } get { return precisionA; } } [SerializeField] private FloatPrecision precisionA = FloatPrecision.Full;

		/// <summary>This allows you to control the precision of the fluid mixing of the B texture (velocity). This should normally be kept at full, but it can be lowered if runtime memory usage is a concern.
		/// Half = 16 bits of precision.
		/// Full = 32 bits of precision.</summary>
		public FloatPrecision PrecisionB { set { precisionB = value; } get { return precisionB; } } [SerializeField] private FloatPrecision precisionB = FloatPrecision.Full;

		/// <summary>This allows you to control the precision of the fluid mixing of the C texture (depth). This should normally be kept at full, but it can be lowered if runtime memory usage is a concern.
		/// Half = 16 bits of precision.
		/// Full = 32 bits of precision.</summary>
		public FloatPrecision PrecisionC { set { precisionC = value; } get { return precisionC; } } [SerializeField] private FloatPrecision precisionC = FloatPrecision.Full;

		/// <summary>This allows you to control the precision of the fluid mixing of the D texture (red, green, blue, alpha). If your game requires small columns of fluid to mix with large columns, and accurately store the mixed result then you can increase this.
		/// Byte = 8 bits of precision.
		/// Half = 16 bits of precision.
		/// Full = 32 bits of precision.</summary>
		public Precision01 PrecisionD { set { precisionD = value; } get { return precisionD; } } [SerializeField] private Precision01 precisionD;

		/// <summary>This allows you to control the precision of the fluid mixing of the E texture (emission, smoothness, metallic, viscosity). If your game requires small columns of fluid to mix with large columns, and accurately store the mixed result then you can increase this.
		/// Byte = 8 bits of precision.
		/// Half = 16 bits of precision.
		/// Full = 32 bits of precision.</summary>
		public Precision01 PrecisionE { set { precisionE = value; } get { return precisionE; } } [SerializeField] private Precision01 precisionE;

		/// <summary>This allows you to control the precision of the fluid mixing of the F texture (foam, custom1, custom2, custom3). If your game requires small columns of fluid to mix with large columns, and accurately store the mixed result then you can increase this.
		/// Byte = 8 bits of precision.
		/// Half = 16 bits of precision.
		/// Full = 32 bits of precision.</summary>
		public Precision01 PrecisionF { set { precisionF = value; } get { return precisionF; } } [SerializeField] private Precision01 precisionF;

		/// <summary>This allows you to change the physics model of the fluid simulation.
		/// Standard = The water will flow downhill and settle.
		/// Alive = The water will flow downhill and produce many waves that constantly appear.
		/// Inverse Peaks = The water will flow downhill and collect toward the center of the body of water.
		/// Simple = Water will only flow down and make simple waves/ripples, waves will not appear to have any inertia when going uphill.</summary>
		public PhysicsType PhysicsModel { set { physicsModel = value; } get { return physicsModel; } } [SerializeField] private PhysicsType physicsModel;

		/// <summary>If you increase this setting then low viscosity waves will not be able to settle down, and will always have some instability that looks like waves.</summary>
		public float PhysicsInstability { set { physicsInstability = value; } get { return physicsInstability; } } [SerializeField] [Range(0.0f, 0.3f)] private float physicsInstability;

		/// <summary>By default, high viscosity fluids will stick to the ground. If you don't want this behavior then you can increase this value.</summary>
		public float PhysicsSpread { set { physicsSpread = value; } get { return physicsSpread; } } [SerializeField] [Range(0.0f, 100.0f)] private float physicsSpread = 2.0f;

		/// <summary>The gives you control over the rate of acceleration of a fluid column, where a higher value means it accelerates faster.</summary>
		public float PhysicsDamping { set { physicsDamping = value; } get { return physicsDamping; } } [SerializeField] [Range(0.01f, 0.25f)] private float physicsDamping = 0.15f;

		/// <summary>The gives you some control over the fluid simulation speed.
		/// NOTE: This is a simple approximation.</summary>
		public float PhysicsSpeed { set { physicsSpeed = value; } get { return physicsSpeed; } } [SerializeField] [Range(0.01f, 1.0f)] private float physicsSpeed = 1.0f;

		/// <summary>This allows the fluid flow rate to exceed the available fluid in the column. This can be combined with strong AddForce modifiers to make larger waves.
		/// NOTE: Enabling this has a slight performance overhead.</summary>
		public bool PhysicsOverflow { set { physicsOverflow = value; } get { return physicsOverflow; } } [SerializeField] private bool physicsOverflow;

		public event System.Action OnUpdated;

		public event System.Action OnUpdatedParticles;

		private static Material fluidForcesMaterial;

		private static Material fluidTransportMaterial;

		private static Material fluidWetnessMaterial;

		private static Material particleForcesMaterial;

		private static Material particleMoveMaterial;

		private static Material particleContributeMaterial;

		/// <summary>The stores the column ground height and wet height.</summary>
		public RenderTexture CurrentFlowDataA { get { return currentFlowDataA; } } private RenderTexture currentFlowDataA;

		/// <summary>This stores the column velocity.</summary>
		public RenderTexture CurrentFlowDataB { get { return currentFlowDataB; } } private RenderTexture currentFlowDataB;

		/// <summary>This stores the column fluid depth.</summary>
		public RenderTexture CurrentFlowDataC { get { return currentFlowDataC; } } private RenderTexture currentFlowDataC;

		/// <summary>This stores the column fluid red, green, blue, alpha.</summary>
		public RenderTexture CurrentFlowDataD { get { return currentFlowDataD; } } private RenderTexture currentFlowDataD;

		/// <summary>This stores the column fluid emission, smoothness, metallic, viscosity.</summary>
		public RenderTexture CurrentFlowDataE { get { return currentFlowDataE; } } private RenderTexture currentFlowDataE;

		/// <summary>This stores the column fluid foam, custom1, custom2, custom3.</summary>
		public RenderTexture CurrentFlowDataF { get { return currentFlowDataF; } } private RenderTexture currentFlowDataF;

		/// <summary>This stores the particle velocity and age.</summary>
		public RenderTexture CurrentParticleDataA { get { return currentParticleDataA; } } private RenderTexture currentParticleDataA;

		/// <summary>This stores the particle position and life.</summary>
		public RenderTexture CurrentParticleDataB { get { return currentParticleDataB; } } private RenderTexture currentParticleDataB;

		/// <summary>This stores the particle fluid depth.</summary>
		public RenderTexture CurrentParticleDataC { get { return currentParticleDataC; } } private RenderTexture currentParticleDataC;

		/// <summary>This stores the particle fluid red, green, blue, alpha.</summary>
		public RenderTexture CurrentParticleDataD { get { return currentParticleDataD; } } private RenderTexture currentParticleDataD;

		/// <summary>This stores the particle fluid emission, smoothness, metallic, viscosity.</summary>
		public RenderTexture CurrentParticleDataE { get { return currentParticleDataE; } } private RenderTexture currentParticleDataE;

		/// <summary>This stores the particle foam, custom1, custom2, custom3.</summary>
		public RenderTexture CurrentParticleDataF { get { return currentParticleDataF; } } private RenderTexture currentParticleDataF;

		private const int CHUNK_SIZE = 64;

		private Texture2D writeBuffer;

		private int particleIndex;

		private bool partiallyUpdated;

		private float foamClearCounter;

		private Matrix4x4 worldToPixelMatrix = Matrix4x4.identity;

		private Matrix4x4 pixelToWorldMatrix = Matrix4x4.identity;

		private Matrix4x4 worldToCoordMatrix = Matrix4x4.identity;

		private Matrix4x4 coordToWorldMatrix = Matrix4x4.identity;

		private List<Vector2Int> dirtyChunks = new List<Vector2Int>();

		/// <summary>This tells you where the fluid begins in local space.</summary>
		public Vector3 ColumnMin { get { return columnMin; } } [SerializeField] private Vector3 columnMin;

		/// <summary>This tells you where the fluid ends in local space.</summary>
		public Vector3 ColumnMax { get { return columnMax; } } [SerializeField] private Vector3 columnMax;

		/// <summary>This tells you how many columns are used to simulate the fluid along the XZ axes.</summary>
		public Vector3Int ColumnCount { get { return columnCount; } } [SerializeField] private Vector3Int columnCount;

		/// <summary>This tells you the distance between each columns on the X and Z axes.</summary>
		public Vector3 ColumnSeparation { get { return columnSeparation; } } [SerializeField] private Vector3 columnSeparation;

		public float VolumePerColumn
		{
			get
			{
				return columnSeparation.x * columnSeparation.z;
			}
		}

		/// <summary>This stores all activate and enabled <b>FlowSimulation</b> instances in the scene.</summary>
		public static LinkedList<FlowSimulation> Instances { get { return instances; } } private static LinkedList<FlowSimulation> instances = new LinkedList<FlowSimulation>(); private LinkedListNode<FlowSimulation> instanceNode;

		private static int _FlowResolution       = Shader.PropertyToID("_FlowResolution");
		private static int _FlowSpeed            = Shader.PropertyToID("_FlowSpeed");
		private static int _FlowDataA            = Shader.PropertyToID("_FlowDataA");
		private static int _FlowDataB            = Shader.PropertyToID("_FlowDataB");
		private static int _FlowDataC            = Shader.PropertyToID("_FlowDataC");
		private static int _FlowDataD            = Shader.PropertyToID("_FlowDataD");
		private static int _FlowDataE            = Shader.PropertyToID("_FlowDataE");
		private static int _FlowDataF            = Shader.PropertyToID("_FlowDataF");
		private static int _FlowMatrix           = Shader.PropertyToID("_FlowMatrix");
		private static int _FlowSeparationXZ     = Shader.PropertyToID("_FlowSeparationXZ");
		private static int _FlowCoordU000        = Shader.PropertyToID("_FlowCoordU000");
		private static int _FlowCoord0V00        = Shader.PropertyToID("_FlowCoord0V00");
		private static int _FlowCoordUV00        = Shader.PropertyToID("_FlowCoordUV00");
		private static int _FlowCountXZ          = Shader.PropertyToID("_FlowCountXZ");
		private static int _PhysicsInstability   = Shader.PropertyToID("_PhysicsInstability");
		private static int _PhysicsDamping       = Shader.PropertyToID("_PhysicsDamping");
		private static int _PhysicsSpread        = Shader.PropertyToID("_PhysicsSpread");
		private static int _PhysicsSpeed         = Shader.PropertyToID("_PhysicsSpeed");
		private static int _FlowSimulationHeight = Shader.PropertyToID("_FlowSimulationHeight");
		private static int _PartDataA            = Shader.PropertyToID("_PartDataA");
		private static int _PartDataB            = Shader.PropertyToID("_PartDataB");
		private static int _PartDataC            = Shader.PropertyToID("_PartDataC");
		private static int _PartDataD            = Shader.PropertyToID("_PartDataD");
		private static int _PartDataE            = Shader.PropertyToID("_PartDataE");
		private static int _PartDataF            = Shader.PropertyToID("_PartDataF");
		private static int _PartCoordUV          = Shader.PropertyToID("_PartCoordUV");
		private static int _PartCountXY          = Shader.PropertyToID("_PartCountXY");
		private static int _FlowFoamClearRate    = Shader.PropertyToID("_FlowFoamClearRate");
		private static int _FlowTableDepth       = Shader.PropertyToID("_FlowTableDepth");
		private static int _FlowDryRate          = Shader.PropertyToID("_FlowDryRate");
		private static int _FlowDelta            = Shader.PropertyToID("_FlowDelta");
		private static int _PartGravity          = Shader.PropertyToID("_PartGravity");
		private static int _PartDrag             = Shader.PropertyToID("_PartDrag");

		public bool Activated
		{
			get
			{
				return currentFlowDataA != null;
			}
		}

		public bool ParticlesActivated
		{
			get
			{
				return currentParticleDataA != null;
			}
		}

		public Vector3 ColumnStepX
		{
			get
			{
				return new Vector3(columnSeparation.x, 0.0f, 0.0f);
			}
		}

		public Vector3 ColumnStepZ
		{
			get
			{
				return new Vector3(0.0f, 0.0f, columnSeparation.z);
			}
		}

		public bool PartiallyUpdated
		{
			get
			{
				return partiallyUpdated;
			}
		}

		public void NotifyUpdated()
		{
			if (OnUpdated != null)
			{
				OnUpdated.Invoke();
			}
		}

		/// <summary>This will update the ground heights for the whole fluid via raycast.
		/// NOTE: This method is slow.</summary>
		[ContextMenu("Update Ground")]
		public void UpdateGround()
		{
			if (Activated == true)
			{
				var chunksX = columnCount.x / CHUNK_SIZE;
				var chunksZ = columnCount.z / CHUNK_SIZE;

				for (var z = 0; z <= chunksZ; z++)
				{
					for (var x = 0; x <= chunksX; x++)
					{
						UpdateGround(x, z);
					}
				}

				dirtyChunks.Clear();
			}
		}

		/// <summary>This allows you to add one particle to the fluid simulation.
		/// NOTE: This fluid simulation must have the <b>Particles</b> setting enabled, and there must be a child <b>FlowParticles</b> component to render it.</summary>
		public void AddParticle(FlowFluid fluid, float depth, Vector3 position, Vector3 velocity, float life, float foam = 0.0f)
		{
			if (Activated == true && ParticlesActivated == true && fluid != null)
			{
				particleIndex = (particleIndex + 1) % particleLimit;

				var pixel = new RectInt(particleIndex, 0, 1, 1);
				var color = CwHelper.ToLinear(fluid.Color);

				FlowReplace.Replace(currentParticleDataA, pixel, Texture2D.whiteTexture, new Vector4(velocity.x, velocity.y, velocity.z, 0.0f));
				FlowReplace.Replace(currentParticleDataB, pixel, Texture2D.whiteTexture, new Vector4(position.x, position.y, position.z, life));
				FlowReplace.Replace(currentParticleDataC, pixel, Texture2D.whiteTexture, new Vector4(depth, 0.0f, 0.0f, 0.0f));
				FlowReplace.Replace(currentParticleDataD, pixel, Texture2D.whiteTexture, new Vector4(color.r, color.g, color.b, color.a));
				FlowReplace.Replace(currentParticleDataE, pixel, Texture2D.whiteTexture, new Vector4(fluid.Emission, fluid.Smoothness, fluid.Metallic, fluid.Viscosity));
				FlowReplace.Replace(currentParticleDataF, pixel, Texture2D.whiteTexture, new Vector4(foam, 0.0f, 0.0f, 0.0f));
			}
		}

		private void UpdateGround(int chunkX, int chunkZ)
		{
			if (writeBuffer == null)
			{
				writeBuffer = new Texture2D(CHUNK_SIZE, CHUNK_SIZE, TextureFormat.RGFloat, false);
			}

			var hit     = default(RaycastHit);
			var corner  = transform.TransformPoint(columnMin); corner.y = heightMax;
			var stepX   = transform.TransformPoint(ColumnStepX) - transform.position;
			var stepZ   = transform.TransformPoint(ColumnStepZ) - transform.position;
			var pixel   = Color.clear;
			var minX    = chunkX * CHUNK_SIZE;
			var minZ    = chunkZ * CHUNK_SIZE;
			var maxX    = minX + CHUNK_SIZE - 1;
			var maxZ    = minZ + CHUNK_SIZE - 1;
			var epsilon = 0.01f;
			var clampX  = (columnCount.x - 1) - epsilon;
			var clampZ  = (columnCount.z - 1) - epsilon;
			var radius  = stepX.magnitude * heightRadius;

			for (var z = minZ; z <= maxZ; z++)
			{
				for (var x = minX; x <= maxX; x++)
				{
					if (heightLayers != 0)
					{
						var world = corner;

						world += stepX * Mathf.Clamp(x, epsilon, clampX); // Prevent samples falling off the edge of terrains due to floating point inaccuracy
						world += stepZ * Mathf.Clamp(z, epsilon, clampZ);

						if (radius == 0.0f)
						{
							if (Physics.Raycast(world, Vector3.down, out hit, heightMax - heightMin, heightLayers) == true)
							{
								pixel.r = hit.point.y;
							}
							else
							{
								pixel.r = heightMin;
							}
						}
						else
						{
							if (Physics.SphereCast(world, radius, Vector3.down, out hit, heightMax - heightMin, heightLayers) == true)
							{
								pixel.r = hit.point.y;
							}
							else
							{
								pixel.r = heightMin;
							}
						}
					}
					else
					{
						pixel.r = heightMin;
					}

					pixel.g = -tableDepth;

					writeBuffer.SetPixel(x, z, pixel);
				}
			}

			writeBuffer.Apply();

			var rect = new RectInt(new Vector2Int(chunkX * CHUNK_SIZE, chunkZ * CHUNK_SIZE), new Vector2Int(CHUNK_SIZE, CHUNK_SIZE));

			FlowReplace.Replace(currentFlowDataA, rect, writeBuffer, Vector4.one);
		}

		/// <summary>This will update the ground heights for a specific region of the fluid via raycast.</summary>
		public void DirtyGround(Vector3 worldCenter, float worldRadius)
		{
			var corner      = transform.right + transform.forward;
			var pixelMin    = worldToPixelMatrix.MultiplyPoint(worldCenter - corner * worldRadius);
			var pixelMax    = worldToPixelMatrix.MultiplyPoint(worldCenter + corner * worldRadius);
			var chunkMinX   = Mathf.FloorToInt(pixelMin.x / CHUNK_SIZE);
			var chunkMinZ   = Mathf.FloorToInt(pixelMin.y / CHUNK_SIZE);
			var chunkMaxX   = Mathf.FloorToInt(pixelMax.x / CHUNK_SIZE);
			var chunkMaxZ   = Mathf.FloorToInt(pixelMax.y / CHUNK_SIZE);

			for (var z = chunkMinZ; z <= chunkMaxZ; z++)
			{
				for (var x = chunkMinX; x <= chunkMaxX; x++)
				{
					DirtyGround(new Vector2Int(x, z));
				}
			}
		}

		public static void DirtyGroundAll(Vector3 worldCenter, float worldRadius)
		{
			foreach (var instance in instances)
			{
				if (instance.Activated == true)
				{
					instance.DirtyGround(worldCenter, worldRadius);
				}
			}
		}

		public void DirtyGround(Vector2Int chunk)
		{
			if (dirtyChunks.Contains(chunk) == false)
			{
				dirtyChunks.Add(chunk);
			}
		}

		public Matrix4x4 GetWorldToPixelMatrix()
		{
			return worldToPixelMatrix;
		}

		public Matrix4x4 GetPixelToWorldMatrix()
		{
			return pixelToWorldMatrix;
		}

		public Matrix4x4 GetWorldToCoordMatrix()
		{
			return worldToCoordMatrix;
		}

		public Matrix4x4 GetCoordToWorldMatrix()
		{
			return coordToWorldMatrix;
		}

		public RectInt CalculatePixelRect(Bounds worldBounds)
		{
			if (Activated == true)
			{
				var min = (Vector2)worldToPixelMatrix.MultiplyPoint(worldBounds.min);
				var max = (Vector2)worldToPixelMatrix.MultiplyPoint(worldBounds.max);

				var minX = Mathf.FloorToInt(min.x);
				var minY = Mathf.FloorToInt(min.y);
				var maxX = Mathf.CeilToInt(max.x + 0.5f);
				var maxY = Mathf.CeilToInt(max.y + 0.5f);

				var sizeX = CurrentFlowDataC.width;
				var sizeY = CurrentFlowDataC.height;

				minX = Mathf.Clamp(minX, 0, sizeX);
				minY = Mathf.Clamp(minY, 0, sizeY);
				maxX = Mathf.Clamp(maxX, 0, sizeX);
				maxY = Mathf.Clamp(maxY, 0, sizeY);

				return new RectInt(minX, minY, maxX - minX, maxY - minY);
			}

			return default(RectInt);
		}

		private void UpdateMatrices()
		{
			var translation = Matrix4x4.Translate(-columnMin);
			var rotation    = Matrix4x4.Rotate(Quaternion.Euler(-90.0f, 0.0f, 0.0f));
			var scaleA      = Matrix4x4.Scale(new Vector3(1.0f / columnSeparation.x, 1.0f, 1.0f / columnSeparation.z));
			var scaleB      = Matrix4x4.Scale(new Vector3(1.0f / (columnCount.x - 1) / columnSeparation.x, 1.0f, 1.0f / (columnCount.z - 1) / columnSeparation.z));

			worldToPixelMatrix = rotation * scaleA * translation * transform.worldToLocalMatrix;
			worldToCoordMatrix = rotation * scaleB * translation * transform.worldToLocalMatrix;

			pixelToWorldMatrix = worldToPixelMatrix.inverse;
			coordToWorldMatrix = worldToCoordMatrix.inverse;
		}

		private void Release(ref RenderTexture renderTexture)
		{
			if (renderTexture != null)
			{
				renderTexture = CwRenderTextureManager.ReleaseTemporary(renderTexture);
			}
		}

		protected virtual void OnEnable()
		{
			instanceNode = instances.AddLast(this);

			FlowManager.EnsureThisComponentExists();
		}

		protected virtual void OnDisable()
		{
			instances.Remove(instanceNode); instanceNode = null;
		}

		protected virtual void OnDestroy()
		{
			Release(ref currentFlowDataA);
			Release(ref currentFlowDataB);
			Release(ref currentFlowDataC);
			Release(ref currentFlowDataD);
			Release(ref currentFlowDataE);
			Release(ref currentFlowDataF);
			Release(ref currentParticleDataA);
			Release(ref currentParticleDataB);
			Release(ref currentParticleDataC);
			Release(ref currentParticleDataD);
			Release(ref currentParticleDataE);
			Release(ref currentParticleDataF);
		}

		protected virtual void Start()
		{
			var ecc = EstimatedColumnCount;

			if (ecc.x > 1 && ecc.z > 1)
			{
				var bounds = CalculateLocalBounds();

				columnMin.x = bounds.min.x;
				columnMin.z = bounds.min.z;
				columnMax.x = bounds.max.x;
				columnMax.z = bounds.max.z;

				columnCount.x = ecc.x;
				columnCount.z = ecc.z;

				columnSeparation.x = (columnMax.x - columnMin.x) / (columnCount.x - 1);
				columnSeparation.z = (columnMax.z - columnMin.z) / (columnCount.z - 1);

				UpdateMatrices();

				TryCreateTemporary(ref currentFlowDataA, columnCount.x, columnCount.z, GetFormatRG(precisionA));
				TryCreateTemporary(ref currentFlowDataB, columnCount.x, columnCount.z, GetFormatRGBA(precisionB));
				TryCreateTemporary(ref currentFlowDataC, columnCount.x, columnCount.z, GetFormatR(precisionC));
				TryCreateTemporary(ref currentFlowDataD, columnCount.x, columnCount.z, GetFormatRGBA(precisionD));
				TryCreateTemporary(ref currentFlowDataE, columnCount.x, columnCount.z, GetFormatRGBA(precisionE));
				TryCreateTemporary(ref currentFlowDataF, columnCount.x, columnCount.z, GetFormatF());

				UpdateGround();

				ReplaceFluids(0.0f, Color.clear, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
				ReplaceOutflow();

				if (particles == true)
				{
					ActivateParticles();
				}
			}
		}

		private RenderTextureFormat GetFormatR(Precision01 precision)
		{
			switch (precision)
			{
				case Precision01.Byte: return RenderTextureFormat.R8;
				case Precision01.Half: return RenderTextureFormat.RHalf;
				case Precision01.Full: return RenderTextureFormat.RFloat;
			}

			return RenderTextureFormat.R8;
		}

		private RenderTextureFormat GetFormatRG(Precision01 precision)
		{
			switch (precision)
			{
				case Precision01.Byte: return RenderTextureFormat.RG16;
				case Precision01.Half: return RenderTextureFormat.RGHalf;
				case Precision01.Full: return RenderTextureFormat.RGFloat;
			}

			return RenderTextureFormat.ARGB32;
		}

		private RenderTextureFormat GetFormatRGBA(Precision01 precision)
		{
			switch (precision)
			{
				case Precision01.Byte: return RenderTextureFormat.ARGB32;
				case Precision01.Half: return RenderTextureFormat.ARGBHalf;
				case Precision01.Full: return RenderTextureFormat.ARGBFloat;
			}

			return RenderTextureFormat.ARGB32;
		}

		private RenderTextureFormat GetFormatR(FloatPrecision precision)
		{
			switch (precision)
			{
				case FloatPrecision.Half: return RenderTextureFormat.RHalf;
				case FloatPrecision.Full: return RenderTextureFormat.RFloat;
			}

			return RenderTextureFormat.R8;
		}

		private RenderTextureFormat GetFormatRG(FloatPrecision precision)
		{
			switch (precision)
			{
				case FloatPrecision.Half: return RenderTextureFormat.RGHalf;
				case FloatPrecision.Full: return RenderTextureFormat.RGFloat;
			}

			return RenderTextureFormat.ARGB32;
		}

		private RenderTextureFormat GetFormatRGBA(FloatPrecision precision)
		{
			switch (precision)
			{
				case FloatPrecision.Half: return RenderTextureFormat.ARGBHalf;
				case FloatPrecision.Full: return RenderTextureFormat.ARGBFloat;
			}

			return RenderTextureFormat.ARGB32;
		}

		private RenderTextureFormat GetFormatF()
		{
			switch (customData)
			{
				case CustomDataType.None:  return GetFormatR(precisionF);
				case CustomDataType.One:   return GetFormatRG(precisionF);
				case CustomDataType.Three: return GetFormatRGBA(precisionF);
			}

			return GetFormatRGBA(precisionF);
		}

		protected virtual void Update()
		{
			UpdateMatrices();

			if (dirtyChunks.Count > 0)
			{
				var chunk = dirtyChunks[0];

				dirtyChunks.RemoveAt(0);

				UpdateGround(chunk.x, chunk.y);
			}
		}

		public Vector3Int EstimatedColumnCount
		{
			get
			{
				var detail   = Mathf.Sqrt(resolution);
				var columnsX = Mathf.RoundToInt((size.x / separation) * detail) + 1;
				var columnsZ = Mathf.RoundToInt((size.z / separation) * detail) + 1;
				var max      = SystemInfo.maxTextureSize;

				if (columnsX > max)
				{
					columnsX = max;
				}

				if (columnsZ > max)
				{
					columnsZ = max;
				}

				return new Vector3Int(columnsX, 0, columnsZ);
			}
		}

		public void ActivateParticles()
		{
			if (ParticlesActivated == false)
			{
				TryCreateTemporary(ref currentParticleDataA, particleLimit, 1, RenderTextureFormat.ARGBFloat);
				TryCreateTemporary(ref currentParticleDataB, particleLimit, 1, RenderTextureFormat.ARGBFloat);
				TryCreateTemporary(ref currentParticleDataC, particleLimit, 1, currentFlowDataC.format);
				TryCreateTemporary(ref currentParticleDataD, particleLimit, 1, currentFlowDataD.format);
				TryCreateTemporary(ref currentParticleDataE, particleLimit, 1, currentFlowDataE.format);
				TryCreateTemporary(ref currentParticleDataF, particleLimit, 1, currentFlowDataF.format);
			}
		}

		public void UpdateFluidForces()
		{
			if (simulating == true && partiallyUpdated == false)
			{
				DoFluidForces();

				partiallyUpdated = true;
			}
		}

		public void UpdateFluidTransport(float delta)
		{
			if (simulating == true && partiallyUpdated == true)
			{
				DoFluidTransport(delta);

				partiallyUpdated = false;
			}
		}

		public void UpdateFluidWetness(float delta)
		{
			if (simulating == true)
			{
				DoFluidWetness(delta);
			}
		}

		public void UpdateParticles(float delta)
		{
			if (simulating == true)
			{
				DoParticleContribute();
				DoParticleForces(delta);
				DoParticleMove(delta);

				if (OnUpdatedParticles != null)
				{
					OnUpdatedParticles.Invoke();
				}
			}
		}

		private void TryCreateTemporary(ref RenderTexture renderTexture, int width, int height, RenderTextureFormat format)
		{
			if (renderTexture == null)
			{
				var descriptor = new RenderTextureDescriptor(width, height, format, 0, 0);

				renderTexture = CwRenderTextureManager.GetTemporary(descriptor, "FlowSimulation");
			}
		}

		/// <summary>This will find the closest simulation to the specified world point, if the current one doesn't exist.</summary>
		public static FlowSimulation FindSimulation(Vector3 worldPoint, FlowSimulation current = null)
		{
			if (current == null || current.Activated == false)
			{
				var bestDist = float.PositiveInfinity;

				foreach (var instance in instances)
				{
					if (instance.Activated == true)
					{
						var localCenter = (instance.columnMin + instance.columnMax) * 0.5f;
						var worldCenter = instance.transform.TransformPoint(localCenter);
						var worldSep    = CalculateDistanceXZ(worldCenter, worldPoint);
						var localPoint  = instance.transform.InverseTransformPoint(worldPoint);
						var localSep    = CalculateDistanceXZ(localCenter, localPoint);
						var localDist   = CalculateDistanceXZ(instance.columnMin, instance.columnMax, localPoint);

						if (localSep > 0.0f)
						{
							localDist *= worldSep / localSep;
						}

						if (localDist < bestDist)
						{
							bestDist = localDist;
							current  = instance;
						}
						/*
						if (localPoint.x >= instance.ColumnMin.x && localPoint.x <= instance.ColumnMax.x)
						{
							if (localPoint.z >= instance.ColumnMin.z && localPoint.z <= instance.ColumnMax.z)
							{
								return instance;
							}
						}
						*/
					}
				}
			}

			return current;
		}

		private static float CalculateDistanceXZ(Vector3 a, Vector3 b)
		{
			var dx = a.x - b.x;
			var dz = a.z - b.z;

			return Mathf.Sqrt(dx * dx + dz * dz);
		}

		private static float CalculateDistanceXZ(Vector3 min, Vector3 max, Vector3 point)
		{
			var dx = Mathf.Max(Mathf.Max(min.x - point.x, point.x - max.x), 0.0f);
			var dz = Mathf.Max(Mathf.Max(min.z - point.z, point.z - max.z), 0.0f);

			return Mathf.Sqrt(dx * dx + dz * dz);
		}

		public void ReplaceHeights(float groundHeight, float wetDepth)
		{
			var vector = default(Vector4);

			vector.x = groundHeight;
			vector.y = wetDepth;
			vector.z = 0.0f;
			vector.w = 0.0f;

			FlowReplace.Replace(currentFlowDataA, null, vector);
		}

		public void ReplaceFluids(float depth, Color color, float emission, float smoothness, float metallic, float foam, float viscosity)
		{
			FlowReplace.Replace(currentFlowDataC, null, new Vector4(depth, 0.0f, 0.0f, 0.0f));
			FlowReplace.Replace(currentFlowDataD, null, new Vector4(color.r, color.g, color.b, color.a));
			FlowReplace.Replace(currentFlowDataE, null, new Vector4(emission, smoothness, metallic, viscosity));
			FlowReplace.Replace(currentFlowDataF, null, new Vector4(foam, 0.0f, 0.0f, 0.0f));
		}

		public void ReplaceOutflow()
		{
			FlowReplace.Replace(currentFlowDataB, null, Vector4.zero);
		}

		public void SetVariables(Material material)
		{
			var stepX = 1.0f / columnCount.x;
			var stepZ = 1.0f / columnCount.z;
			
			material.SetFloat(_FlowResolution, resolution);
			material.SetFloat(_FlowSpeed, speed);
			material.SetTexture(_FlowDataA, currentFlowDataA);
			material.SetTexture(_FlowDataB, currentFlowDataB);
			material.SetTexture(_FlowDataC, currentFlowDataC);
			material.SetTexture(_FlowDataD, currentFlowDataD);
			material.SetTexture(_FlowDataE, currentFlowDataE);
			material.SetTexture(_FlowDataF, currentFlowDataF);
			material.SetMatrix(_FlowMatrix, worldToPixelMatrix);
			material.SetVector(_FlowSeparationXZ, new Vector2(columnSeparation.x, columnSeparation.z));
			material.SetVector(_FlowCoordU000, new Vector4(stepX, 0.0f, 0.0f, 0.0f));
			material.SetVector(_FlowCoord0V00, new Vector4(0.0f, stepZ, 0.0f, 0.0f));
			material.SetVector(_FlowCoordUV00, new Vector4(stepX, stepZ, 0.0f, 0.0f));
			material.SetVector(_FlowCountXZ, new Vector2(columnCount.x, columnCount.z));
		}

		public void SetVariables(MaterialPropertyBlock properties)
		{
			properties.SetFloat(_FlowResolution, resolution);
			properties.SetFloat(_FlowSpeed, speed);
			properties.SetFloat(_FlowSimulationHeight, transform.position.y);
			properties.SetTexture(_FlowDataA, currentFlowDataA);
			properties.SetTexture(_FlowDataB, currentFlowDataB);
			properties.SetTexture(_FlowDataC, currentFlowDataC);
			properties.SetTexture(_FlowDataD, currentFlowDataD);
			properties.SetTexture(_FlowDataE, currentFlowDataE);
			properties.SetTexture(_FlowDataF, currentFlowDataF);
			properties.SetMatrix(_FlowMatrix, worldToPixelMatrix);
			properties.SetVector(_FlowSeparationXZ, new Vector2(columnSeparation.x, columnSeparation.z));
			properties.SetVector(_FlowCountXZ, new Vector2(columnCount.x, columnCount.z));
			properties.SetVector(_FlowCoordU000, new Vector4(1.0f / columnCount.x, 0.0f, 0.0f, 0.0f));
			properties.SetVector(_FlowCoord0V00, new Vector4(0.0f, 1.0f / columnCount.z, 0.0f, 0.0f));
		}

		public void SetWetnessVariables(Material material)
		{
			material.SetTexture(_FlowDataA, currentFlowDataA);
			material.SetVector(_FlowCountXZ, new Vector2(columnCount.x, columnCount.z));
			material.SetMatrix(_FlowMatrix, worldToPixelMatrix);
		}

		public void SetWetnessVariables(MaterialPropertyBlock properties)
		{
			properties.SetTexture(_FlowDataA, currentFlowDataA);
			properties.SetVector(_FlowCountXZ, new Vector2(columnCount.x, columnCount.z));
			properties.SetMatrix(_FlowMatrix, worldToPixelMatrix);
		}

		public void SetParticleVariables(Material material)
		{
			material.SetTexture(_PartDataA, currentParticleDataA);
			material.SetTexture(_PartDataB, currentParticleDataB);
			material.SetTexture(_PartDataC, currentParticleDataC);
			material.SetTexture(_PartDataD, currentParticleDataD);
			material.SetTexture(_PartDataE, currentParticleDataE);
			material.SetTexture(_PartDataF, currentParticleDataF);
			material.SetVector(_PartCoordUV, new Vector2(1.0f / particleLimit, 1.0f / particleLimit));
			material.SetVector(_PartCountXY, new Vector2(particleLimit, particleLimit));
		}

		public void SetParticleVariables(MaterialPropertyBlock properties)
		{
			properties.SetTexture(_PartDataA, currentParticleDataA);
			properties.SetTexture(_PartDataB, currentParticleDataB);
			properties.SetTexture(_PartDataC, currentParticleDataC);
			properties.SetTexture(_PartDataD, currentParticleDataD);
			properties.SetTexture(_PartDataE, currentParticleDataE);
			properties.SetTexture(_PartDataF, currentParticleDataF);
			properties.SetVector(_PartCoordUV, new Vector2(1.0f / particleLimit, 1.0f / particleLimit));
			properties.SetVector(_PartCountXY, new Vector2(particleLimit, particleLimit));
		}

		/// <summary>This tells you the 0..1 percentage the specified 2D circle overlaps this simulation on the XZ axes.</summary>
		public float GetOverlapXZ(Vector3 worldPoint, float radius)
		{
			// TODO: Implement this
			return GetOverlapXZ(worldPoint) == true ? 1.0f : 0.0f;
		}

		/// <summary>This tells you if the specified 2D point overlaps this simulation on the XZ axes.</summary>
		public bool GetOverlapXZ(Vector3 worldPoint)
		{
			if (Activated == true)
			{
				var localPoint = transform.InverseTransformPoint(worldPoint);

				if (localPoint.x >= columnMin.x && localPoint.x <= columnMax.x)
				{
					if (localPoint.z >= columnMin.z && localPoint.z <= columnMax.z)
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowSimulation</b> component attached.</summary>
		public static FlowSimulation Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowSimulation</b> component attached.</summary>
		public static FlowSimulation Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Simulation", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowSimulation>();
		}

		private void DoFluidForces()
		{
			if (fluidForcesMaterial == null)
			{
				fluidForcesMaterial = CwHelper.CreateTempMaterial("Simulation_FluidForces", Resources.Load<Shader>("FLOW/Simulation_FluidForces"));
			}

			SetVariables(fluidForcesMaterial);

			fluidForcesMaterial.DisableKeyword("PHYSICS_STANDARD");
			fluidForcesMaterial.DisableKeyword("PHYSICS_ALIVE");
			fluidForcesMaterial.DisableKeyword("PHYSICS_INVERSEPEAKS");
			fluidForcesMaterial.DisableKeyword("PHYSICS_SIMPLE");
			fluidForcesMaterial.SetFloat(_PhysicsInstability, physicsInstability);
			fluidForcesMaterial.SetFloat(_PhysicsDamping, physicsDamping);
			fluidForcesMaterial.SetFloat(_PhysicsSpread, physicsSpread);
			fluidForcesMaterial.SetFloat(_PhysicsSpeed, physicsSpeed);

			switch (physicsModel)
			{
				case PhysicsType.Standard:
				{
					fluidForcesMaterial.EnableKeyword("PHYSICS_STANDARD");
				}
				break;

				case PhysicsType.Alive:
				{
					fluidForcesMaterial.EnableKeyword("PHYSICS_ALIVE");
				}
				break;

				case PhysicsType.InversePeaks:
				{
					fluidForcesMaterial.EnableKeyword("PHYSICS_INVERSEPEAKS");
				}
				break;

				case PhysicsType.Simple:
				{
					fluidForcesMaterial.EnableKeyword("PHYSICS_SIMPLE");
				}
				break;
			}

			if (physicsOverflow == true)
			{
				fluidForcesMaterial.EnableKeyword("PHYSICS_OVERFLOW");
			}
			else
			{
				fluidForcesMaterial.DisableKeyword("PHYSICS_OVERFLOW");
			}

			var tempBuffer = CwRenderTextureManager.GetTemporary(currentFlowDataB.descriptor, "FlowSimulation tempBuffer");

			Graphics.Blit(null, tempBuffer, fluidForcesMaterial, 0);

			CwRenderTextureManager.ReleaseTemporary(currentFlowDataB);

			// Swap
			currentFlowDataB = tempBuffer;
		}

		private RenderBuffer[] tempBuffers = new RenderBuffer[4];

		private void DoFluidTransport(float delta)
		{
			if (fluidTransportMaterial == null)
			{
				fluidTransportMaterial = CwHelper.CreateTempMaterial("Simulation_FluidTransport", Resources.Load<Shader>("FLOW/Simulation_FluidTransport"));
			}

			SetVariables(fluidTransportMaterial);

			// Pixels
			foamClearCounter += 255.0f * foamClearRate * delta;

			fluidTransportMaterial.SetFloat(_FlowFoamClearRate, SnapPixel255(ref foamClearCounter, 1));

			if (physicsOverflow == true)
			{
				fluidTransportMaterial.EnableKeyword("PHYSICS_OVERFLOW");
			}
			else
			{
				fluidTransportMaterial.DisableKeyword("PHYSICS_OVERFLOW");
			}

			var tempBufferC = CwRenderTextureManager.GetTemporary(currentFlowDataC.descriptor, "FlowSimulation tempBufferC");
			var tempBufferD = CwRenderTextureManager.GetTemporary(currentFlowDataD.descriptor, "FlowSimulation tempBufferD");
			var tempBufferE = CwRenderTextureManager.GetTemporary(currentFlowDataE.descriptor, "FlowSimulation tempBufferE");
			var tempBufferF = CwRenderTextureManager.GetTemporary(currentFlowDataF.descriptor, "FlowSimulation tempBufferF");

			tempBuffers[0] = tempBufferC.colorBuffer;
			tempBuffers[1] = tempBufferD.colorBuffer;
			tempBuffers[2] = tempBufferE.colorBuffer;
			tempBuffers[3] = tempBufferF.colorBuffer;

			Graphics.SetRenderTarget(tempBuffers, tempBufferC.depthBuffer);
			Graphics.Blit(null, fluidTransportMaterial, 0);

			CwRenderTextureManager.ReleaseTemporary(currentFlowDataC);
			CwRenderTextureManager.ReleaseTemporary(currentFlowDataD);
			CwRenderTextureManager.ReleaseTemporary(currentFlowDataE);
			CwRenderTextureManager.ReleaseTemporary(currentFlowDataF);

			// Swap
			currentFlowDataC = tempBufferC;
			currentFlowDataD = tempBufferD;
			currentFlowDataE = tempBufferE;
			currentFlowDataF = tempBufferF;
		}

		private float SnapPixel255(ref float counter, int threshold)
		{
			var integer = (int)counter;

			if (integer >= threshold)
			{
				counter -= integer;

				return integer / 255.0f;
			}

			return 0.0f;
		}

		private void DoFluidWetness(float delta)
		{
			if (wetness == true)
			{
				if (fluidWetnessMaterial == null)
				{
					fluidWetnessMaterial = CwHelper.CreateTempMaterial("Simulation_FluidWetness", Resources.Load<Shader>("FLOW/Simulation_FluidWetness"));
				}

				SetVariables(fluidWetnessMaterial);
				
				fluidWetnessMaterial.SetFloat(_FlowTableDepth, tableDepth);
				fluidWetnessMaterial.SetFloat(_FlowDryRate, dryRate);
				fluidWetnessMaterial.SetFloat(_FlowDelta, delta);

				var tempBuffer = CwRenderTextureManager.GetTemporary(currentFlowDataA.descriptor, "FlowSimulation tempBuffer2");

				Graphics.Blit(null, tempBuffer, fluidWetnessMaterial, 0);

				CwRenderTextureManager.ReleaseTemporary(currentFlowDataA);

				// Swap
				currentFlowDataA = tempBuffer;
			}
		}

		private Mesh particlesMesh;

		private Mesh ParticlesMesh
		{
			get
			{
				if (particlesMesh == null)
				{
					particlesMesh = new Mesh();

					var positions = new List<Vector3>();
					var coords    = new List<Vector4>();
					var indices   = new List<int>();

					for (var i = 0; i < particleLimit; i++)
					{
						var x = i;
						var y = 0;

						positions.Add(new Vector3(0.0f, 0.0f, 0.0f));
						positions.Add(new Vector3(0.0f, 0.0f, 0.0f));
						positions.Add(new Vector3(0.0f, 0.0f, 0.0f));
						positions.Add(new Vector3(0.0f, 0.0f, 0.0f));

						coords.Add(new Vector4(x, y, 0.0f, 0.0f));
						coords.Add(new Vector4(x, y, 1.0f, 0.0f));
						coords.Add(new Vector4(x, y, 0.0f, 1.0f));
						coords.Add(new Vector4(x, y, 1.0f, 1.0f));
					}

					for (var i = 0; i < particleLimit; i++)
					{
						var vertexA = i * 4;
						var vertexB = vertexA + 1;
						var vertexC = vertexA + 2;
						var vertexD = vertexA + 3;

						indices.Add(vertexA);
						indices.Add(vertexD);
						indices.Add(vertexB);

						indices.Add(vertexD);
						indices.Add(vertexA);
						indices.Add(vertexC);
					}

					particlesMesh.SetVertices(positions);
					particlesMesh.SetUVs(0, coords);
					particlesMesh.SetTriangles(indices, 0);
				}

				return particlesMesh;
			}
		}

		private void DoParticleForces(float delta)
		{
			if (ParticlesActivated == true)
			{
				if (particleForcesMaterial == null)
				{
					particleForcesMaterial = CwHelper.CreateTempMaterial("Simulation_ParticleForces", Resources.Load<Shader>("FLOW/Simulation_ParticleForces"));
				}

				SetVariables(particleForcesMaterial);
				SetParticleVariables(particleForcesMaterial);

				particleForcesMaterial.SetFloat(_FlowDelta, delta);
				particleForcesMaterial.SetVector(_PartGravity, Physics.gravity * delta);
				particleForcesMaterial.SetFloat(_PartDrag, 1.0f - CwHelper.DampenFactor(1.0f - particleDrag, delta));

				var tempBuffer = CwRenderTextureManager.GetTemporary(currentParticleDataA.descriptor, "FlowSimulation tempBuffer3");

				Graphics.Blit(null, tempBuffer, particleForcesMaterial, 0);

				CwRenderTextureManager.ReleaseTemporary(currentParticleDataA);

				// Swap
				currentParticleDataA = tempBuffer;
			}
		}

		private void DoParticleMove(float delta)
		{
			if (ParticlesActivated == true)
			{
				if (particleMoveMaterial == null)
				{
					particleMoveMaterial = CwHelper.CreateTempMaterial("Simulation_ParticleMove", Resources.Load<Shader>("FLOW/Simulation_ParticleMove"));
				}

				SetVariables(particleMoveMaterial);
				SetParticleVariables(particleMoveMaterial);

				particleMoveMaterial.SetFloat(_FlowDelta, delta);

				var tempBuffer = CwRenderTextureManager.GetTemporary(currentParticleDataB.descriptor, "FlowSimulation tempBuffer4");

				Graphics.Blit(null, tempBuffer, particleMoveMaterial, 0);

				CwRenderTextureManager.ReleaseTemporary(currentParticleDataB);

				// Swap
				currentParticleDataB = tempBuffer;
			}
		}

		private void DoParticleContribute()
		{
			if (ParticlesActivated == true)
			{
				if (particleContributeMaterial == null)
				{
					particleContributeMaterial = CwHelper.CreateTempMaterial("Simulation_ParticleContribute", Resources.Load<Shader>("FLOW/Simulation_ParticleContribute"));
				}

				SetVariables(particleContributeMaterial);
				SetParticleVariables(particleContributeMaterial);

				// Contribute to temp buffers
				FlowBuffer.Size4.Set(0, currentFlowDataC);
				FlowBuffer.Size4.Set(1, currentFlowDataD);
				FlowBuffer.Size4.Set(2, currentFlowDataE);
				FlowBuffer.Size4.Set(3, currentFlowDataF);

				FlowBuffer.Size4.SetRenderTargets();

				if (particleContributeMaterial.SetPass(0) == true)
				{
					Graphics.DrawMeshNow(ParticlesMesh, Matrix4x4.identity, 0);
				}

				// Copy contributions back to main buffers
				particleContributeMaterial.SetTexture(_FlowDataC, FlowBuffer.Size4.TempTextures[0]);
				particleContributeMaterial.SetTexture(_FlowDataD, FlowBuffer.Size4.TempTextures[1]);
				particleContributeMaterial.SetTexture(_FlowDataE, FlowBuffer.Size4.TempTextures[2]);
				particleContributeMaterial.SetTexture(_FlowDataF, FlowBuffer.Size4.TempTextures[3]);

				FlowBuffer.Size4.InvertAndSetRenderTargets();

				if (particleContributeMaterial.SetPass(1) == true)
				{
					Graphics.DrawMeshNow(ParticlesMesh, Matrix4x4.identity, 0);
				}

				FlowBuffer.Size4.ReleaseAll();
			}
		}

		private Bounds CalculateLocalBounds()
		{
			var boundsCenter = Vector3.zero;
			var boundsSize   = Vector3.zero;

			if (stretch == true)
			{
				boundsSize.x = size.x;
				boundsSize.z = size.z;
			}
			else
			{
				var ecc    = EstimatedColumnCount;
				var detail = Mathf.Sqrt(resolution);

				boundsSize.x = (ecc.x - 1) * (separation / detail);
				boundsSize.z = (ecc.z - 1) * (separation / detail);
			}

			if (center == false)
			{
				boundsCenter = boundsSize * 0.5f;
			}

			boundsCenter.y = (heightMin + heightMax) * 0.5f;
			boundsSize.y   = heightMax - heightMin;

			return new Bounds(boundsCenter, boundsSize);
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			var bounds  = CalculateLocalBounds();
			var corner  = bounds.min;
			var right   = Vector3.right   * bounds.size.x;
			var forward = Vector3.forward * bounds.size.z;
			var height  = new Vector3(0.0f, bounds.size.y, 0.0f);
			var pointA  = corner;
			var pointB  = corner + right;
			var pointC  = corner + forward;
			var pointD  = corner + right + forward;

			Gizmos.DrawLine(pointA, pointB);
			Gizmos.DrawLine(pointA, pointC);
			Gizmos.DrawLine(pointD, pointB);
			Gizmos.DrawLine(pointD, pointC);

			Gizmos.DrawLine(pointA, pointA + height);
			Gizmos.DrawLine(pointB, pointB + height);
			Gizmos.DrawLine(pointC, pointC + height);
			Gizmos.DrawLine(pointD, pointD + height);

			Gizmos.DrawLine(pointA + height, pointB + height);
			Gizmos.DrawLine(pointA + height, pointC + height);
			Gizmos.DrawLine(pointD + height, pointB + height);
			Gizmos.DrawLine(pointD + height, pointC + height);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSimulation;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSimulation_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Size.x <= 0.0f || t.Size.z <= 0.0f));
				Draw("size", "This allows you to set the size of the fluid simulation along the X and Z axes in local space.\n\nNOTE: The Y value is ignored.");
			EndError();
			BeginError(Any(tgts, t => t.Separation <= 0.0f));
				Draw("separation", "This allows you to set the distance between each fluid column on the X and Z axes in local space. The lower you set this, the higher detail the simulation will be, and the lower the performance.");
			EndError();
			BeginError(Any(tgts, t => t.Resolution <= 0.0f));
				Draw("resolution", "The amount of fluid columns used in the simulation will be multiplied by this, giving higher detail.\n\nNOTE: Increasing this will slow down performance.\n\nNOTE: The way the fluid physics behave will slightly change when changing this value, so higher isn't necessarily better depending on the look you want.");
			EndError();
			Draw("center", "Should the fluid boundary be centered, or start from the corner at 0,0?");
			Draw("stretch", "If the <b>Size</b> setting isn't perfectly divisible by the <b>Separation</b> setting, should the separation automatically be adjusted so the fluid volume matches the size?");
			Draw("simulating", "If you disable this, then the simulation will stop/freeze in time.");
			Draw("wetness", "Simulate ground wetness?\n\nNOTE: This requires the objects in your scene to use a shader/material that implements FLOW wetness. This can be done with the provided FLOW wetness shaders, manually implemented using a custom shader, or added on top using <b>BetterShaders</b> with the <b>Wetness</b> stacked shader that comes with FLOW.");
			if (Any(tgts, t => t.Wetness == true))
			{
				BeginIndent();
					Draw("tableDepth", "When the ground is dry, the water table will be this many meters below ground.");
					Draw("dryRate", "The wet areas of the scene will dry at this speed in meters per second.");
				EndIndent();
			}
			Draw("particles", "If you enable this then you will be able to spawn particles into this fluid simulation.");
			if (Any(tgts, t => t.Particles == true))
			{
				BeginIndent();
					Draw("particleLimit", "This allows you to set the maximum amount of particles that can be simulated.", "Limit");
					Draw("particleDrag", "The particles will be slowed down by this amount of atmospheric drag.", "Drag");
				EndIndent();
			}

			Separator();

			BeginDisabled();
				EditorGUILayout.IntField(new GUIContent("Total Columns", "This simulation is currently processing this many columns of fluid."), tgt.ColumnCount.x * tgt.ColumnCount.z);
			EndDisabled();

			if (tgt.Activated == false)
			{
				var ecc = tgt.EstimatedColumnCount;

				Info("Based on these settings, this simulation will use " + Mathf.Max(0, ecc.x * ecc.z) + " columns of fluid when activated.");
			}

			Separator();

			Draw("heightLayers", "This allows you to define which layers will be raycast to find the ground heights.");
			BeginError(Any(tgts, t => t.HeightMin >= t.HeightMax));
				Draw("heightMin", "This allows you to specify the minimum ground height.");
				Draw("heightMax", "This allows you to specify the maximum ground height.");
			EndError();
			Draw("heightRadius", "If you want the height sampling to have radius, specify it here. This is useful if your scene contains many thin objects that are normally missed by the ground height raycasts.\n\nNOTE: The higher you set this, the higher the disparity between the fluid and ground rendering can become.\n\n0 = No radius.\n\n1 = Radius matches column separation.");

			Separator();

			Draw("speed", "When the surface is flowing, how fast should it be considered to be moving relative to the change in fluid level? This is used when calculating the buoyancy drag.");
			Draw("foamClearRate", "The speed of the foam removal where 1 means it takes 1 second.");

			Separator();

			Draw("physicsModel", "This allows you to change the physics model of the fluid simulation.\n\nStandard = The water will flow downhill and settle.\n\nAlive = The water will flow downhill and produce many waves that constantly appear.\n\nInverse Peaks = The water will flow downhill and collect toward the center of the body of water.\n\nSimple = Water will only flow down and make simple waves/ripples, waves will not appear to have any inertia when going uphill.");
			BeginIndent();
				Draw("physicsInstability", "If you increase this setting then low viscosity waves will not be able to settle down, and will always have some instability that looks like waves.", "Instability");
				Draw("physicsDamping", "The gives you control over the rate of acceleration of a fluid column, where a higher value means it accelerates faster.", "Damping");
				Draw("physicsSpread", "By default, high viscosity fluids will stick to the ground. If you don't want this behavior then you can increase this value.", "Spread");
				Draw("physicsSpeed", "The gives you some control over the fluid simulation speed.\n\nNOTE: This is a simple approximation.", "Speed");
				Draw("physicsOverflow", "This allows the fluid flow rate to exceed the available fluid in the column. This can be combined with strong AddForce modifiers to make larger waves.\n\nNOTE: Enabling this has a slight performance overhead.", "Overflow (Experimental)");
			EndIndent();

			Separator();

			DrawAdvanced();
		}

		private void DrawAdvanced()
		{
			if (DrawFoldout("Advanced", "Show advanced settings?") == true)
			{
				BeginIndent();
					Draw("customData", "If you enable this then the fluid simulation will store up to three additional custom fluid properties.");
					Draw("precisionA", "This allows you to control the precision of the fluid mixing of the A texture (ground height, wet height). This should normally be kept at full, but it can be lowered if runtime memory usage is a concern.\n\nHalf = 16 bits of precision.\n\nFull = 32 bits of precision.");
					Draw("precisionB", "This allows you to control the precision of the fluid mixing of the B texture (velocity). This should normally be kept at full, but it can be lowered if runtime memory usage is a concern.\n\nHalf = 16 bits of precision.\n\nFull = 32 bits of precision.");
					Draw("precisionC", "This allows you to control the precision of the fluid mixing of the C texture (depth). This should normally be kept at full, but it can be lowered if runtime memory usage is a concern.\n\nHalf = 16 bits of precision.\n\nFull = 32 bits of precision.");
					Draw("precisionD", "This allows you to control the precision of the fluid mixing of the D texture (red, green, blue, alpha). If your game requires small columns of fluid to mix with large columns, and accurately store the mixed result then you can increase this.\n\nByte = 8 bits of precision.\n\nHalf = 16 bits of precision.\n\nFull = 32 bits of precision.");
					Draw("precisionE", "This allows you to control the precision of the fluid mixing of the E texture (emission, smoothness, metallic, viscosity). If your game requires small columns of fluid to mix with large columns, and accurately store the mixed result then you can increase this.\n\nByte = 8 bits of precision.\n\nHalf = 16 bits of precision.\n\nFull = 32 bits of precision.");
					Draw("precisionF", "This allows you to control the precision of the fluid mixing of the F texture (foam, custom1, custom2, custom3). If your game requires small columns of fluid to mix with large columns, and accurately store the mixed result then you can increase this.\n\nByte = 8 bits of precision.\n\nHalf = 16 bits of precision.\n\nFull = 32 bits of precision.");
				EndIndent();
			}
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Simulation", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowSimulation.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif