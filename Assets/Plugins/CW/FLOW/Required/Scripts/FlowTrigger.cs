using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you to trigger an event when the specified <b>FlowSample</b> meets the criteria.
	/// NOTE: The trigger will not work properly if it's underground.
	/// NOTE: If you only want the trigger to work once, you can disable this component via the <b>OnMet</b> event.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowTrigger")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Trigger")]
	public class FlowTrigger : MonoBehaviour
	{
		public enum CriteriaType
		{
			FluidDepthAbove = 0,
			FluidDepthBelow = 1,
			FluidHeightAbovePosition = 10,
			FluidHeightBelowPosition = 11,
		}

		[System.Serializable] public class FloatEvent : UnityEvent<float> {}

		/// <summary>The trigger will be calculated using this sample.</summary>
		public FlowSample Sample { set { sample = value; } get { return sample; } } [SerializeField] private FlowSample sample;

		/// <summary>The specified <b>Sample</b> must meet this criteria to trigger the event.
		/// FluidDepthAbove = The fluid depth must be greater than the specified <b>Depth</b> value.
		/// FluidDepthBelow = Inverse of <b>FluidDepthAbove</b>.
		/// FluidHeightAbovePosition = The fluid height must be above this trigger's <b>Transform.position.y</b>.
		/// FluidHeightBelowPosition = Inverse of <b>FluidHeightAbovePosition</b>.</summary>
		public CriteriaType Criteria { set { criteria = value; } get { return criteria; } } [SerializeField] private CriteriaType criteria;

		/// <summary>The world space depth.</summary>
		public float Depth { set { depth = value; } get { return depth; } } [SerializeField] private float depth = 1.0f;

		/// <summary>Has the specified <b>Criteria</b> been met?
		/// NOTE: Manually changing this will not invoke any events.</summary>
		public bool Met { set { met = value; } get { return met; } } [SerializeField] private bool met;

		/// <summary>This event will be invoked when the criteria is met.</summary>
		public UnityEvent OnMet { get { return onMet; } } [SerializeField] private UnityEvent onMet = null;

		/// <summary>This event will be invoked when the criteria is no longer met.</summary>
		public UnityEvent OnUnmet { get { return onUnmet; } } [SerializeField] private UnityEvent onUnmet = null;

		/// <summary>This will automatically reset the <b>Sample</b> based on any child GameObjects that contain a <b>FlowSample</b>.</summary>
		[ContextMenu("Reset Sample")]
		public void ResetSample()
		{
			Sample = GetComponentInChildren<FlowSample>();
		}

		/// <summary>This will immediately update the trigger criteria.</summary>
		[ContextMenu("Update Criteria")]
		public void UpdateCriteria()
		{
			var newMet = CalculateCriteriaMet();

			if (newMet != met)
			{
				met = newMet;

				if (met == true)
				{
					if (onMet != null)
					{
						onMet.Invoke();
					}
				}
				else
				{
					if (onUnmet != null)
					{
						onUnmet.Invoke();
					}
				}
			}
		}

		private bool CalculateCriteriaMet()
		{
			if (sample != null && sample.Sampled == true)
			{
				switch (criteria)
				{
					case CriteriaType.FluidDepthAbove: return sample.FluidDepth > depth;
					case CriteriaType.FluidDepthBelow: return sample.FluidDepth < depth;
					case CriteriaType.FluidHeightAbovePosition: return sample.FluidHeight > transform.position.y;
					case CriteriaType.FluidHeightBelowPosition: return sample.FluidHeight < transform.position.y;
				}
			}

			return false;
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowTrigger</b> component attached.</summary>
		public static FlowTrigger Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowParticles</b> component attached.</summary>
		public static FlowTrigger Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Trigger", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowTrigger>();
		}

		protected virtual void Update()
		{
			UpdateCriteria();
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			if (sample != null)
			{
				Gizmos.DrawLine(transform.position, sample.transform.position);
			}
		}
#endif
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowTrigger;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowTrigger_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Sample == null));
				Draw("sample", "The trigger will be calculated using this sample.");
			EndError();
			Draw("criteria", "The specified <b>Sample</b> must meet this criteria to trigger the event.\n\nFluidDepthAbove = The fluid depth must be greater than the specified <b>Depth</b> value.\n\nFluidDepthBelow = Inverse of <b>FluidDepthAbove</b>.\n\nFluidHeightAbovePosition = The fluid height must be above this trigger's <b>Transform.position.y</b>.\n\nFluidHeightBelowPosition = Inverse of <b>FluidHeightAbovePosition</b>.");
			BeginIndent();
				if (Any(tgts, t => t.Criteria == FlowTrigger.CriteriaType.FluidDepthAbove || t.Criteria == FlowTrigger.CriteriaType.FluidDepthBelow))
				{
					Draw("depth", "The world space depth.");
				}
			EndIndent();

			Separator();

			if (Any(tgts, t => t.Sample != null && t.Sample.Sampled == true && t.transform.position.y <= t.Sample.GroundHeight))
			{
				Warning("This trigger is underground, so it may not work as expected.");
			}

			if (Any(tgts, t => t.Sample == null))
			{
				if (HelpButton("This component has no Sample set, so it cannot trigger.", MessageType.Info, "Add", 40) == true)
				{
					Each(tgts, t => { var child = FlowSample.Create(t.gameObject.layer, t.transform); CwHelper.SelectAndPing(child); }, true);
				}
			}

			Separator();

			Draw("met", "Has the specified <b>Criteria</b> been met?\n\nNOTE: Manually changing this will not invoke any events.");
			Draw("onMet");
			Draw("onUnmet");
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Trigger", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowTrigger.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Sample + Trigger", false, 10)]
		public static void CreateMenuItem2()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowSample.Create(parent != null ? parent.gameObject.layer : 0, parent);

			instance.gameObject.AddComponent<FlowTrigger>().Sample = instance;

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif