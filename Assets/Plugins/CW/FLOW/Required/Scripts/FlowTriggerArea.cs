using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you to trigger an event when the specified <b>FlowSampleArea</b> meets the criteria.
	/// NOTE: The trigger will not work properly if it's underground.
	/// NOTE: If you only want the trigger to work once, you can disable this component via the <b>OnMet</b> event.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowTriggerArea")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Trigger Area")]
	public class FlowTriggerArea : MonoBehaviour
	{
		public enum CriteriaType
		{
			TotalFluidVolumeAbove = 0,
			TotalFluidVolumeBelow = 1,
			TotalFluidVolumeWithin = 2,
		}

		[System.Serializable] public class FloatEvent : UnityEvent<float> {}

		/// <summary>The trigger will be calculated using this sample area.</summary>
		public FlowSampleArea SampleArea { set { sampleArea = value; } get { return sampleArea; } } [SerializeField] private FlowSampleArea sampleArea;

		/// <summary>The specified <b>SampleArea</b> must meet this criteria to trigger the event.
		/// TotalFluidVolumeAbove = The total volume of fluid within the specified <b>SampleArea</b> must be greater than the specified <b>VolumeMin</b> value.
		/// TotalFluidVolumeBelow = The total volume of fluid within the specified <b>SampleArea</b> must be greater than the specified <b>VolumeMin</b> value.
		/// TotalFluidVolumeWithin = The fluid height must be above this trigger's <b>Transform.position.y</b>.</summary>
		public CriteriaType Criteria { set { criteria = value; } get { return criteria; } } [SerializeField] private CriteriaType criteria;

		/// <summary>The minimum fluid volume in meters cubed.</summary>
		public float VolumeMin { set { volumeMin = value; } get { return volumeMin; } } [SerializeField] private float volumeMin = 1.0f;

		/// <summary>The maximum fluid volume in meters cubed.</summary>
		public float VolumeMax { set { volumeMax = value; } get { return volumeMax; } } [SerializeField] private float volumeMax = 1.0f;

		/// <summary>Has the specified <b>Criteria</b> been met?
		/// NOTE: Manually changing this will not invoke any events.</summary>
		public bool Met { set { met = value; } get { return met; } } [SerializeField] private bool met;

		/// <summary>This event will be invoked when the criteria is met.</summary>
		public UnityEvent OnMet { get { return onMet; } } [SerializeField] private UnityEvent onMet = null;

		/// <summary>This event will be invoked when the criteria is no longer met.</summary>
		public UnityEvent OnUnmet { get { return onUnmet; } } [SerializeField] private UnityEvent onUnmet = null;

		/// <summary>This will automatically reset the <b>SampleArea</b> based on any child GameObjects that contain a <b>FlowSampleArea</b>.</summary>
		[ContextMenu("Reset Sample Area")]
		public void ResetSampleArea()
		{
			sampleArea = GetComponentInChildren<FlowSampleArea>();
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
			if (sampleArea != null && sampleArea.Sampled == true)
			{
				switch (criteria)
				{
					case CriteriaType.TotalFluidVolumeAbove: return sampleArea.TotalFluidVolume > volumeMin;
					case CriteriaType.TotalFluidVolumeBelow: return sampleArea.TotalFluidVolume < volumeMin;
					case CriteriaType.TotalFluidVolumeWithin: return sampleArea.TotalFluidVolume >= volumeMin && sampleArea.TotalFluidVolume <= volumeMax;
				}
			}

			return false;
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowTrigger</b> component attached.</summary>
		public static FlowTriggerArea Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowParticles</b> component attached.</summary>
		public static FlowTriggerArea Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Trigger Area", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowTriggerArea>();
		}

		protected virtual void Update()
		{
			UpdateCriteria();
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowTriggerArea;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowTriggerArea_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.SampleArea == null));
				Draw("sampleArea", "The trigger will be calculated using this sample area.");
			EndError();
			Draw("criteria", "The specified <b>Sample</b> must meet this criteria to trigger the event.\n\nFluidDepthAbove = The fluid depth must be greater than the specified <b>Depth</b> value.\n\nFluidDepthBelow = Inverse of <b>FluidDepthAbove</b>.\n\nFluidHeightAbovePosition = The fluid height must be above this trigger's <b>Transform.position.y</b>.\n\nFluidHeightBelowPosition = Inverse of <b>FluidHeightAbovePosition</b>.");
			BeginIndent();
				if (Any(tgts, t => t.Criteria == FlowTriggerArea.CriteriaType.TotalFluidVolumeAbove || t.Criteria == FlowTriggerArea.CriteriaType.TotalFluidVolumeWithin))
				{
					Draw("volumeMin", "The minimum fluid volume in meters cubed.");
				}
				if (Any(tgts, t => t.Criteria == FlowTriggerArea.CriteriaType.TotalFluidVolumeBelow || t.Criteria == FlowTriggerArea.CriteriaType.TotalFluidVolumeWithin))
				{
					Draw("volumeMax", "The maximum fluid volume in meters cubed.");
				}
			EndIndent();

			Separator();

			if (Any(tgts, t => t.SampleArea == null))
			{
				if (HelpButton("This component has no SampleArea set, so it cannot trigger.", MessageType.Info, "Add", 40) == true)
				{
					Each(tgts, t => { var child = FlowSample.Create(t.gameObject.layer, t.transform); CwHelper.SelectAndPing(child); }, true);
				}
			}

			Separator();

			Draw("met", "Has the specified <b>Criteria</b> been met?\n\nNOTE: Manually changing this will not invoke any events.");
			Draw("onMet");
			Draw("onUnmet");
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Trigger Area", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowTriggerArea.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Sample Area + Trigger Area", false, 10)]
		public static void CreateMenuItem2()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowSampleArea.Create(parent != null ? parent.gameObject.layer : 0, parent);

			instance.gameObject.AddComponent<FlowTriggerArea>().SampleArea = instance;

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif