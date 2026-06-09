using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace FLOW
{
	/// <summary>This component stores a volume of fluid that can then be filled and emptied via the <b>FlowModifierContainer</b> component.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowContainer")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Container")]
	public class FlowContainer : MonoBehaviour
	{
		[System.Serializable] public class FloatEvent : UnityEvent<float> {}

		/// <summary>The volume of fluid stored in this container.</summary>
		public float Volume { set {  volume = value; UpdateClamp(); } get { return volume; } } [SerializeField] private float volume;

		/// <summary>The maximum volume of fluid that can be stored in this container.</summary>
		public float Capacity { set { capacity = value; } get { return capacity; } } [SerializeField] private float capacity = 100.0f;

		/// <summary>Should the <b>Volume</b> be restricted to the <b>0..Capacity</b> range?
		/// The precise volume of fluid a modifier adds or removes isn't known ahead of time, so it can sometimes be beneficial to allow the <b>Volume</b> value to go slightly below or above the standard range.</summary>
		public bool Clamp { set { clamp = value; UpdateClamp(); } get { return clamp; } } [SerializeField] private bool clamp;

		/// <summary>This gets invoked every time the <b>Volume</b> value is changed.
		/// Float = Current Volume.</summary>
		public FloatEvent OnVolume { get { if (onVolume == null) onVolume = new FloatEvent(); return onVolume; } } [SerializeField] private FloatEvent onVolume;

		/// <summary>This gets invoked every time the <b>Volume</b> value is changed.
		/// Float = Current Volume / Capacity clamped to 0..1.</summary>
		public FloatEvent OnVolume01 { get { if (onVolume01 == null) onVolume01 = new FloatEvent(); return onVolume01; } } [SerializeField] private FloatEvent onVolume01;

		public float Volume01
		{
			get
			{
				return Mathf.Clamp01(CwHelper.Divide(volume, capacity));
			}
		}

		public void SetVolume(float newVolume)
		{
			if (clamp == true)
			{
				newVolume = Mathf.Clamp(newVolume, 0, capacity);
			}

			if (volume != newVolume)
			{
				volume = newVolume;

				if (onVolume != null)
				{
					onVolume.Invoke(volume);
				}

				if (onVolume01 != null)
				{
					onVolume01.Invoke(Volume01);
				}
			}
		}

		/// <summary>This method will add fluid to the container.</summary>
		public void AddVolume(FlowSimulation simulation, float delta)
		{
			SetVolume(volume + delta);
		}

		/// <summary>This method will remove fluid from the container.</summary>
		public void RemoveVolume(FlowSimulation simulation, float delta)
		{
			SetVolume(volume - delta);
		}

		/// <summary>This method will add fluid to the container.</summary>
		public void AddVolume(float delta)
		{
			SetVolume(volume + delta);
		}

		/// <summary>This method will add fluid to the container.</summary>
		public void RemoveVolume(float delta)
		{
			SetVolume(volume - delta);
		}

		public void UpdateClamp()
		{
			if (clamp == true)
			{
				volume = Mathf.Clamp(volume, 0, capacity);
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowContainer;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowContainer_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("volume", "The submersion value will be read from this sample point.");
			Draw("capacity", "The maximum volume of fluid that can be stored in this container.");
			Draw("clamp", "Should the <b>Volume</b> be restricted to the <b>0..Capacity</b> range?\n\nThe precise volume of fluid a modifier adds or removes isn't known ahead of time, so it can sometimes be beneficial to allow the <b>Volume</b> value to go slightly below or above the standard range.");

			Separator();

			Draw("onVolume");
			Draw("onVolume01");
		}
	}
}
#endif