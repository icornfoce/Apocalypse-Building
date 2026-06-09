using UnityEngine;
using CW.Common;

namespace FLOW
{
	[RequireComponent(typeof(Rigidbody))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowShipController")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Ship Controller")]
	public class FlowShipController : MonoBehaviour
	{
		/// <summary>The keys/fingers required to move left/right.</summary>
		public CwInputManager.Axis TurnControls { set { turnControls = value; } get { return turnControls; } } [SerializeField] private CwInputManager.Axis turnControls = new CwInputManager.Axis(2, false, CwInputManager.AxisGesture.HorizontalDrag, 1.0f, KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow, 100.0f);

		/// <summary>The keys/fingers required to move backward/forward.</summary>
		public CwInputManager.Axis MoveControls { set { moveControls = value; } get { return moveControls; } } [SerializeField] private CwInputManager.Axis moveControls = new CwInputManager.Axis(2, false, CwInputManager.AxisGesture.HorizontalDrag, 1.0f, KeyCode.S, KeyCode.W, KeyCode.DownArrow, KeyCode.UpArrow, 100.0f);

		/// <summary>The movement speed will be multiplied by this.</summary>
		public float TurnSpeed { set { turnSpeed = value; } get { return turnSpeed; } } [SerializeField] private float turnSpeed = 1.0f;

		/// <summary>The turn speed will be multiplied by this.</summary>
		public float MoveSpeed { set { moveSpeed = value; } get { return moveSpeed; } } [SerializeField] private float moveSpeed = 1.0f;

		[System.NonSerialized]
		private Rigidbody cachedRigidbody;

		[System.NonSerialized]
		private float turn;

		[System.NonSerialized]
		private float move;

		protected virtual void OnEnable()
		{
			cachedRigidbody = GetComponent<Rigidbody>();
		}

		protected virtual void Update()
		{
			turn = turnControls.GetValue(Time.fixedDeltaTime) * turnSpeed;
			move = moveControls.GetValue(Time.fixedDeltaTime) * moveSpeed;
		}

		protected virtual void FixedUpdate()
		{
			var axis = transform.forward; axis.y = 0.0f;
			var mass = cachedRigidbody.mass;

			cachedRigidbody.AddTorque(0.0f, turn * mass, 0.0f, ForceMode.Force);

			cachedRigidbody.AddForce(axis * move * mass, ForceMode.Force);
		}
	}
}