using UnityEngine;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component can be added to a child GameObject of the <b>FlowSimulation</b>, and allows rendering of the fluid when the camera is above the surface.</summary>
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowParticles")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Particles")]
	public class FlowParticles : MonoBehaviour
	{
		/// <summary>The fluid simulation this component will render particles for.</summary>
		public FlowSimulation Simulation { set { simulation = value; } get { return simulation; } } [SerializeField] private FlowSimulation simulation;

		[System.NonSerialized]
		private MeshFilter cachedMeshFilter;

		[System.NonSerialized]
		private bool cachedMeshFilterSet;

		[System.NonSerialized]
		private MeshRenderer cachedMeshRenderer;

		[System.NonSerialized]
		private bool cachedMeshRendererSet;

		[SerializeField]
		private int particleLimit;

		[System.NonSerialized]
		private Mesh generatedMesh;

		[System.NonSerialized]
		private MaterialPropertyBlock block;

		[System.NonSerialized]
		private FlowSimulation registeredSimulation;

		[System.NonSerialized]
		private bool dirty;

		private static List<Vector3> positions = new List<Vector3>();

		private static List<Vector2> coords0 = new List<Vector2>();

		private static List<Vector4> coords1 = new List<Vector4>();

		private static List<int> indices = new List<int>();

		private static readonly int _FlowDelta  = Shader.PropertyToID("_FlowDelta");

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

		/// <summary>This allows you create a new GameObject with the <b>FlowParticles</b> component attached.</summary>
		public static FlowParticles Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowParticles</b> component attached.</summary>
		public static FlowParticles Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Particles", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowParticles>();
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			simulation = GetComponentInParent<FlowSimulation>();

			CachedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // NOTE: Property

			if (cachedMeshRenderer.sharedMaterial == null)
			{
				var guids = UnityEditor.AssetDatabase.FindAssets("t:Material Particles");

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
				registeredSimulation.OnUpdatedParticles -= HandleUpdated;
			}
		}

		protected virtual void Update()
		{
			if (registeredSimulation != simulation)
			{
				if (registeredSimulation != null)
				{
					registeredSimulation.OnUpdatedParticles -= HandleUpdated;
				}

				registeredSimulation = simulation;

				if (registeredSimulation != null)
				{
					registeredSimulation.OnUpdatedParticles += HandleUpdated;
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
			if (block == null)
			{
				block = new MaterialPropertyBlock();
			}

			CachedMeshRenderer.GetPropertyBlock(block); // NOTE: Property

			block.SetFloat(_FlowDelta, Time.fixedDeltaTime);

			if (simulation.ParticlesActivated == true)
			{
				simulation.SetVariables(block);
				simulation.SetParticleVariables(block);
			}

			cachedMeshRenderer.SetPropertyBlock(block);
		}

		private void UpdateMesh()
		{
			if (generatedMesh != null)
			{
				if (simulation == null || dirty == true || simulation.ParticleLimit != particleLimit)
				{
					DestroyImmediate(generatedMesh);
				}
			}

			if (generatedMesh == null && simulation != null)
			{
				generatedMesh = new Mesh();
				generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

				particleLimit = simulation.ParticleLimit;

				positions.Clear();
				coords0.Clear();
				coords1.Clear();
				indices.Clear();

				Build();

				generatedMesh.SetVertices(positions);
				generatedMesh.SetUVs(0, coords0);
				generatedMesh.SetUVs(1, coords1);
				generatedMesh.SetTriangles(indices, 0);

				generatedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000.0f);
			}

			dirty = false;

			CachedMeshFilter.sharedMesh = generatedMesh;
		}

		private void Build()
		{
			for (var i = 0; i < particleLimit; i++)
			{
				var x  = i;
				var y  = 0;
				var r  = Random.value * Mathf.PI * 2.0f;

				positions.Add(new Vector3(0.0f, 0.0f, 0.0f));
				positions.Add(new Vector3(0.0f, 0.0f, 0.0f));
				positions.Add(new Vector3(0.0f, 0.0f, 0.0f));
				positions.Add(new Vector3(0.0f, 0.0f, 0.0f));

				coords0.Add(new Vector4(-0.5f, -0.5f));
				coords0.Add(new Vector4(+0.5f, -0.5f));
				coords0.Add(new Vector4(-0.5f, +0.5f));
				coords0.Add(new Vector4(+0.5f, +0.5f));

				coords1.Add(new Vector4(x, y, r, 0.0f));
				coords1.Add(new Vector4(x, y, r, 0.0f));
				coords1.Add(new Vector4(x, y, r, 0.0f));
				coords1.Add(new Vector4(x, y, r, 0.0f));
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
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowParticles;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowParticles_Editor : CwEditor
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
				Draw("simulation", ref dirty, "The fluid simulation this component will render particles for.");
			EndError();

			if (tgt.CachedMeshRenderer.sharedMaterial == null)
			{
				Error("You must set a particle material in the MeshRenderer component's Materials setting.");
			}

			if (dirty == true)
			{
				Each(tgts, t => t.MarkAsDirty(), true);
			}
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Particles", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowParticles.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif