using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace FLOW
{
	/// <summary>This component samples an area of fluid and gives you information about it.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSampleArea")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Sample Area")]
	public class FlowSampleArea : MonoBehaviour, ISampleHandler
	{
		[System.Serializable] public class FlowSampleAreaEvent : UnityEvent<FlowSampleArea> {}

		public enum CheckType
		{
			Manually,
			Once,
			Continuously
		}

		/// <summary>If you only want to sample data from a specific simulation, specify it here.
		/// None/null = The closest simulation to this Transform will be sampled.</summary>
		public FlowSimulation Simulation { set { simulation = value; } get { return simulation; } } [SerializeField] private FlowSimulation simulation;

		/// <summary>This allows you to set the size of the modifier boundary in local space.
		/// NOTE: The Y value is ignored.</summary>
		public Vector3 Size { set { size = value; } get { return size; } } [SerializeField] private Vector3 size = new Vector3(1.0f, 0.0f, 1.0f);

		/// <summary>By default all fluid within the sample area rect will be read, but this allows you to specify a texture that acts as a mask shape.</summary>
		public Texture Shape { set { shape = value; } get { return shape; } } [SerializeField] private Texture shape;

		/// <summary>This allows you to choose which channel from the <b>Shape</b> texture will be used.</summary>
		public FlowChannel ShapeChannel { set { shapeChannel = value; } get { return shapeChannel; } } [SerializeField] private FlowChannel shapeChannel = FlowChannel.Alpha;

		/// <summary>Should the modifier's boundary be centered, or start from the corner</summary>
		public bool Center { set { center = value; } get { return center; } } [SerializeField] private bool center = true;

		/// <summary>How often should this component check the underlying fluid?
		/// Manually = You must manually call the <b>CheckNow</b> method from code or inspector event.
		/// Once = The check will occur on the first frame after this component gets activated.
		/// Continuously = The fluid will be checked every <b>CheckInterval</b> seconds.</summary>
		public CheckType Check { set { check = value; } get { return check; } } [SerializeField] private CheckType check = CheckType.Continuously;

		/// <summary>The time between each fluid sample in seconds.</summary>
		public float CheckInterval { set { checkInterval = value; } get { return checkInterval; } } [SerializeField] private float checkInterval = 1.0f;

		/// <summary>This tells you the previously sampled total fluid depth in meters.</summary>
		public float TotalFluidDepth { get { return totalFluidDepth; } } [SerializeField] private float totalFluidDepth;

		/// <summary>This tells you the previously sampled fluid volume in meters cubed.</summary>
		public float TotalFluidVolume { get { return totalFluidVolume; } } [SerializeField] private float totalFluidVolume;

		/// <summary>This tells you the previously sampled deepest fluid pixel coordinate.</summary>
		public Vector2Int DeepestColumn { get { return deepestColumn; } } [SerializeField] private Vector2Int deepestColumn;

		/// <summary>This tells you the previously sampled deepest fluid depth in meters.</summary>
		public float DeepestFluidDepth { get { return deepestFluidDepth; } } [SerializeField] private float deepestFluidDepth;

		/// <summary>This tells you the previously sampled deepest ground position in world space.</summary>
		public Vector3 DeepestGroundPosition { get { return deepestGroundPosition; } } [SerializeField] private Vector3 deepestGroundPosition;

		/// <summary>This tells you the previously sampled deepest fluid surface position in world space.</summary>
		public Vector3 DeepestFluidPosition { get { return deepestFluidPosition; } } [SerializeField] private Vector3 deepestFluidPosition;

		/// <summary>This tells you if this component has sampled the scene.</summary>
		public bool Sampled { get { return sampled; } } [SerializeField] private bool sampled;

		/// <summary>This event is invoked after the fluid has been sampled.</summary>
		public FlowSampleAreaEvent OnSampled { get { if (onSampled == null) onSampled = new FlowSampleAreaEvent(); return onSampled; } } [SerializeField] private FlowSampleAreaEvent onSampled;

		private bool primed = true;

		[System.NonSerialized]
		private float counter;

		[System.NonSerialized]
		private FlowReader reader;

		[System.NonSerialized]
		private RectInt readerPixels;

		/// <summary>This allows you create a new GameObject with the <b>FlowSample</b> component attached.</summary>
		public static FlowSampleArea Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowParticles</b> component attached.</summary>
		public static FlowSampleArea Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Sample Area", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowSampleArea>();
		}

		public Bounds CalculateLocalBounds()
		{
			var boundsCenter = Vector3.zero;
			var boundsSize   = new Vector3(size.x, 0.0f, size.z);

			if (center == false)
			{
				boundsCenter.x = boundsSize.x * 0.5f;
				boundsCenter.z = boundsSize.z * 0.5f;
			}

			return new Bounds(boundsCenter, boundsSize);
		}

		protected virtual void Update()
		{
			if (check == CheckType.Continuously)
			{
				counter += Time.deltaTime;

				if (counter >= checkInterval)
				{
					primed = true;
				}
			}
				
			if (primed == true)
			{
				CheckNow();
			}
		}

		[ContextMenu("Check Now")]
		public void CheckNow()
		{
			if (reader == null)
			{
				var finalSimulation = FlowSimulation.FindSimulation(transform.position, simulation);

				if (finalSimulation != null && finalSimulation.Activated == true)
				{
					var localBounds = CalculateLocalBounds();
					var worldBounds = FlowCommon.CalculateWorldBounds(transform, localBounds);
					var pixels      = finalSimulation.CalculatePixelRect(worldBounds);

					if (pixels.width * pixels.height > 0)
					{
						var shapeMatrix = Matrix4x4.Scale(new Vector3(1.0f / localBounds.size.x, 1.0f, 1.0f / localBounds.size.z)) * Matrix4x4.Translate(new Vector3(-localBounds.min.x, 0.0f, -localBounds.min.z)) * transform.worldToLocalMatrix;
						var shapePlane  = new Plane(transform.up, transform.position);

						primed       = false;
						counter      = checkInterval > 0.0f ? counter % checkInterval : 0.0f;
						reader       = FlowReader.SampleFluidArea(finalSimulation, pixels, shape, shapeChannel, shapeMatrix, shapePlane, this);
						readerPixels = pixels;
					}
				}
			}
		}

		public void HandleSamples(FlowSimulation simulation, List<Color> samples)
		{
			reader            = null;
			totalFluidDepth   = 0.0f;
			deepestFluidDepth = 0.0f;

			var deepestGroundHeight = 0.0f;

			for (var x = 0; x < samples.Count; x++)
			{
				var sample = samples[x]; // Depth Total, Deepest Depth, Deepest Y, Deepest Height

				totalFluidDepth += sample.r;

				if (sample.g > deepestFluidDepth)
				{
					deepestFluidDepth   = sample.g;
					deepestGroundHeight = sample.a;

					deepestColumn.x = readerPixels.x + x;
					deepestColumn.y = (int)sample.b;
				}
			}

			deepestGroundPosition   = simulation.GetWorldToPixelMatrix().inverse.MultiplyPoint((Vector2)deepestColumn);
			deepestGroundPosition.y = deepestGroundHeight;

			deepestFluidPosition    = deepestGroundPosition;
			deepestFluidPosition.y += deepestFluidDepth;

			totalFluidVolume = totalFluidDepth * simulation.VolumePerColumn;

			sampled = true;

			if (onSampled != null)
			{
				onSampled.Invoke(this);
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			if (sampled == true)
			{
				Gizmos.DrawWireSphere(deepestGroundPosition, 1.0f);
				Gizmos.DrawWireSphere(deepestGroundPosition + Vector3.up * deepestFluidDepth, 1.0f);
			}

			DrawGizmosHeight();

			DrawGizmosEdges(-1000.0f, 1000.0f);

			if (shape != null)
			{
				var localBounds    = CalculateLocalBounds();
				var modifierMatrix = transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(localBounds.min.x, 0.0f, localBounds.min.z)) * Matrix4x4.Scale(new Vector3(localBounds.size.x, 1.0f, localBounds.size.z));

				modifierMatrix *= Matrix4x4.Rotate(Quaternion.Euler(90.0f, 0.0f, 0.0f));
				modifierMatrix *= Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.0f));

				FlowCommon.DrawShapeOutline(shape, shapeChannel, modifierMatrix);
			}
		}

		private void DrawGizmosEdges(float verticalA, float verticalB)
		{
			var bounds  = CalculateLocalBounds();
			var corner  = transform.TransformPoint(bounds.min);
			var right   = transform.TransformPoint(bounds.min + Vector3.right   * bounds.size.x) - corner;
			var forward = transform.TransformPoint(bounds.min + Vector3.forward * bounds.size.z) - corner;
			var cornerA = corner + Vector3.up * verticalA;
			var cornerB = corner + Vector3.up * verticalB;

			Gizmos.DrawLine(cornerA, cornerB);
			Gizmos.DrawLine(cornerA + right, cornerB + right);
			Gizmos.DrawLine(cornerA + forward, cornerB + forward);
			Gizmos.DrawLine(cornerA + right + forward, cornerB + right + forward);
		}

		private void DrawGizmosHeight()
		{
			var bounds  = CalculateLocalBounds();
			var corner  = transform.TransformPoint(bounds.min);
			var right   = transform.TransformPoint(bounds.min + Vector3.right   * bounds.size.x) - corner;
			var forward = transform.TransformPoint(bounds.min + Vector3.forward * bounds.size.z) - corner;

			Gizmos.DrawLine(corner, corner + right);
			Gizmos.DrawLine(corner, corner + forward);
			Gizmos.DrawLine(corner + right + forward, corner + right);
			Gizmos.DrawLine(corner + right + forward, corner + forward);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSampleArea;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSampleArea_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			if (tgt.Sampled == false)
			{
				Info("This component hasn't sampled the scene yet. Enter play mode to begin sampling.");
			}

			Draw("simulation", "If you only want to sample data from a specific simulation, specify it here.\n\nNone/null = The closest simulation to this Transform will be sampled.");
			BeginError(Any(tgts, t => t.Size.x <= 0.0f || t.Size.z <= 0.0f));
				Draw("size", "This allows you to set the size of the modifier boundary in local space.\n\nNOTE: The Y value is ignored.");
			EndError();
			EditorGUILayout.BeginHorizontal();
				Draw("shape", "By default all fluid within the sample area rect will be read, but this allows you to specify a texture that acts as a mask shape.");
				EditorGUILayout.PropertyField(serializedObject.FindProperty("shapeChannel"), GUIContent.none, GUILayout.Width(60));
			EditorGUILayout.EndHorizontal();
			Draw("center", "Should the modifier's boundary be centered, or start from the corner?");

			Draw("check", "How often should this component check the underlying fluid?\n\nManually = You must manually call the <b>CheckNow</b> method from code or inspector event.\n\nOnce = The check will occur on the first frame after this component gets activated.\n\nContinuously = The fluid will be checked every <b>CheckInterval</b> seconds.");
			if (Any(tgts, t => t.Check == TARGET.CheckType.Continuously))
			{
				BeginIndent();
					Draw("checkInterval", "The time between each fluid sample in seconds.", "Interval");
				EndIndent();
			}

			Separator();

			Draw("onSampled");

			if (tgt.Sampled == true)
			{
				Separator();

				BeginDisabled();
					EditorGUILayout.FloatField(new GUIContent("Total Fluid Depth", "This tells you the previously sampled total fluid depth in meters."), tgt.TotalFluidDepth);
					EditorGUILayout.FloatField(new GUIContent("Total Fluid Volume", "This tells you the previously sampled fluid volume in meters cubed."), tgt.TotalFluidVolume);
					EditorGUILayout.Vector2IntField(new GUIContent("Deepest Column", "This tells you the previously sampled deepest fluid pixel coordinate."), tgt.DeepestColumn);
					EditorGUILayout.FloatField(new GUIContent("Deepest Fluid Depth", "This tells you the previously sampled deepest fluid depth in meters."), tgt.DeepestFluidDepth);
					EditorGUILayout.Vector3Field(new GUIContent("Deepest Ground Position", "This tells you the previously sampled deepest ground position in world space."), tgt.DeepestGroundPosition);
					EditorGUILayout.Vector3Field(new GUIContent("Deepest Fluid Position", "This tells you the previously sampled deepest fluid surface position in world space."), tgt.DeepestFluidPosition);
				EndDisabled();
			}
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Sample Area", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowSampleArea.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif