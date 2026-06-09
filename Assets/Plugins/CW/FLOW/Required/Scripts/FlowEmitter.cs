using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you to emit fluid particles into the scene.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowEmitter")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Emitter")]
	public class FlowEmitter : MonoBehaviour
	{
		/// <summary>This allows you to choose what type of fluid this component emits particles of.</summary>
		public FlowFluid Fluid { set { fluid = value; } get { return fluid; } } [SerializeField] private FlowFluid fluid;

		/// <summary>This allows you to choose which fluid simulation the emitted particles will be added to.
		/// None/null = The closest fluid will be used.</summary>
		public FlowSimulation Simulation { set { simulation = value; } get { return simulation; } } [SerializeField] private FlowSimulation simulation;

		/// <summary>The minimum amount of fluid added to the simulation.</summary>
		public float VolumeMin { set { volumeMin = value; } get { return volumeMin; } } [SerializeField] private float volumeMin = 1.0f;

		/// <summary>The maximum amount of fluid added to the simulation.</summary>
		public float VolumeMax { set { volumeMax = value; } get { return volumeMax; } } [SerializeField] private float volumeMax = 1.0f;

		/// <summary>The minimum speed of the emitted particles in world space.</summary>
		public float SpeedMin { set { speedMin = value; } get { return speedMin; } } [SerializeField] private float speedMin = 1.0f;

		/// <summary>The maximum speed of the emitted particles in world space.</summary>
		public float SpeedMax { set { speedMax = value; } get { return speedMax; } } [SerializeField] private float speedMax = 1.5f;

		/// <summary>The minimum lifespan of the emitted particles in seconds.</summary>
		public float LifeMin { set { lifeMin = value; } get { return lifeMin; } } [SerializeField] private float lifeMin = 4.0f;

		/// <summary>The maximum lifespan of the emitted particles in seconds.</summary>
		public float LifeMax { set { lifeMax = value; } get { return lifeMax; } } [SerializeField] private float lifeMax = 5.0f;

		/// <summary>The particles will fire out in a spread from the forward direction by up to this many degrees.</summary>
		public float Spread { set { spread = value; } get { return spread; } } [SerializeField] private float spread = 10.0f;

		/// <summary>The particles will emit in this direction in local space.</summary>
		public Vector3 Direction { set { direction = value; } get { return direction; } } [SerializeField] private Vector3 direction = Vector3.forward;

		/// <summary>The time between each particle emission in seconds.</summary>
		public float Interval { set { interval = value; } get { return interval; } } [SerializeField] private float interval = 0.1f;

		[SerializeField]
		private float age;

		[ContextMenu("Emit Now")]
		public void EmitNow()
		{
			var bestSimulation = FlowSimulation.FindSimulation(transform.position, simulation);

			if (bestSimulation != null)
			{
				var spreadX = Random.Range(-spread, +spread);
				var spreadY = Random.Range(-spread, +spread);
				var wdir    = transform.TransformDirection(Quaternion.Euler(spreadX, spreadY, 0.0f) * direction.normalized);
				var volume  = Random.Range(volumeMin, volumeMax) * bestSimulation.Resolution;
				var speed   = Random.Range(speedMin, speedMax);
				var life    = Random.Range(lifeMin, lifeMax);

				bestSimulation.AddParticle(fluid, volume, transform.position, wdir * speed, life);
			}
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowEmitter</b> component attached.</summary>
		public static FlowEmitter Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>This allows you create a new GameObject with the <b>FlowEmitter</b> component attached.</summary>
		public static FlowEmitter Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			return CwHelper.CreateGameObject("Emitter", layer, parent, localPosition, localRotation, localScale).AddComponent<FlowEmitter>();
		}

		protected virtual void Update()
		{
			age += Time.deltaTime;

			if (age > interval)
			{
				age %= interval;

				EmitNow();
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmos()
		{
			var worldDirection = transform.TransformDirection(direction);

			Gizmos.DrawLine(transform.position, transform.position + worldDirection * speedMin);

			Gizmos.color = Color.red;

			Gizmos.DrawLine(transform.position + worldDirection * speedMin, transform.position + worldDirection * speedMax);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowEmitter;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowEmitter_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Fluid == null));
				Draw("fluid", "This allows you to choose what type of fluid this component emits particles of.");
			EndError();
			Draw("simulation", "This allows you to choose which fluid simulation the emitted particles will be added to.\n\nNone/null = The closest fluid will be used.");
			Draw("volumeMin", "The minimum amount of fluid added to the simulation.");
			Draw("volumeMax", "The maximum amount of fluid added to the simulation.");

			Separator();

			Draw("speedMin", "The minimum speed of the emitted particles in world space.");
			Draw("speedMax", "The maximum speed of the emitted particles in world space.");
			Draw("lifeMin", "The minimum lifespan of the emitted particles in seconds.");
			Draw("lifeMax", "The maximum lifespan of the emitted particles in seconds.");
			Draw("spread", "The particles will fire out in a spread from the forward direction by up to this many degrees.");
			BeginError(Any(tgts, t => t.Direction == Vector3.zero));
				Draw("direction", "The particles will emit in this direction in local space.");
			EndError();
			Draw("interval", "The time between each particle emission in seconds.");
		}

		[MenuItem(FlowCommon.GameObjectMenuPrefix + "Emitter", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = CwHelper.GetSelectedParent();
			var instance = FlowEmitter.Create(parent != null ? parent.gameObject.layer : 0, parent);

			CwHelper.SelectAndPing(instance);
		}
	}
}
#endif