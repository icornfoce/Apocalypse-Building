using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component works alongside the <b>LeanSample</b> component and tells you what type of fluid has been sampled based on the specified list of potential fluids.</summary>
	[RequireComponent(typeof(FlowSample))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSampleFluid")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Sample Fluid")]
	public class FlowSampleFluid : MonoBehaviour
	{
		[System.Serializable] public class FlowFluidEvent : UnityEvent<FlowFluid> {}

		[System.Serializable]
		public class Trigger
		{
			public FlowFluid Fluid;

			public UnityEvent OnSampled;
		}

		/// <summary>This allows you to specify all the possible fluid types you want this component to be able to detect.</summary>
		public List<FlowFluid> Fluids { get { if (fluids == null) fluids = new List<FlowFluid>(); return fluids; } } [SerializeField] private List<FlowFluid> fluids;

		/// <summary>Triggers allow you to fire an event when a specific fluid has been sampled by this component.</summary>
		public List<Trigger> Triggers { get { if (triggers == null) triggers = new List<Trigger>(); return triggers; } } [SerializeField] private List<Trigger> triggers;

		/// <summary>The maximum 0..1 difference in each RGBA channel value.</summary>
		public float ColorThreshold { get { return colorThreshold; } } [SerializeField] [Range(0.0f, 1.0f)] private float colorThreshold = 0.3f;

		/// <summary>The maximum 0..1 difference in emission value.</summary>
		public float EmissionThreshold { get { return emissionThreshold; } } [SerializeField] [Range(0.0f, 1.0f)] private float emissionThreshold = 0.3f;

		/// <summary>The maximum 0..1 difference in smoothness value.</summary>
		public float SmoothnessThreshold { get { return smoothnessThreshold; } } [SerializeField] [Range(0.0f, 1.0f)] private float smoothnessThreshold = 0.3f;

		/// <summary>The maximum 0..1 difference in metallic value.</summary>
		public float MetallicThreshold { get { return metallicThreshold; } } [SerializeField] [Range(0.0f, 1.0f)] private float metallicThreshold = 0.3f;

		/// <summary>This allows you to control how important the color matching is relative to the other fluid properties.</summary>
		public float ColorWeight { get { return colorWeight; } } [SerializeField] [Range(0.0f, 1.0f)] private float colorWeight = 1.0f;

		/// <summary>This allows you to control how important the emission matching is relative to the other fluid properties.</summary>
		public float EmissionWeight { get { return emissionWeight; } } [SerializeField] [Range(0.0f, 1.0f)] private float emissionWeight = 1.0f;

		/// <summary>This allows you to control how important the smoothness matching is relative to the other fluid properties.</summary>
		public float SmoothnessWeight { get { return smoothnessWeight; } } [SerializeField] [Range(0.0f, 1.0f)] private float smoothnessWeight = 1.0f;

		/// <summary>This allows you to control how important the metallic matching is relative to the other fluid properties.</summary>
		public float MetallicWeight { get { return metallicWeight; } } [SerializeField] [Range(0.0f, 1.0f)] private float metallicWeight = 1.0f;

		/// <summary>This event is invoked after the fluid has been sampled.</summary>
		public FlowFluidEvent OnSampledFluid { get { if (onSampledFluid == null) onSampledFluid = new FlowFluidEvent(); return onSampledFluid; } } [SerializeField] private FlowFluidEvent onSampledFluid;

		[System.NonSerialized]
		private FlowSample cachedSample;

		/// <summary>After <b>OnSampledFluid</b> is invoked, the newly sampled fluid will be stored here.</summary>
		public FlowFluid LastSampledFluid { get { return lastSampledFluid; } } [SerializeField] private FlowFluid lastSampledFluid;

		protected virtual void OnEnable()
		{
			cachedSample = GetComponent<FlowSample>();

			cachedSample.OnSampled.AddListener(HandleSampled);
		}

		protected virtual void OnDisable()
		{
			cachedSample.OnSampled.RemoveListener(HandleSampled);
		}

		private void HandleSampled(FlowSample sample)
		{
			var bestFluid    = default(FlowFluid);
			var bestDistance = float.PositiveInfinity;

			foreach (var fluid in fluids)
			{
				var distance = GetDistance(fluid);

				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestFluid    = fluid;
				}
			}

			lastSampledFluid = bestFluid;

			if (onSampledFluid != null)
			{
				onSampledFluid.Invoke(bestFluid);
			}

			if (triggers != null)
			{
				foreach (var trigger in triggers)
				{
					if (trigger != null && trigger.Fluid == bestFluid)
					{
						if (trigger.OnSampled != null)
						{
							trigger.OnSampled.Invoke();
						}
					}
				}
			}
		}

		private float GetDistance(FlowFluid fluid)
		{
			if (fluid != null)
			{
				var deltaR = Mathf.Abs(fluid.Color.r - cachedSample.FluidColor.r);
				var deltaG = Mathf.Abs(fluid.Color.g - cachedSample.FluidColor.g);
				var deltaB = Mathf.Abs(fluid.Color.b - cachedSample.FluidColor.b);
				var deltaA = Mathf.Abs(fluid.Color.a - cachedSample.FluidColor.a);

				if (deltaR <= colorThreshold && deltaG < colorThreshold && deltaB < colorThreshold && deltaA < colorThreshold)
				{
					var deltaE = Mathf.Abs(fluid.Emission - cachedSample.FluidEmission);
					var deltaS = Mathf.Abs(fluid.Smoothness - cachedSample.FluidSmoothness);
					var deltaM = Mathf.Abs(fluid.Metallic - cachedSample.FluidMetallic);

					if (deltaE <= emissionThreshold && deltaS <= smoothnessThreshold && deltaM <= metallicThreshold)
					{
						var distance = 0.0f;

						distance += (deltaR + deltaG + deltaB + deltaA) * colorWeight;
						distance += deltaE * emissionWeight;
						distance += deltaS * smoothnessWeight;
						distance += deltaM * metallicWeight;

						return distance;
					}
				}
			}

			return float.PositiveInfinity;
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSampleFluid;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSampleFluid_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);
			
			BeginDisabled();
				Draw("lastSampledFluid", "After <b>OnSampledFluid</b> is invoked, the newly sampled fluid will be stored here.");
			EndDisabled();
			BeginError(Any(tgts, t => t.Fluids.Count == 0));
				Draw("fluids", "This allows you to specify all the possible fluid types you want this component to be able to detect.");
			EndError();
			if (Any(tgts, t => t.Fluids.Exists(f => f != null) == false))
			{
				Error("You must set some fluids for this component to detect.");
			}
			Draw("triggers", "Triggers allow you to fire an event when a specific fluid has been sampled by this component.");

			Separator();

			Draw("colorThreshold", "The maximum 0..1 difference in each RGBA channel value.");
			Draw("emissionThreshold", "The maximum 0..1 difference in emission value.");
			Draw("smoothnessThreshold", "The maximum 0..1 difference in smoothness value.");
			Draw("metallicThreshold", "The maximum 0..1 difference in metallic value.");

			Separator();

			Draw("colorWeight", "This allows you to control how important the color matching is relative to the other fluid properties.");
			Draw("emissionWeight", "This allows you to control how important the emission matching is relative to the other fluid properties.");
			Draw("smoothnessWeight", "This allows you to control how important the smoothness matching is relative to the other fluid properties.");
			Draw("metallicWeight", "This allows you to control how important the metallic matching is relative to the other fluid properties.");

			Separator();

			Draw("onSampledFluid");
		}
	}
}
#endif