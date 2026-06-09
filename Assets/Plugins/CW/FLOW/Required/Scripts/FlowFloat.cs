using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you to make a <b>Rigidbody</b> float in a fluid.</summary>
	[RequireComponent(typeof(Rigidbody))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowFloat")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Float")]
	public class FlowFloat : MonoBehaviour
	{
		[System.Serializable] public class FloatEvent : UnityEvent<float> {}

		public enum TorqueType
		{
			WorldUp,
			WorldUpBidirectional,
			NormalUp,
			NormalUpBidirectional
		}

		/// <summary>This object's buoyancy will be calculated using these height samples.</summary>
		public List<FlowSample> Samples { get { if (samples == null) samples = new List<FlowSample>(); return samples; } } [SerializeField] private List<FlowSample> samples;

		/// <summary>The buoyancy of this object.
		/// 0 = Sinks.
		/// 1 = Neutral.
		/// 2 = Floats.</summary>
		public float Buoyancy { set { buoyancy = value; } get { return buoyancy; } } [SerializeField] private float buoyancy = 1.5f;

		/// <summary>The positional drag of this object while underwater.</summary>
		public float Drag { set { drag = value; } get { return drag; } } [SerializeField] private float drag;

		/// <summary>This allows you to control how much the fluid velocity is involved in the drag calculation. For most objects this should be set to 1, but if your object applies force to the fluid then you can end up with scenarios where the object causes itself to accelerate, so reducing this can counteract that.</summary>
		public float FluidDrag { set { fluidDrag = value; } get { return fluidDrag; } } [SerializeField] [Range(0.0f, 1.0f)] private float fluidDrag = 1.0f;

		/// <summary>The rotation drag of this object while underwater.
		/// NOTE: If you have multiple sample points then this setting may be unnecessary, because the sample points will work together to slow the object's rotation.</summary>
		public float AngularDrag { set { angularDrag = value; } get { return angularDrag; } } [SerializeField] private float angularDrag;

		/// <summary>This setting tries to turn the object upwards when underwater. This is useful for boats, buoys, and other objects that should remain upright. Similar behavior can be achieved with many sample points, but that can be difficult to set up and have negative performance impact.</summary>
		public float Torque { set { torque = value; } get { return torque; } } [SerializeField] private float torque;

		/// <summary>This allows you to control how the <b>Torque</b> setting will apply.
		/// WorldUp = Rotate to face up.
		/// WorldUpBidirectional = Rotate to face up or down.
		/// NormalUp = Rotate up to fluid surface.
		/// NormalUpBidirectional = Rotate up or down to fluid surface.</summary>
		public TorqueType TorqueMode { set { torqueMode = value; } get { return torqueMode; } } [SerializeField] private TorqueType torqueMode;

		[System.NonSerialized]
		private Rigidbody cachedRigidbody;

		/// <summary>This will automatically reset the <b>Samples</b> list based on any child GameObjects that contain a <b>FlowSample</b>.</summary>
		public void ResetSamples()
		{
			Samples.Clear();

			samples.AddRange(GetComponentsInChildren<FlowSample>());
		}

		protected virtual void OnEnable()
		{
			cachedRigidbody = GetComponent<Rigidbody>();
		}

		protected virtual void FixedUpdate()
		{
			var totalSamples  = 0;
			var totalStrength = 0.0f;

			if (samples != null)
			{
				foreach (var sample in samples)
				{
					if (sample != null && sample.Sampled == true)
					{
						totalSamples  += 1;
						totalStrength += Mathf.Max(0.0f, sample.Strength);
					}
				}
			}

			if (totalSamples > 0)
			{
				var gravity    = Physics.gravity;
				var submersion = 0.0f;
				var mass       = cachedRigidbody.mass;

				foreach (var sample in samples)
				{
					if (sample != null && sample.Sampled == true && sample.Strength > 0.0f)
					{
						var force          = Vector3.zero;
						var samplePosition = sample.transform.position;
						var weight         = sample.Strength / totalStrength;

						submersion += sample.Submersion * sample.Overlap * weight;

						if (buoyancy != 0.0f)
						{
							force += -gravity * buoyancy;
						}

						if (drag != 0.0f)
						{
							force -= (cachedRigidbody.GetPointVelocity(samplePosition) - sample.FluidVelocity * fluidDrag) * drag;
						}

						force *= sample.Submersion;
						force *= sample.Overlap;
						force *= weight;

						cachedRigidbody.AddForceAtPosition(force * mass, samplePosition, ForceMode.Force);
					}
				}

				submersion /= totalSamples;

				if (angularDrag != 0.0f)
				{
					cachedRigidbody.angularVelocity *= Mathf.Exp(-angularDrag * angularDrag * submersion * Time.fixedDeltaTime);
				}

				if (torque != 0.0f)
				{
					var axis = Vector3.up;

					if (torqueMode == TorqueType.NormalUp || torqueMode == TorqueType.NormalUpBidirectional)
					{
						axis = Vector3.zero;

						foreach (var sample in samples)
						{
							if (sample != null && sample.Sampled == true && sample.Strength > 0.0f)
							{
								var weight = sample.Strength / totalStrength;

								axis += sample.FluidNormal * weight;
							}
						}
					}

					if (torqueMode == TorqueType.WorldUpBidirectional || torqueMode == TorqueType.NormalUpBidirectional)
					{
						if (Vector3.Dot(transform.up, axis) < 0.0f)
						{
							axis = -axis;
						}
					}

					var rotation = Quaternion.FromToRotation(transform.up, axis);

					cachedRigidbody.AddTorque(new Vector3(rotation.x, rotation.y, rotation.z) * submersion * torque * mass, ForceMode.Force);
				}
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			if (samples != null)
			{
				foreach (var sample in samples)
				{
					if (sample != null)
					{
						Gizmos.DrawLine(transform.position, sample.transform.position);
					}
				}
			}
		}
#endif
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowFloat;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowFloat_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("buoyancy", "The buoyancy of this object.\n\n0 = Sinks.\n\n1 = Neutral.\n\n2 = Floats.");
			Draw("drag", "The positional drag of this object while underwater.");
			Draw("fluidDrag", "This allows you to control how much the fluid velocity is involved in the drag calculation. For most objects this should be set to 1, but if your object applies force to the fluid then you can end up with scenarios where the object causes itself to accelerate, so reducing this can counteract that.");
			Draw("angularDrag", "The rotation drag of this object while underwater.\n\nNOTE: If you have multiple sample points then this setting may be unnecessary, because the sample points will work together to slow the object's rotation.");
			Draw("torque", "This setting tries to turn the object upwards when underwater. This is useful for boats, buoys, and other objects that should remain upright. Similar behavior can be achieved with many sample points, but that can be difficult to set up and have negative performance impact.");
			Draw("torqueMode", "This allows you to control how the <b>Torque</b> setting will apply.\n\nWorldUp = Rotate to face up.\n\nWorldUpBidirectional = Rotate to face up or down.\n\nNormalUp = Rotate up to fluid surface.\n\nNormalUpBidirectional = Rotate up or down to fluid surface.");

			Separator();

			BeginError(Any(tgts, SamplesMissing));
				Draw("samples", "This object's buoyancy will be calculated using these height samples.");
			EndError();

			if (Any(tgts, SamplesMissing))
			{
				if (HelpButton("This component has no Samples set, so it cannot float.", MessageType.Info, "Add", 40) == true)
				{
					Each(tgts, t => { var child = FlowSample.Create(t.gameObject.layer, t.transform); CwHelper.SelectAndPing(child); }, true);
				}
			}

			if (Any(tgts, SamplesMismatch))
			{
				if (HelpButton("This component's Samples list differs from the FlowSample components in the child GameObjects.", MessageType.Info, "Reset", 60) == true)
				{
					Each(tgts, t => t.ResetSamples(), true);
				}
			}
		}

		private static List<FlowSample> tempSamples = new List<FlowSample>();

		private bool SamplesMissing(FlowFloat tgt)
		{
			foreach (var sample in tgt.Samples)
			{
				if (sample != null)
				{
					return false;
				}
			}

			return true;
		}

		private bool SamplesMismatch(FlowFloat tgt)
		{
			tempSamples.Clear();
			tempSamples.AddRange(tgt.GetComponentsInChildren<FlowSample>());

			if (tgt.Samples.Count != tempSamples.Count)
			{
				return true;
			}

			foreach (var sample in tgt.Samples)
			{
				tempSamples.Remove(sample);
			}

			return tempSamples.Count > 0;
		}
	}
}
#endif