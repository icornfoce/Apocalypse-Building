using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component applies continuous torque to the sibling <b>Rigidbody</b> component.</summary>
	[RequireComponent(typeof(Rigidbody))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowTorque")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Torque")]
	public class FlowTorque : MonoBehaviour
	{
		/// <summary>This allows you to specify a positional offset relative to the <b>Target</b>.</summary>
		public Vector3 Torque { set { torque = value; } get { return torque; } } [SerializeField] private Vector3 torque;

		/// <summary>The frame of reference the torque will be applied in.</summary>
		public Space Space { set { space = value; } get { return space; } } [SerializeField] private Space space;

		/// <summary>The force mode the torque will be applied using.</summary>
		public ForceMode ForceMode { set { forceMode = value; } get { return forceMode; } } [SerializeField] private ForceMode forceMode;

		[System.NonSerialized]
		private Rigidbody cachedRigidbody;

		protected virtual void OnEnable()
		{
			cachedRigidbody = GetComponent<Rigidbody>();
		}

		protected virtual void FixedUpdate()
		{
			if (space == Space.Self)
			{
				cachedRigidbody.AddRelativeTorque(torque * Time.fixedDeltaTime, forceMode);
			}
			else
			{
				cachedRigidbody.AddTorque(torque * Time.fixedDeltaTime, forceMode);
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowTorque;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowTorque_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("torque", "This allows you to specify a positional offset relative to the <b>Target</b>.");
			Draw("space", "The frame of reference the torque will be applied in.");
			Draw("forceMode", "The force mode the torque will be applied using.");
		}
	}
}
#endif