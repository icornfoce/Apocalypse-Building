using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you to define fluid properties.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowFluid")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Fluid")]
	public class FlowFluid : MonoBehaviour
	{
		/// <summary>The color of this fluid.</summary>
		public Color Color { set { color = value; } get { return color; } } [SerializeField] private Color color = Color.white;

		/// <summary>The emission of this fluid.</summary>
		public float Emission { set { emission = value; } get { return emission; } } [SerializeField] [Range(0.0f, 1.0f)] private float emission;

		/// <summary>The PBR smoothness of this fluid.</summary>
		public float Smoothness { set { smoothness = value; } get { return smoothness; } } [SerializeField] [Range(0.0f, 1.0f)] private float smoothness;

		/// <summary>The PBR metallic of this fluid.</summary>
		public float Metallic { set { metallic = value; } get { return metallic; } } [SerializeField] [Range(0.0f, 1.0f)] private float metallic;

		/// <summary>The viscosity of this fluid.</summary>
		public float Viscosity { set { viscosity = value; } get { return viscosity; } } [SerializeField] [Range(0.0f, 1.0f)] private float viscosity = 0.1f;

		/// <summary>The first custom property of this fluid.
		/// NOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>One</b> or <b>Three</b>.</summary>
		public float Custom1 { set { custom1 = value; } get { return custom1; } } [SerializeField] [Range(0.0f, 1.0f)] private float custom1;

		/// <summary>The second custom property of this fluid.
		/// NOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>Three</b>.</summary>
		public float Custom2 { set { custom2 = value; } get { return custom2; } } [SerializeField] [Range(0.0f, 1.0f)] private float custom2;

		/// <summary>The third custom property of this fluid.
		/// NOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>Three</b>.</summary>
		public float Custom3 { set { custom3 = value; } get { return custom3; } } [SerializeField] [Range(0.0f, 1.0f)] private float custom3;
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowFluid;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowFluid_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("color", "The color of this fluid.");
			Draw("emission", "The emission of this fluid.");
			Draw("smoothness", "The PBR smoothness of this fluid.");
			Draw("metallic", "The PBR metallic of this fluid.");
			Draw("viscosity", "The viscosity of this fluid.");

			Separator();

			Draw("custom1", "The first custom property of this fluid.\n\nNOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>One</b> or <b>Three</b>.");
			Draw("custom2", "The second custom property of this fluid.\n\nNOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>Three</b>.");
			Draw("custom3", "The third custom property of this fluid.\n\nNOTE: To use this, your <b>FlowSimulation</b> component's <b>Advanced / CustomData</b> setting must be set to <b>Three</b>.");
		}

		[MenuItem("Assets/Create/FLOW/Fluid")]
		private static void CreateLanguage()
		{
			CwHelper.CreatePrefabAsset("New Fluid").AddComponent<FlowFluid>();
		}
	}
}
#endif