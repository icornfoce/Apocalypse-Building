using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace FLOW
{
	/// <summary>This component marks the current GameObject as an object that can block.</summary>
	[DefaultExecutionOrder(-200)]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowObject")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Object")]
	public class FlowObject : MonoBehaviour
	{
		[System.Serializable] public class FloatEvent : UnityEvent<float> {}

		/// <summary>The center of the object in local space.</summary>
		public Vector3 Center { set { if (center != value) { center = value; MarkAsDirty(); } } get { return center; } } [SerializeField] private Vector3 center;

		/// <summary>The radius of the object in local space.</summary>
		public float Radius { set { if (radius != value) { radius = value; MarkAsDirty(); } } get { return radius; } } [SerializeField] private float radius = 1.0f;

		[System.NonSerialized]
		private bool dirty;

		[SerializeField]
		private float expectedRadius;

		[SerializeField]
		private Matrix4x4 expectedMatrix;

		[SerializeField]
		private Vector3 expectedPosition;

		/// <summary>If this object has been modified in a way that isn't normally detected (e.g. MeshCollider.sharedMesh changed), then you can manually call this method.</summary>
		[ContextMenu("Mark As Dirty")]
		public void MarkAsDirty()
		{
			dirty = true;
		}

		protected virtual void OnEnable()
		{
			dirty            = false;
			expectedRadius   = CwHelper.UniformScale(transform.lossyScale) * radius;
			expectedMatrix   = transform.localToWorldMatrix;
			expectedPosition = transform.TransformPoint(center);

			FlowSimulation.DirtyGroundAll(expectedPosition, expectedRadius);
		}

		protected virtual void OnDisable()
		{
			FlowSimulation.DirtyGroundAll(expectedPosition, expectedRadius);
		}

		protected virtual void Update()
		{
			var newRadius   = CwHelper.UniformScale(transform.lossyScale) * radius;
			var newMatrix   = transform.localToWorldMatrix;
			var newPosition = transform.TransformPoint(center);

			if (dirty == true || expectedRadius != newRadius || expectedMatrix != newMatrix || expectedPosition != newPosition)
			{
				FlowSimulation.DirtyGroundAll(expectedPosition, expectedRadius);

				expectedRadius   = newRadius;
				expectedMatrix   = newMatrix;
				expectedPosition = newPosition;

				FlowSimulation.DirtyGroundAll(expectedPosition, expectedRadius);

				dirty = false;
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(center), Quaternion.identity, new Vector3(1.0f, 0.0f, 1.0f));

			Gizmos.DrawWireSphere(Vector3.zero, CwHelper.UniformScale(transform.lossyScale) * radius);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowObject;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowObject_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			var maskAsDirty = false;

			Draw("center", ref maskAsDirty, "The center of the object in local space.");
			Draw("radius", ref maskAsDirty, "The radius of the object in local space.");

			if (maskAsDirty == true)
			{
				Each(tgts, t => t.MarkAsDirty(), true);
			}
		}
	}
}
#endif