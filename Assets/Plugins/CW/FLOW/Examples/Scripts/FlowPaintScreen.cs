using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you paint the specified fluid prefab under the mouse/finger as it drags across the screen.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowPaintScreen")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Paint Screen")]
	public class FlowPaintScreen : MonoBehaviour
	{
		/// <summary>The finger/mouse/key that must be pressed for the painting.</summary>
		public CwInputManager.Trigger PaintControls = new CwInputManager.Trigger(true, true, KeyCode.None);

		/// <summary>The modifier that will be applied to the fluid simulations.</summary>
		public FlowModifier Modifier { set { modifier = value; } get { return modifier; } } [SerializeField] private FlowModifier modifier;

		/// <summary>If you need to display some sort of visual while this component is drawing, you can set it here.</summary>
		public GameObject Visual { set { visual = value; } get { return visual; } } [SerializeField] private GameObject visual;

		/// <summary>The scene layers under the finger/mouse that this component will raycast. The spawned prefab will be placed there.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [SerializeField] private LayerMask layers = Physics.DefaultRaycastLayers;

		/// <summary>The maximum distance of the raycast.</summary>
		public float MaxDistance { set { maxDistance = value; } get { return maxDistance; } } [SerializeField] private float maxDistance = 100.0f;

		protected virtual void OnEnable()
		{
			CwInputManager.EnsureThisComponentExists();
		}

		protected virtual void FixedUpdate()
		{
			var showVisual = false;

			if (modifier != null)
			{
				var camera = Camera.main;

				if (camera != null)
				{
					var fingers = CwInputManager.GetFingers(true, false);

					foreach (var finger in fingers)
					{
						if (PaintControls.IsDown(finger) == true)
						{
							var hit = default(RaycastHit);

							if (Physics.Raycast(camera.ScreenPointToRay(finger.ScreenPosition), out hit, float.PositiveInfinity, layers) == true)
							{
								DoSpawn(hit);

								showVisual = true;

								break;
							}
						}
					}
				}
			}

			if (visual != null)
			{
				visual.SetActive(showVisual);
			}
		}

		private void DoSpawn(RaycastHit hit)
		{
			modifier.transform.position = hit.point;
			modifier.transform.rotation = modifier.transform.rotation;

			if (visual != null)
			{
				visual.transform.position = modifier.transform.position;
				visual.transform.rotation = modifier.transform.rotation;
			}

			modifier.ApplyNow(Time.fixedDeltaTime);
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowPaintScreen;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowPaintScreen_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("PaintControls", "The finger/mouse/key that must be pressed for the painting.");

			Separator();

			BeginError(Any(tgts, t => t.Modifier == null));
				Draw("modifier", "The modifier that will be applied to the fluid simulations.");
			EndError();

			Draw("visual", "If you need to display some sort of visual while this component is drawing, you can set it here.");
			Draw("layers", "The scene layers under the finger/mouse that this component will raycast. The spawned prefab will be placed there.");
			Draw("maxDistance", "The maximum distance of the raycast.");
		}
	}
}
#endif