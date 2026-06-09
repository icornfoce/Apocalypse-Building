using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component can be attached to any prefab spawned by the <b>FlowSplash</b> component, and it will emit an amount of particles relative to the splash strength.</summary>
	[RequireComponent(typeof(ParticleSystem))]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSplashSpawnEmit")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Splash Spawn Emit")]
	public class FlowSplashSpawnEmit : MonoBehaviour, ISplashSpawnHandler
	{
		/// <summary>The amount of particles that will be spawned when the splash strength is 0.</summary>
		public int CountMin { set  { countMin = value; } get { return countMin; } } [SerializeField] private int countMin;

		/// <summary>The amount of particles that will be spawned when the splash strength is 1.</summary>
		public int CountMax { set  { countMax = value; } get { return countMax; } } [SerializeField] private int countMax = 10;

		public void HandleSplashSpawn(float strength)
		{
			var count = Mathf.RoundToInt(Mathf.Lerp(countMin, countMax, strength));

			if (count > 0)
			{
				var particleSystem = GetComponent<ParticleSystem>();

				particleSystem.Emit(count);
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSplashSpawnEmit;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSplashSpawnEmit_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("countMin", "The amount of particles that will be spawned when the splash strength is 0.");
			Draw("countMax", "The amount of particles that will be spawned when the splash strength is 1.");
		}
	}
}
#endif