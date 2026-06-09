using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component works alongside the <b>LeanSample</b> component and tells you what type of fluid has been sampled based on the specified list of potential fluids.</summary>
	[RequireComponent(typeof(FlowSampleArea))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSampleAreaDeepest")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Sample Area Deepest")]
	public class FlowSampleAreaDeepest : MonoBehaviour
	{
		[System.Serializable] public class Vector3Event : UnityEvent<Vector3> {}

		/// <summary>The deepest sampled fluid depth must be at least this depth in meters for the events to invoke.</summary>
		public float MinimumDepth { set { minimumDepth = value; } get { return minimumDepth; } } [SerializeField] private float minimumDepth;

		/// <summary>This event is invoked after the fluid has been sampled.
		/// Vector3 = The position of the ground at the deepest sampled fluid depth.</summary>
		public Vector3Event OnDeepestGroundPosition { get { if (onDeepestGroundPosition == null) onDeepestGroundPosition = new Vector3Event(); return onDeepestGroundPosition; } } [SerializeField] private Vector3Event onDeepestGroundPosition;

		/// <summary>This event is invoked after the fluid has been sampled.
		/// Vector3 = The position of the fluid surface at the deepest sampled fluid depth.</summary>
		public Vector3Event OnDeepestSurfacePosition { get { if (onDeepestSurfacePosition == null) onDeepestSurfacePosition = new Vector3Event(); return onDeepestSurfacePosition; } } [SerializeField] private Vector3Event onDeepestSurfacePosition;

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
			if (onDeepestGroundPosition != null)
			{
				onDeepestGroundPosition.Invoke(sampleArea.DeepestGroundPosition);
			}

			if (onDeepestSurfacePosition != null)
			{
				onDeepestSurfacePosition.Invoke(sampleArea.DeepestFluidPosition);
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSampleAreaDeepest;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSampleAreaDeepest_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("minimumDepth", "The deepest sampled fluid depth must be at least this depth in meters for the events to invoke.");

			Separator();

			Draw("onDeepestGroundPosition");
			Draw("onDeepestSurfacePosition");
		}
	}
}
#endif