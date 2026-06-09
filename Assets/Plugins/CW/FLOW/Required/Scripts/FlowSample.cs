using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component samples the ground and fluid data at the current transform XZ position.
	/// NOTE: This data will be sampled regardless of the Y position (height) even if this component is below the ground or above the water level.
	/// NOTE: This data will be sampled asynchronously when made available.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSample")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Sample")]
	public class FlowSample : MonoBehaviour, ISampleHandler
	{
		[System.Serializable] public class FlowSampleEvent : UnityEvent<FlowSample> {}

		/// <summary>If you only want to sample data from a specific simulation, specify it here.
		/// None/null = The closest simulation to this Transform will be sampled.</summary>
		public FlowSimulation Simulation { set { simulation = value; } get { return simulation; } } [SerializeField] private FlowSimulation simulation;

		/// <summary>This allows you to control the weight this sample gives to the <b>FlowFloat</b> component's buoyancy calculation.</summary>
		public float Strength { set { strength = value; } get { return strength; } } [SerializeField] private float strength = 1.0f;

		/// <summary>The radius of this sample.
		/// NOTE: This is only used by the buoyancy feature.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [SerializeField] private float radius = 1.0f;

		/// <summary>The fluid depth must be above this value for it to be considered to be there.</summary>
		public float Epsilon { set { epsilon = value; } get { return epsilon; } } [SerializeField] private float epsilon = 0.1f;

		/// <summary>This event is invoked after the fluid has been sampled.</summary>
		public FlowSampleEvent OnSampled { get { if (onSampled == null) onSampled = new FlowSampleEvent(); return onSampled; } } [SerializeField] private FlowSampleEvent onSampled;

		/// <summary>This event is invoked after the fluid has been sampled, if it has a depth of 0.</summary>
		public FlowSampleEvent OnSampledNothing { get { if (onSampledNothing == null) onSampledNothing = new FlowSampleEvent(); return onSampledNothing; } } [SerializeField] private FlowSampleEvent onSampledNothing;

		/// <summary>This stores all activate and enabled <b>FlowSample</b> instances in the scene.</summary>
		public static LinkedList<FlowSample> Instances { get { return instances; } } private static LinkedList<FlowSample> instances = new LinkedList<FlowSample>(); private LinkedListNode<FlowSample> instanceNode;

		/// <summary>This tells you the previously sampled ground height in world space.</summary>
		public float GroundHeight { get { return groundHeight; } } [SerializeField] private float groundHeight;

		/// <summary>This tells you the previously sampled wetness height in world space.</summary>
		public float WetHeight { get { return wetHeight; } } [SerializeField] private float wetHeight;

		/// <summary>This tells you the previously sampled fluid depth in meters.</summary>
		public float FluidDepth { get { return fluidDepth; } } [SerializeField] private float fluidDepth;

		/// <summary>This tells you the previously sampled fluid surface normal in world space.</summary>
		public Vector3 FluidNormal { get { return fluidNormal; } } [SerializeField] private Vector3 fluidNormal;

		/// <summary>This tells you the previously sampled fluid velocity in world space.</summary>
		public Vector3 FluidVelocity { get { return fluidVelocity; } } [SerializeField] private Vector3 fluidVelocity;

		/// <summary>This tells you the previously sampled fluid color.</summary>
		public Color FluidColor { get { return fluidColor; } } [SerializeField] private Color fluidColor;

		/// <summary>This tells you the previously sampled fluid emission.</summary>
		public float FluidEmission { get { return fluidEmission; } } [SerializeField] private float fluidEmission;

		/// <summary>This tells you the previously sampled fluid smoothness.</summary>
		public float FluidSmoothness { get { return fluidSmoothness; } } [SerializeField] private float fluidSmoothness;

		/// <summary>This tells you the previously sampled fluid smoothness.</summary>
		public float FluidMetallic { get { return fluidMetallic; } } [SerializeField] private float fluidMetallic;

		/// <summary>This tells you the previously sampled fluid viscosity.</summary>
		public float FluidViscosity { get { return fluidViscosity; } } [SerializeField] private float fluidViscosity;

		/// <summary>This tells you the previously sampled fluid foam.</summary>
		public float FluidFoam { get { return fluidFoam; } } [SerializeField] private float fluidFoam;

		/// <summary>This tells you the previously sampled fluid's Custom1 value.
		/// NOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>One</b> or <b>Three</b>.</summary>
		public float FluidCustom1 { get { return fluidCustom1; } } [SerializeField] private float fluidCustom1;

		/// <summary>This tells you the previously sampled fluid's Custom2 value.
		/// NOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>Three</b>.</summary>
		public float FluidCustom2 { get { return fluidCustom2; } } [SerializeField] private float fluidCustom2;

		/// <summary>This tells you the previously sampled fluid's Custom3 value.
		/// NOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>Three</b>.</summary>
		public float FluidCustom3 { get { return fluidCustom3; } } [SerializeField] private float fluidCustom3;

		/// <summary>This tells you the previously sampled simulation 0..1 overlap.</summary>
		public float Overlap { get { return overlap; } } [SerializeField] private float overlap;

		/// <summary>This tells you if this component has sampled the scene.</summary>
		public bool Sampled { get { return sampled; } } [SerializeField] private bool sampled;

		[System.NonSerialized]
		private float pendingOverlap;

		/// <summary>This tells you the height of the fluid surface in world space.</summary>
		public float FluidHeight
		{
			get
			{
				return groundHeight + fluidDepth;
			}
		}

		public float Submersion
		{
			get
			{
				var fluidHeight = groundHeight + fluidDepth;

				return Mathf.InverseLerp(fluidHeight + radius, fluidHeight - radius, transform.position.y);
			}
		}

		public void HandleSamples(FlowSimulation simulation, List<Color> samples)
		{
			// VelocityXZ, GroundHeight, WetHeight
			var sample0 = samples[0];

			fluidVelocity.x = sample0.r;
			fluidVelocity.z = sample0.g;
			groundHeight    = sample0.b;
			wetHeight       = sample0.a;

			// NormalXYZ, Depth
			var sample1 = samples[1];

			fluidNormal.x = sample1.r;
			fluidNormal.y = sample1.g;
			fluidNormal.z = sample1.b;
			fluidDepth    = sample1.a;

			// RGBA
			var sample2 = samples[2];

			fluidColor.r = sample2.r;
			fluidColor.g = sample2.g;
			fluidColor.b = sample2.b;
			fluidColor.a = sample2.a;

			// ESMV
			var sample3 = samples[3];

			fluidEmission   = sample3.r;
			fluidSmoothness = sample3.g;
			fluidMetallic   = sample3.b;
			fluidViscosity  = sample3.a;

			// F123
			var sample4 = samples[4];

			fluidFoam    = sample4.r;
			fluidCustom1 = sample4.g;
			fluidCustom2 = sample4.b;
			fluidCustom3 = sample4.a;

			overlap = pendingOverlap;
			sampled = true;

			if (fluidDepth > 0.001f)
			{
				if (onSampled != null)
				{
					onSampled.Invoke(this);
				}
			}
			else
			{
				if (onSampledNothing != null)
				{
					onSampledNothing.Invoke(this);
				}
			}
		}

		public void SetPendingOverlap(float value)
		{
			pendingOverlap = value;
		}

		/// <summary>This will clear any sampled data.</summary>
		[ContextMenu("Clear")]
		public void Clear()
		{
			if (sampled == true)
			{
				sampled = false;

				if (onSampledNothing != null)
				{
					onSampledNothing.Invoke(this);
				}
			}
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowSample</b> component attached.</summary>
		public static FlowSample Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowParticles</b> component attached.</summary>
		public static FlowSample Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Sample", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowSample>();
		}

		protected virtual void OnEnable()
		{
			FlowManager.EnsureThisComponentExists();

			instanceNode = instances.AddLast(this);
		}

		protected virtual void OnDisable()
		{
			instances.Remove(instanceNode); instanceNode = null;
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			var parentFloat = GetComponentInParent<FlowFloat>();

			if (parentFloat != null && parentFloat.Samples.Contains(this) == false)
			{
				parentFloat.Samples.Add(this); return;
			}

			var parentTrigger = GetComponentInParent<FlowTrigger>();

			if (parentTrigger != null && parentTrigger.Sample == null)
			{
				parentTrigger.Sample = this; return;
			}
		}
#endif

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			var posA = transform.position;

			if (sampled == true)
			{
				var posB = posA; posB.y = groundHeight;
				var posC = posA; posC.y = groundHeight + fluidDepth;

				Gizmos.DrawLine(posA, posB);
				Gizmos.DrawLine(posB, posC);
				Gizmos.DrawWireSphere(posA, radius);
				Gizmos.DrawWireSphere(posB, radius);
				Gizmos.DrawWireSphere(posC, radius);
				Gizmos.DrawLine(posC, posC + fluidVelocity);
			}
			else
			{
				Gizmos.DrawWireSphere(posA, radius);
			}
		}
#endif
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSample;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSample_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			if (tgt.Sampled == false)
			{
				Info("This component hasn't sampled the scene yet. Enter play mode to begin sampling.");
			}

			Draw("simulation", "If you only want to sample data from a specific simulation, specify it here.\n\nNone/null = The closest simulation to this Transform will be sampled.");
			Draw("strength", "This allows you to control the weight this sample gives to the <b>FlowFloat</b> component's buoyancy calculation.");
			Draw("radius", "The radius of this sample.\n\nNOTE: This is only used by the buoyancy feature.");
			Draw("epsilon", "The fluid depth must be above this value for it to be considered to be there.");

			Separator();

			Draw("onSampled");
			Draw("onSampledNothing", "This event is invoked after the fluid has been sampled, if it has a depth of 0.");

			if (tgt.Sampled == true)
			{
				Separator();

				BeginDisabled();
					EditorGUILayout.Slider(new GUIContent("Overlap", "This tells you the previously sampled simulation 0..1 overlap."), tgt.Overlap, 0.0f, 1.0f);
					EditorGUILayout.Slider(new GUIContent("Submersion", "This tells you the previously sampled ground height + fluid depth in world space."), tgt.Submersion, 0.0f, 1.0f);
					EditorGUILayout.FloatField(new GUIContent("Ground Height", "This tells you the previously sampled ground height in world space."), tgt.GroundHeight);
					EditorGUILayout.FloatField(new GUIContent("Wet Height", "This tells you the previously sampled wetness height in world space."), tgt.WetHeight);
					EditorGUILayout.FloatField(new GUIContent("Fluid Depth", "This tells you the previously sampled fluid depth in meters."), tgt.FluidDepth);
					EditorGUILayout.FloatField(new GUIContent("Fluid Height", "This tells you the height of the fluid surface in world space."), tgt.FluidHeight);
					EditorGUILayout.Vector3Field(new GUIContent("Fluid Normal", "This tells you the previously sampled fluid surface normal in world space."), tgt.FluidNormal);
					EditorGUILayout.Vector3Field(new GUIContent("Fluid Velocity", "This tells you the previously sampled fluid velocity in world space."), tgt.FluidVelocity);
					EditorGUILayout.ColorField(new GUIContent("Fluid Color", "This tells you the previously sampled fluid velocity in world space."), tgt.FluidColor);
					EditorGUILayout.Slider(new GUIContent("Fluid Emission", "This tells you the previously sampled fluid emission."), tgt.FluidEmission, 0.0f, 1.0f);
					EditorGUILayout.Slider(new GUIContent("Fluid Smoothness", "This tells you the previously sampled fluid smoothness."), tgt.FluidSmoothness, 0.0f, 1.0f);
					EditorGUILayout.Slider(new GUIContent("Fluid Metallic", "This tells you the previously sampled fluid metallic."), tgt.FluidMetallic, 0.0f, 1.0f);
					EditorGUILayout.Slider(new GUIContent("Fluid Viscosity", "This tells you the previously sampled fluid viscosity."), tgt.FluidViscosity, 0.0f, 1.0f);
					EditorGUILayout.Slider(new GUIContent("Fluid Foam", "This tells you the previously sampled fluid foam."), tgt.FluidFoam, 0.0f, 1.0f);
					EditorGUILayout.Slider(new GUIContent("Fluid Custom1", "This tells you the previously sampled fluid's Custom1 value.\n\nNOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>One</b> or <b>Three</b>."), tgt.FluidCustom1, 0.0f, 1.0f);
					EditorGUILayout.Slider(new GUIContent("Fluid Custom2", "This tells you the previously sampled fluid's Custom1 value.\n\nNOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>Three</b>."), tgt.FluidCustom2, 0.0f, 1.0f);
					EditorGUILayout.Slider(new GUIContent("Fluid Custom3", "This tells you the previously sampled fluid's Custom1 value.\n\nNOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>Three</b>."), tgt.FluidCustom3, 0.0f, 1.0f);
				EndDisabled();
			}
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Sample", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowSample.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif