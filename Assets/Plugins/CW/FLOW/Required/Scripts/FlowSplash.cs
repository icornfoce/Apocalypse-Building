using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you to invoke an event when this component gets enabled.</summary>
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSplash")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Splash")]
	public class FlowSplash : MonoBehaviour
	{
		[System.Serializable] public class FloatEvent : UnityEvent<float> {}

		/// <summary>The prefabs spawned by this component.
		/// NOTE: These prefabs can have components that implement the <b>ISplashSpawnHandler</b> interface.</summary>
		public List<GameObject> Prefabs { get { if (prefabs == null) prefabs = new List<GameObject>(); return prefabs; } } [SerializeField] private List<GameObject> prefabs;

		/// <summary>This event will be invoked when this splash is applied somewhere.
		/// Float = Strength</summary>
		public FloatEvent OnSplash { get { if (onSplash == null) onSplash = new FloatEvent(); return onSplash; } } [SerializeField] private FloatEvent onSplash;

		[System.NonSerialized]
		private List<FlowModifier> cachedModifiers = new List<FlowModifier>();

		private static List<ISplashSpawnHandler> tempSplashSpawnHandlers = new List<ISplashSpawnHandler>();

		/// <summary>This component caches the <b>FlowModifier</b> components that it will apply to. If you add/remove modifiers then this method allows you to clear this cache.</summary>
		[ContextMenu("Clear Cache")]
		public void ClearCache()
		{
			cachedModifiers.Clear();
		}

		public void Apply(Vector3 position, float strength, float scale)
		{
			if (cachedModifiers.Count == 0)
			{
				GetComponentsInChildren(true, cachedModifiers);
			}

			var oldPosition = transform.position;
			var oldRotation = transform.rotation;
			var oldScale    = transform.localScale;

			transform.position   = position;
			transform.rotation   = Quaternion.identity;
			transform.localScale = oldScale * scale;

			foreach (var modifier in cachedModifiers)
			{
				modifier.ApplyNow(strength);
			}

			if (prefabs != null)
			{
				foreach (var prefab in prefabs)
				{
					if (prefab != null)
					{
						var clone = Instantiate(prefab, transform.position, transform.rotation);

						clone.GetComponentsInChildren(tempSplashSpawnHandlers);

						foreach (var handler in tempSplashSpawnHandlers)
						{
							handler.HandleSplashSpawn(strength);
						}
					}
				}
			}

			if (onSplash != null)
			{
				onSplash.Invoke(strength);
			}

			transform.position   = oldPosition;
			transform.rotation   = oldRotation;
			transform.localScale = oldScale;
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSplash;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSplash_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("prefabs", "The prefabs spawned by this component.\n\nNOTE: These prefabs can have components that implement the <b>ISplashSpawnHandler</b> interface.");

			Separator();

			Draw("onSplash");
		}
	}
}
#endif