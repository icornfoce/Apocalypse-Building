using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component will modify the sibling <b>FlowModifier.Strength</b> setting based on the speed this GameObject moves.</summary>
	[RequireComponent(typeof(FlowModifier))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowModifierSpeed")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Modifier Speed")]
	public class FlowModifierSpeed : MonoBehaviour
	{
		public enum RotationType
		{
			None,
			WorldDelta,
			LocalDelta
		}

		/// <summary>When this GameObject's speed matches this value, the <b>FlowModifier.Strength</b> will be set to the specified <b>Strength</b> value.</summary>
		public float SpeedMax { set { speedMax = value; } get { return speedMax; } } [SerializeField] private float speedMax = 10.0f;

		/// <summary>The <b>FlowModifier.Strength</b> will be set to this value when this GameObject's speed matches the <b>SpeedMax</b> value.</summary>
		public float Strength { set { strength = value; } get { return strength; } } [SerializeField] private float strength = 1.0f;

		/// <summary>If the speed exceeds <b>SpeedMax</b>, should the calculations be clamped, as if the speed wasn't exceeded?</summary>
		public bool Clamp { set { clamp = value; } get { return clamp; } } [SerializeField] private bool clamp = true;

		/// <summary>If the sibling <b>FlowModifier</b> uses <b>Mode = AddForceUniform</b>, should its <b>Angle</b> setting automatically be updated?
		/// WorldDelta = The angle will be set to the world space movement of this object (should be used if this GameObject doesn't rotate).
		/// LocalDelta = The angle will be set to the local space movement of this object (should be used if this GameObject does rotate).</summary>
		public RotationType Rotation { set { rotation = value; } get { return rotation; } } [SerializeField] private RotationType rotation;

		public float Speed
		{
			get
			{
				return lastSpeed;
			}
		}

		[System.NonSerialized]
		private FlowModifier cachedModifier;

		[System.NonSerialized]
		private Vector3 lastPosition;

		[System.NonSerialized]
		private float lastSpeed;

		protected virtual void OnEnable()
		{
			cachedModifier = GetComponent<FlowModifier>();
			lastPosition   = transform.position;
		}

		protected virtual void FixedUpdate()
		{
			var newPosition = transform.position;
			var delta       = lastPosition - newPosition;
			var speed       = Vector3.Magnitude(delta) / Time.fixedDeltaTime;
			var speed01     = speedMax != 0.0f ? speed / speedMax : 0.0f;

			if (clamp == true)
			{
				speed01 = Mathf.Clamp01(speed01);
			}

			if (rotation != RotationType.None && cachedModifier.Mode == FlowModifier.ModeType.AddForceUniform)
			{
				var radians = Mathf.PI + Mathf.Atan2(delta.x, delta.z);

				if (rotation == RotationType.LocalDelta)
				{
					var forward = transform.forward;

					radians -= Mathf.Atan2(forward.x, forward.z);
				}

				cachedModifier.Angle = radians * Mathf.Rad2Deg;
			}

			cachedModifier.Strength = strength * speed01;

			lastPosition = newPosition;
			lastSpeed    = speed;
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowModifierSpeed;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowModifierSpeed_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("speedMax", "When this GameObject's speed matches this value, the <b>FlowModifier.Strength</b> will be set to the specified <b>Strength</b> value.");
			Draw("strength", "The <b>FlowModifier.Strength</b> will be set to this value when this GameObject's speed matches the <b>SpeedMax</b> value.");
			Draw("clamp", "If the speed exceeds <b>SpeedMax</b>, should the calculations be clamped, as if the speed wasn't exceeded?");
			Draw("rotation", "If the sibling <b>FlowModifier</b> uses <b>Mode = AddForceUniform</b>, should its <b>Angle</b> setting automatically be updated?\n\nWorldDelta = The angle will be set to the world space movement of this object (should be used if this GameObject doesn't rotate).\n\nLocalDelta = The angle will be set to the local space movement of this object (should be used if this GameObject does rotate).");

			Separator();

			BeginDisabled();
				EditorGUILayout.FloatField("Speed", tgt.Speed);
			EndDisabled();
		}
	}
}
#endif