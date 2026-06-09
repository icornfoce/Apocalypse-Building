using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component will scale the specified </summary>
	[ExecuteInEditMode]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowContainerTransform")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Container Scale")]
	public class FlowContainerTransform : MonoBehaviour
	{
		/// <summary>The container this component will read.</summary>
		public FlowContainer Container { set  { container = value; } get { return container; } } [SerializeField] private FlowContainer container;

		/// <summary>The Transform this component will modify.</summary>
		public Transform Target { set  { target = value; } get { return target; } } [SerializeField] private Transform target;

		/// <summary>Should the <b>Target</b>'s localPosition be modified?</summary>
		public bool LocalPosition { set  { localPosition = value; } get { return localPosition; } } [SerializeField] private bool localPosition;

		public Vector3 LocalPositionEmpty { set  { localPositionEmpty = value; } get { return localPositionEmpty; } } [SerializeField] private Vector3 localPositionEmpty;

		public Vector3 LocalPositionFull { set  { localPositionFull = value; } get { return localPositionFull; } } [SerializeField] private Vector3 localPositionFull;

		/// <summary>Should the <b>Target</b>'s localRotation be modified using Euler values?</summary>
		public bool LocalRotation { set  { localRotation = value; } get { return localRotation; } } [SerializeField] private bool localRotation;

		public Vector3 LocalRotationEmpty { set  { localRotationEmpty = value; } get { return localRotationEmpty; } } [SerializeField] private Vector3 localRotationEmpty;

		public Vector3 LocalRotationFull { set  { localRotationFull = value; } get { return localRotationFull; } } [SerializeField] private Vector3 localRotationFull;

		/// <summary>Should the <b>Target</b>'s localPosition be modified?</summary>
		public bool LocalScale { set  { localScale = value; } get { return localScale; } } [SerializeField] private bool localScale;

		public Vector3 LocalScaleEmpty { set  { localScaleEmpty = value; } get { return localScaleEmpty; } } [SerializeField] private Vector3 localScaleEmpty = Vector3.one;

		public Vector3 LocalScaleFull { set  { localScaleFull = value; } get { return localScaleFull; } } [SerializeField] private Vector3 localScaleFull = Vector3.one;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			container = GetComponentInParent<FlowContainer>();
		}
#endif

		protected virtual void Update()
		{
			if (container != null && target != null)
			{
				var volume01 = container.Volume01;

				if (localPosition == true)
				{
					target.localPosition = Vector3.Lerp(localPositionEmpty, localPositionFull, volume01);
				}

				if (localRotation == true)
				{
					target.localEulerAngles = Vector3.Lerp(localRotationEmpty, localRotationFull, volume01);
				}

				if (localScale == true)
				{
					target.localScale = Vector3.Lerp(localScaleEmpty, localScaleFull, volume01);
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowContainerTransform;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowContainerTransform_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Container == null));
				Draw("container", "The container this component will read.");
			EndError();
			BeginError(Any(tgts, t => t.Target == null));
				Draw("target", "The Transform this component will modify.");
			EndError();
		}
	}
}
#endif