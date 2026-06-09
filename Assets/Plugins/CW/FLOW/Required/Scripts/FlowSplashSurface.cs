using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component adds a splash to the fluid simulation if the current object crosses the fluid surface.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSplashSurface")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Splash Surface")]
	public class FlowSplashSurface : MonoBehaviour
	{
		/// <summary>The submersion value will be read from this sample point.</summary>
		public FlowSample Sample { set { sample = value; } get { return sample; } } [SerializeField] private FlowSample sample = null;

		/// <summary>The splash effect that will be spawned when a splash occurs.</summary>
		public FlowSplash SplashPrefab { set { splashPrefab = value; } get { return splashPrefab; } } [SerializeField] private FlowSplash splashPrefab = null;

		/// <summary>The splash effect size will be multiplied by this.</summary>
		public float Scale { set { scale = value; } get { return scale; } } [SerializeField] private float scale = 1.0f;

		/// <summary>The splash effect strength will be multiplied by this.</summary>
		public float Strength { set { strength = value; } get { return strength; } } [SerializeField] private float strength = 1.0f;

		/// <summary>The object's submersion value must change by this much for a splash to occur.</summary>
		public float Threshold { set { threshold = value; } get { return threshold; } } [SerializeField] [Range(0.0f, 1.0f)] private float threshold = 0.25f;

		/// <summary>This disables the splash effect for some time, allowing you to use stronger strength values without having the object cause itself to splash forever.</summary>
		public float Cooldown { set { cooldown = value; } get { return cooldown; } } [SerializeField] private float cooldown = 1.0f;

		[System.NonSerialized]
		private float lastSubmersion;

		[System.NonSerialized]
		private bool lastSubmersionSet;

		[System.NonSerialized]
		private float dampen;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			sample = GetComponentInChildren<FlowSample>();
		}
#endif

		protected virtual void Update()
		{
			if (sample != null && sample.Sampled == true)
			{
				var newSubmersion = sample.Submersion;

				if (lastSubmersionSet == false)
				{
					lastSubmersion    = newSubmersion;
					lastSubmersionSet = true;
				}

				if (lastSubmersion != newSubmersion && splashPrefab != null)
				{
					var delta = Mathf.Abs(lastSubmersion - newSubmersion);

					if (delta > threshold)
					{
						if (cooldown > 0.0f)
						{
							delta *= 1.0f - dampen / cooldown;
						}

						splashPrefab.Apply(transform.position, delta * strength, scale);
					}

					dampen = Mathf.Min(cooldown, dampen + delta);
				}

				lastSubmersion = newSubmersion;
			}

			dampen = Mathf.Max(0.0f, dampen - Time.deltaTime);
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSplashSurface;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSplashSurface_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Sample == null));
				Draw("sample", "The submersion value will be read from this sample point.");
			EndError();

			Separator();

			BeginError(Any(tgts, t => t.SplashPrefab == null));
				Draw("splashPrefab", "The splash effect that will be spawned when a splash occurs.");
			EndError();
			BeginError(Any(tgts, t => t.Scale <= 0.0f));
				Draw("scale", "The splash effect size will be multiplied by this.");
			EndError();
			BeginError(Any(tgts, t => t.Strength <= 0.0f));
				Draw("strength", "The splash effect strength will be multiplied by this.");
			EndError();

			Separator();

			Draw("threshold", "The object's submersion value must change by this much for a splash to occur.");
			Draw("cooldown", "This disables the splash effect for some time, allowing you to use stronger strength values without having the object cause itself to splash forever.");
		}
	}
}
#endif