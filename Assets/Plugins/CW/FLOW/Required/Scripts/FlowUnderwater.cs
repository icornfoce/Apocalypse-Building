using UnityEngine;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component can be added to a child GameObject of the <b>FlowSimulation</b>, and allows rendering of the fluid when the camera is above the surface.</summary>
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowUnderwater")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Underwater")]
	public class FlowUnderwater : MonoBehaviour
	{
		/// <summary>The simulation this component will render.</summary>
		public FlowSimulation Simulation { set { simulation = value; } get { return simulation; } } [SerializeField] private FlowSimulation simulation;

		/// <summary>The object that will view the underwater scene.
		/// None/null = Main Camera.</summary>
		public Transform Observer { set { observer = value; } get { return observer; } } [SerializeField] private Transform observer;

		/// <summary>The amount of tiles across the X and Z axis.</summary>
		public int TileRadius { set { if (tileRadius != value) { tileRadius = value; MarkAsDirty(); } } get { return tileRadius; } } [SerializeField] private int tileRadius = 1;

		/// <summary>The underwater mesh will be offset from the ground by this distance in world space. This is used to avoid any clipping or flickering issues.</summary>
		public float GroundOffset { set { groundOffset = value; } get { return groundOffset; } } [SerializeField] private float groundOffset = 0.1f;

		/// <summary>The underwater mesh will be offset from the surface by this distance in world space. This is used to avoid any clipping or flickering issues.</summary>
		public float SurfaceOffset { set { surfaceOffset = value; } get { return surfaceOffset; } } [SerializeField] private float surfaceOffset = 0.01f;

		[System.NonSerialized]
		private MeshFilter cachedMeshFilter;

		[System.NonSerialized]
		private bool cachedMeshFilterSet;

		[System.NonSerialized]
		private MeshRenderer cachedMeshRenderer;

		[System.NonSerialized]
		private bool cachedMeshRendererSet;

		private Mesh generatedMesh;

		private MaterialPropertyBlock block;

		private FlowSimulation registeredSimulation;

		private bool dirty = true;

		private static List<Vector3> positions = new List<Vector3>();

		private static List<Vector2> coords = new List<Vector2>();

		private static List<int> indices = new List<int>();

		private static readonly int _FlowCameraHeight  = Shader.PropertyToID("_FlowCameraHeight");
		private static readonly int _FlowGroundOffset  = Shader.PropertyToID("_FlowGroundOffset");
		private static readonly int _FlowSurfaceOffset = Shader.PropertyToID("_FlowSurfaceOffset");

		public MeshFilter CachedMeshFilter
		{
			get
			{
				if (cachedMeshFilterSet == false)
				{
					cachedMeshFilter    = GetComponent<MeshFilter>();
					cachedMeshFilterSet = true;
				}

				return cachedMeshFilter;
			}
		}

		public MeshRenderer CachedMeshRenderer
		{
			get
			{
				if (cachedMeshRendererSet == false)
				{
					cachedMeshRenderer    = GetComponent<MeshRenderer>();
					cachedMeshRendererSet = true;
				}

				return cachedMeshRenderer;
			}
		}

		public void MarkAsDirty()
		{
			dirty = true;
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowUnderwater</b> component attached.</summary>
		public static FlowUnderwater Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowUnderwater</b> component attached.</summary>
		public static FlowUnderwater Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Underwater", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowUnderwater>();
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			simulation = GetComponentInParent<FlowSimulation>();

			CachedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // NOTE: Property
			cachedMeshRenderer.receiveShadows    = false;

			if (cachedMeshRenderer.sharedMaterial == null)
			{
				var guids = UnityEditor.AssetDatabase.FindAssets("t:Material Underwater");

				foreach (var guid in guids)
				{
					var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

					if (path.Contains("FLOW/Materials") == true)
					{
						cachedMeshRenderer.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);

						break;
					}
				}
			}
		}
#endif

		protected virtual void OnDisable()
		{
			DestroyImmediate(generatedMesh);

			if (registeredSimulation != null)
			{
				registeredSimulation.OnUpdated -= HandleUpdated;
			}
		}

		protected virtual void Update()
		{
			if (registeredSimulation != simulation)
			{
				if (registeredSimulation != null)
				{
					registeredSimulation.OnUpdated -= HandleUpdated;
				}

				registeredSimulation = simulation;

				if (registeredSimulation != null)
				{
					registeredSimulation.OnUpdated += HandleUpdated;
				}
			}
		}

		protected virtual void LateUpdate()
		{
			if (simulation != null)
			{
				var local   = simulation.transform.InverseTransformPoint(GetObserverPosition()) - simulation.ColumnMin;
				var borderX = simulation.ColumnSeparation.x * tileRadius;
				var borderZ = simulation.ColumnSeparation.z * tileRadius;

				local.x = Mathf.Clamp(local.x, borderX, (simulation.ColumnCount.x - 1) * simulation.ColumnSeparation.x - borderX);
				local.z = Mathf.Clamp(local.z, borderZ, (simulation.ColumnCount.z - 1) * simulation.ColumnSeparation.z - borderZ);

				local.x -= (tileRadius - 0.5f) * simulation.ColumnSeparation.x;
				local.z -= (tileRadius - 0.5f) * simulation.ColumnSeparation.z;

				local.x -= CwHelper.Mod(local.x, simulation.ColumnSeparation.x);
				local.z -= CwHelper.Mod(local.z, simulation.ColumnSeparation.z);

				local.y = 0.0f;

				transform.position = simulation.transform.TransformPoint(local + simulation.ColumnMin);

				HandleUpdated();

				if (dirty == true)
				{
					BuildMesh();
				}
			}
		}

		private Vector3 GetObserverPosition()
		{
			if (observer != null)
			{
				return observer.position;
			}

			var mainCamera = Camera.main;

			if (mainCamera != null)
			{
				return mainCamera.transform.position;
			}

			return Vector3.zero;
		}

		private void HandleUpdated()
		{
			if (block == null)
			{
				block = new MaterialPropertyBlock();
			}

			CachedMeshRenderer.GetPropertyBlock(block); // NOTE: Property

			if (simulation.Activated == true)
			{
				simulation.SetVariables(block);
			}

			block.SetFloat(_FlowCameraHeight, GetObserverPosition().y);
			block.SetFloat(_FlowGroundOffset, groundOffset);
			block.SetFloat(_FlowSurfaceOffset, surfaceOffset);

			cachedMeshRenderer.SetPropertyBlock(block);
		}

		private void BuildMesh()
		{
			if (generatedMesh == null)
			{
				generatedMesh = new Mesh();
				generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			}
			else
			{
				generatedMesh.Clear();
			}

			positions.Clear();
			coords.Clear();
			indices.Clear();

			var tiles     = tileRadius * 2;
			var vertsX    = tiles + 1;
			var pointSize = simulation.ColumnSeparation;

			BuildTop(tiles, vertsX, pointSize);
			BuildBottom(tiles, vertsX, pointSize);
			BuildEdge(0, 0, 1, 0, tiles, pointSize);
			BuildEdge(0, tiles, 0, -1, tiles, pointSize);
			BuildEdge(tiles, 0, 0, 1, tiles, pointSize);
			BuildEdge(tiles, tiles, -1, 0, tiles, pointSize);

			generatedMesh.SetVertices(positions);
			generatedMesh.SetUVs(0, coords);
			generatedMesh.SetTriangles(indices, 0);

			generatedMesh.RecalculateBounds();
			generatedMesh.bounds = new Bounds(generatedMesh.bounds.center, generatedMesh.bounds.size + Vector3.up * 1000.0f);

			CachedMeshFilter.sharedMesh = generatedMesh; // NOTE: Property

			dirty = false;
		}

		private void BuildTop(int tiles, int vertsX, Vector3 pointSize)
		{
			var vertex = positions.Count;

			for (var z = 0; z <= tiles; z++)
			{
				for (var x = 0; x <= tiles; x++)
				{
					positions.Add(new Vector3(x * pointSize.x, 1.0f, z * pointSize.z));
					coords.Add(Vector2.zero);
				}
			}

			for (var z = 0; z < tiles; z++)
			{
				for (var x = 0; x < tiles; x++)
				{
					var vertexA = vertex + x + z * vertsX;
					var vertexB = vertexA + 1;
					var vertexC = vertexA + vertsX;
					var vertexD = vertexC + 1;

					indices.Add(vertexA); indices.Add(vertexB); indices.Add(vertexD);
					indices.Add(vertexD); indices.Add(vertexC); indices.Add(vertexA);
				}
			}
		}

		private void BuildBottom(int tiles, int vertsX, Vector3 pointSize)
		{
			var vertex = positions.Count;

			for (var z = 0; z <= tiles; z++)
			{
				for (var x = 0; x <= tiles; x++)
				{
					positions.Add(new Vector3(x * pointSize.x, 0.0f, z * pointSize.z));
					coords.Add(Vector2.zero);
				}
			}

			for (var z = 0; z < tiles; z++)
			{
				for (var x = 0; x < tiles; x++)
				{
					var vertexA = vertex + x + z * vertsX;
					var vertexB = vertexA + 1;
					var vertexC = vertexA + vertsX;
					var vertexD = vertexC + 1;

					indices.Add(vertexA); indices.Add(vertexD); indices.Add(vertexB);
					indices.Add(vertexD); indices.Add(vertexA); indices.Add(vertexC);
				}
			}
		}

		private void BuildEdge(int x, int z, int stepX, int stepZ, int tiles, Vector3 pointSize)
		{
			var vertex = positions.Count;

			for (var i = 0; i <= tiles; i++)
			{
				positions.Add(new Vector3(x * pointSize.x, 1.0f, z * pointSize.z));
				coords.Add(Vector2.zero);

				positions.Add(new Vector3(x * pointSize.x, 0.0f, z * pointSize.z));
				coords.Add(Vector2.zero);

				x += stepX;
				z += stepZ;
			}

			for (var i = 0; i < tiles; i++)
			{
				var vertexA = vertex + i * 2;
				var vertexB = vertexA + 1;
				var vertexC = vertexA + 2;
				var vertexD = vertexA + 3;

				indices.Add(vertexA); indices.Add(vertexB); indices.Add(vertexD);
				indices.Add(vertexD); indices.Add(vertexC); indices.Add(vertexA);
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowUnderwater;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowUnderwater_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			var markAsDirty = false;

			if (tgt.Simulation != null)
			{
				if (tgt.Simulation.transform != tgt.transform.parent)
				{
					Warning("This GameObject should be the child of the simulation.");
				}

				if (tgt.transform.localPosition != Vector3.zero)
				{
					Warning("This GameObject should have a local scale of 0.");
				}

				if (tgt.transform.localRotation != Quaternion.identity)
				{
					Warning("This GameObject should have a local rotation of 0.");
				}

				if (tgt.transform.localScale != Vector3.one)
				{
					Warning("This GameObject should have a local scale of 1.");
				}
			}

			BeginError(Any(tgts, t => t.Simulation == null));
				Draw("simulation", "The simulation this component will render.");
			EndError();
			Draw("observer", "The object that will view the underwater scene.\n\nNone/null = Main Camera.");
			BeginError(Any(tgts, t => t.TileRadius < 1));
				Draw("tileRadius", ref markAsDirty, "The amount of tiles across the X and Z axis.");
			EndError();
			BeginError(Any(tgts, t => t.GroundOffset < 0.0f));
				Draw("groundOffset", "The underwater mesh will be offset from the ground by this distance in world space. This is used to avoid any clipping or flickering issues.");
			EndError();
			BeginError(Any(tgts, t => t.SurfaceOffset < 0.0f));
				Draw("surfaceOffset", "The underwater mesh will be offset from the surface by this distance in world space. This is used to avoid any clipping or flickering issues.");
			EndError();

			if (tgt.CachedMeshRenderer.sharedMaterial == null)
			{
				Error("You must set an underwater material in the MeshRenderer component's Materials setting.");
			}

			if (markAsDirty == true)
			{
				Each(tgts, t => t.MarkAsDirty(), true);
			}
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Underwater", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowUnderwater.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif