using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace FLOW
{
	/// <summary>This component fades out the first material's Color.a to 0.</summary>
	[RequireComponent(typeof(Renderer))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowFadeOut")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Fade Out")]
	public class FlowFadeOut : MonoBehaviour
	{
		/// <summary>The time the fade out takes in seconds.</summary>
		public float Duration { set { duration = value; } get { return duration; } } [SerializeField] private float duration = 1.0f;

		/// <summary>The maximum opacity that will be faded from.</summary>
		public float Opacity { set { opacity = value; } get { return opacity; } } [SerializeField] private float opacity = 0.5f;

		/// <summary>This event is invoked when the fade out finishes.</summary>
		public UnityEvent OnFinished { get { if (onFinished == null) onFinished = new UnityEvent(); return onFinished; } } [SerializeField] private UnityEvent onFinished;

		[System.NonSerialized]
		private Material cachedMaterial;

		[SerializeField]
		private float alpha;

		protected virtual void OnEnable()
		{
			alpha = 1.0f;

			UpdateAlpha();
		}

		protected virtual void Update()
		{
			if (alpha > 0.0f)
			{
				var delta = 1.0f / Mathf.Max(duration, 0.01f);

				alpha = Mathf.MoveTowards(alpha, 0.0f, delta * Time.deltaTime);

				UpdateAlpha();

				if (alpha == 0.0f)
				{
					if (onFinished != null)
					{
						onFinished.Invoke();
					}
				}
			}
		}

		private void UpdateAlpha()
		{
			if (cachedMaterial == null)
			{
				cachedMaterial = GetComponent<Renderer>().material;
			}

			if (cachedMaterial != null)
			{
				var color = cachedMaterial.color;

				color.a  = 1.0f - Mathf.Sqrt(1.0f - Mathf.Pow(alpha, 2.0f));
				color.a *= opacity;

				cachedMaterial.color = color;
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowFadeOut;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowFadeOut_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("duration", "The time the fade out takes in seconds.");
			Draw("opacity", "The maximum opacity that will be faded from.");

			Separator();

			Draw("onFinished");
		}
	}
}
#endif