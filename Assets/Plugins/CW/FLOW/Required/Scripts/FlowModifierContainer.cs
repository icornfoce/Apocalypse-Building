using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component will either add or remove fluid from the specified <b>Container</b> based on the sibling <b>FlowModifier</b> component's <b>Mode</b> setting. If the container volume is too little or too much then the modifer will be disabled.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(FlowModifier))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowModifierContainer")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Modifier Container")]
	public class FlowModifierContainer : MonoBehaviour
	{
		/// <summary>The container whose <b>Volume</b> must be greater than 1 for the sibling <b>FlowModifier</b> to be enabled.</summary>
		public FlowContainer Container { set { container = value; } get { return container; } } [SerializeField] private FlowContainer container;

		[System.NonSerialized]
		private FlowModifier cachedModifier;

		[System.NonSerialized]
		private bool cachedModifierSet;

		public FlowModifier CachedModifier
		{
			get
			{
				if (cachedModifierSet == false)
				{
					cachedModifier    = GetComponent<FlowModifier>();
					cachedModifierSet = true;
				}

				return cachedModifier;
			}
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			CachedModifier.MonitorFluidDepth = true; // This is required to be enabled, so turn it on
		}
#endif

		protected virtual void OnEnable()
		{
			CachedModifier.OnFluidVolumeDelta.AddListener(HandleFluidVolumeDelta); // NOTE: Property
		}

		protected virtual void OnDisable()
		{
			cachedModifier.OnFluidVolumeDelta.RemoveListener(HandleFluidVolumeDelta);
		}

		protected virtual void Update()
		{
			if (cachedModifier.AddsFluid == true)
			{
				cachedModifier.enabled = container != null && container.Volume > 0.0f;
			}
			else if (cachedModifier.RemovesFluid == true)
			{
				cachedModifier.enabled = container != null && container.Volume < container.Capacity;
			}
		}

		private void HandleFluidVolumeDelta(FlowSimulation simulation, float delta)
		{
			if (container != null)
			{
				if (delta > 0.0f)
				{
					container.RemoveVolume(delta);
				}
				else if (delta < 0.0f)
				{
					container.AddVolume(-delta);
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowModifierContainer;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowModifierContainer_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			if (Any(tgts, t => t.CachedModifier.MonitorFluidDepth == false))
			{
				Error("The sibling FlowModifier component's MonitorFluidDepth setting is disabled, but this setting is required for this component to function.");
			}

			BeginError(Any(tgts, t => t.Container == null));
				Draw("container", "The container whose <b>Volume</b> must be greater than 1 for the sibling <b>FlowModifier</b> to be enabled.");
			EndError();
		}
	}
}
#endif