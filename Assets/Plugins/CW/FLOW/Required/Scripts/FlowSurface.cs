using UnityEngine;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component can be added to a child GameObject of the <b>FlowSimulation</b>, and allows rendering of the fluid when the camera is above the surface.</summary>
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSurface")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Surface")]
	public class FlowSurface : MonoBehaviour
	{
		/// <summary>The simulation this component will render.</summary>
		public FlowSimulation Simulation { set { simulation = value; } get { return simulation; } } [SerializeField] private FlowSimulation simulation;

		/// <summary>Should this component draw the outer edge/sides of the water?</summary>
		public bool Edge { set { edge = value; } get { return edge; } } [SerializeField] private bool edge = true;

		[System.NonSerialized]
		private MeshFilter cachedMeshFilter;

		[System.NonSerialized]
		private bool cachedMeshFilterSet;

		[System.NonSerialized]
		private MeshRenderer cachedMeshRenderer;

		[System.NonSerialized]
		private bool cachedMeshRendererSet;

		[SerializeField]
		private Vector3Int pointCount;

		[SerializeField]
		private Vector3 pointSize;

		[SerializeField]
		private Vector3 pointMin;

		[System.NonSerialized]
		private Mesh generatedMesh;

		[System.NonSerialized]
		private MaterialPropertyBlock block;

		[System.NonSerialized]
		private FlowSimulation registeredSimulation;

		[System.NonSerialized]
		private bool dirty;

		private static List<Vector3> positions = new List<Vector3>();

		private static List<Vector3> normals = new List<Vector3>();

		private static List<Vector4> tangents = new List<Vector4>();

		private static List<Vector4> coords = new List<Vector4>();

		private static List<int> indices = new List<int>();

		private static List<Material> tempMaterials = new List<Material>();

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

		/// <summary>This allows you create a new GameObject with the <b>FlowSurface</b> component attached.</summary>
		public static FlowSurface Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowSurface</b> component attached.</summary>
		public static FlowSurface Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Surface", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowSurface>();
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			simulation = GetComponentInParent<FlowSimulation>();

			CachedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // NOTE: Property

			if (cachedMeshRenderer.sharedMaterial == null)
			{
				var guids = UnityEditor.AssetDatabase.FindAssets("t:Material Surface");

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
			UpdateMesh();
			HandleUpdated();
		}

		private void HandleUpdated()
		{
			if (simulation.Activated == true)
			{
				if (block == null)
				{
					block = new MaterialPropertyBlock();
				}

				CachedMeshRenderer.GetSharedMaterials(tempMaterials); // NOTE: Property

				for (var i = 0; i < tempMaterials.Count; i++)
				{
					if (tempMaterials[i] != null)
					{
						cachedMeshRenderer.GetPropertyBlock(block, i);

						simulation.SetVariables(block);

						cachedMeshRenderer.SetPropertyBlock(block);
					}
				}
			}
		}

		private void UpdateMesh()
		{
			if (generatedMesh != null)
			{
				if (simulation == null || dirty == true || simulation.ColumnCount.x != pointCount.x || simulation.ColumnCount.z != pointCount.z || simulation.ColumnSeparation.x != pointSize.x || simulation.ColumnSeparation.z != pointSize.z || simulation.ColumnMin.x != pointMin.x || simulation.ColumnMin.z != pointMin.z)
				{
					DestroyImmediate(generatedMesh);
				}
			}

			if (simulation != null)
			{
				if (generatedMesh == null)
				{
					generatedMesh = new Mesh();
					generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

					pointCount = simulation.ColumnCount;
					pointSize  = simulation.ColumnSeparation;
					pointMin   = simulation.ColumnMin;

					positions.Clear();
					coords.Clear();
					normals.Clear();
					tangents.Clear();
					indices.Clear();

					BuildTop();

					if (edge == true)
					{
						BuildEdge(Vector3.back, 0, 0, 1, 0, pointCount.x);
						BuildEdge(Vector3.right, pointCount.x - 1, 0, 0, 1, pointCount.z);
						BuildEdge(Vector3.forward, pointCount.x - 1, pointCount.z - 1, -1, 0, pointCount.x);
						BuildEdge(Vector3.left, 0, pointCount.z - 1, 0, -1, pointCount.z);
					}

					generatedMesh.SetVertices(positions);
					generatedMesh.SetUVs(0, coords);
					generatedMesh.SetNormals(normals);
					generatedMesh.SetTangents(tangents);
					generatedMesh.SetTriangles(indices, 0);

					generatedMesh.RecalculateBounds();
					generatedMesh.bounds = new Bounds(generatedMesh.bounds.center, generatedMesh.bounds.size + Vector3.up * 1000.0f);
				}

				dirty = false;

				CachedMeshFilter.sharedMesh = generatedMesh;
			}
		}

		private void BuildTop()
		{
			var vertex = positions.Count;
			var quadsX = pointCount.x - 1;
			var quadsZ = pointCount.z - 1;
			var stepX  = 1.0f / quadsX;
			var stepZ  = 1.0f / quadsZ;

			for (var z = 0; z < pointCount.z; z++)
			{
				for (var x = 0; x < pointCount.x; x++)
				{
					positions.Add(new Vector3(pointMin.x + x * pointSize.x, 1.0f, pointMin.z + z * pointSize.z));
					coords.Add(new Vector4(x * stepX, z * stepZ, 0.0f, 1.0f));
					normals.Add(new Vector3(0.0f, 1.0f, 0.0f));
					tangents.Add(new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
				}
			}

			for (var z = 0; z < quadsZ; z++)
			{
				for (var x = 0; x < quadsX; x++)
				{
					var vertexA = vertex + x + z * pointCount.x;
					var vertexB = vertexA + 1;
					var vertexC = vertexA + pointCount.x;
					var vertexD = vertexC + 1;

					indices.Add(vertexA);
					indices.Add(vertexD);
					indices.Add(vertexB);

					indices.Add(vertexD);
					indices.Add(vertexA);
					indices.Add(vertexC);
				}
			}
		}

		private void BuildEdge(Vector3 normal, int x, int z, int incX, int incZ, int points)
		{
			var vertex  = positions.Count;
			var quadsX  = pointCount.x - 1;
			var quadsZ  = pointCount.z - 1;
			var stepX   = 1.0f / quadsX;
			var stepZ   = 1.0f / quadsZ;
			var quads   = points - 1;
			var tangent = (Vector4)Vector3.Cross(normal, Vector3.up); tangent.w = 1.0f;

			for (var i = 0; i < points; i++)
			{
				positions.Add(new Vector3(pointMin.x + x * pointSize.x, 0.0f, pointMin.z + z * pointSize.z));
				coords.Add(new Vector4(x * stepX, z * stepZ, 1.0f, 0.0f));
				normals.Add(normal);
				tangents.Add(tangent);

				positions.Add(new Vector3(pointMin.x + x * pointSize.x, 1.0f, pointMin.z + z * pointSize.z));
				coords.Add(new Vector4(x * stepX, z * stepZ, 1.0f, 1.0f));
				normals.Add(normal);
				tangents.Add(tangent);

				x += incX;
				z += incZ;
			}

			for (var i = 0; i < quads; i++)
			{
				var vertexA = vertex + i * 2;
				var vertexB = vertexA + 1;
				var vertexC = vertexA + 2;
				var vertexD = vertexA + 3;

				indices.Add(vertexA);
				indices.Add(vertexB);
				indices.Add(vertexD);

				indices.Add(vertexD);
				indices.Add(vertexC);
				indices.Add(vertexA);
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSurface;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSurface_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			var dirty = false;

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
				Draw("simulation", ref dirty, "The simulation this component will render.");
			EndError();
			Draw("edge", ref dirty, "Should this component draw the outer edge/sides of the water?");

			if (tgt.CachedMeshRenderer.sharedMaterial == null)
			{
				Error("You must set surface material in the MeshRenderer component's Materials setting.");
			}

			if (dirty == true)
			{
				Each(tgts, t => t.MarkAsDirty(), true);
			}
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Surface", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowSurface.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif