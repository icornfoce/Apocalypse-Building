using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component randomizes the settings of the specified <b>FlowFluid</b> component.
	/// This component automatically activates when enabled, or when you manually call the <b>Randomize</b> method.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowRandomizeFluid")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Randomize Fluid")]
	public class FlowRandomizeFluid : MonoBehaviour
	{
		/// <summary>The fluid whose settings will be modified.</summary>
		public FlowFluid Fluid { set { fluid = value; } get { return fluid; } } [SerializeField] private FlowFluid fluid;

		/// <summary>The possible colors.</summary>
		public Gradient Colors { get { return colors; } } [SerializeField] private Gradient colors = null;

		/// <summary>The min/max emission value.</summary>
		public Vector2 Emission { set { emission = value; } get { return emission; } } [SerializeField] private Vector2 emission;

		/// <summary>The min/max smoothness value.</summary>
		public Vector2 Smoothness { set { smoothness = value; } get { return smoothness; } } [SerializeField] private Vector2 smoothness;

		/// <summary>The min/max metallic value.</summary>
		public Vector2 Metallic { set { metallic = value; } get { return metallic; } } [SerializeField] private Vector2 metallic;

		/// <summary>The min/max viscosity value.</summary>
		public Vector2 Viscosity { set { viscosity = value; } get { return viscosity; } } [SerializeField] private Vector2 viscosity;

		[SerializeField]
		private float alpha;

		/// <summary>This method will immediately randomize the <b>Target</b> fluid settings.</summary>
		[ContextMenu("Randomize")]
		public void Randomize()
		{
			if (fluid != null)
			{
				if (colors != null)
				{
					fluid.Color = colors.Evaluate(Random.value);
				}

				fluid.Emission   = Random.Range(  emission.x,   emission.y);
				fluid.Smoothness = Random.Range(smoothness.x, smoothness.y);
				fluid.Metallic   = Random.Range(  metallic.x,   metallic.y);
				fluid.Viscosity  = Random.Range( viscosity.x,  viscosity.y);
			}
		}

		protected virtual void OnEnable()
		{
			Randomize();
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowRandomizeFluid;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowRandomizeFluid_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Fluid == null));
				Draw("fluid", "The fluid whose settings will be modified.");
			EndError();

			Draw("colors", "The possible colors.");
			DrawMinMax("emission", 0.0f, 1.0f, "The min/max emission value.");
			DrawMinMax("smoothness", 0.0f, 1.0f, "The min/max smoothness value.");
			DrawMinMax("metallic", 0.0f, 1.0f, "The min/max metallic value.");
			DrawMinMax("viscosity", 0.0f, 1.0f, "The min/max viscosity value.");
		}
	}
}
#endif