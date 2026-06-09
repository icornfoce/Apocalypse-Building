using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component works alongside the <b>LeanSample</b> component and tells you what type of fluid has been sampled based on the specified list of potential fluids.</summary>
	[RequireComponent(typeof(FlowSampleArea))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSampleAreaVolume")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Sample Area Volume")]
	public class FlowSampleAreaVolume : MonoBehaviour
	{
		[System.Serializable] public class FloatEvent : UnityEvent<float> {}

		/// <summary>This event is invoked after the fluid has been sampled.
		/// Float = The total volume in meters cubed.</summary>
		public FloatEvent OnVolume { get { if (onVolume == null) onVolume = new FloatEvent(); return onVolume; } } [SerializeField] private FloatEvent onVolume;

		[System.NonSerialized]
		private FlowSampleArea cachedSampleArea;

		protected virtual void OnEnable()
		{
			cachedSampleArea = GetComponent<FlowSampleArea>();

			cachedSampleArea.OnSampled.AddListener(HandleSampled);
		}

		protected virtual void OnDisable()
		{
			cachedSampleArea.OnSampled.RemoveListener(HandleSampled);
		}

		private void HandleSampled(FlowSampleArea sampleArea)
		{
			if (onVolume != null)
			{
				onVolume.Invoke(sampleArea.TotalFluidVolume);
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSampleAreaVolume;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSampleAreaVolume_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("onVolume");
		}
	}
}
#endif