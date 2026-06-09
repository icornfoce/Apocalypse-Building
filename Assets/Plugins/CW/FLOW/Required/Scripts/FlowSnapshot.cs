using UnityEngine;
using CW.Common;

namespace FLOW
{
	/// <summary>This component allows you to create a snapshot from a simulation, and later reset the simulation back to that state.</summary>
	[ExecuteInEditMode]
	[HelpURL(FlowCommon.HelpUrlPrefix + "FlowSnapshot")]
	[AddComponentMenu(FlowCommon.ComponentMenuPrefix + "Snapshot")]
	public class FlowSnapshot : MonoBehaviour
	{
		/// <summary>The simulation that will be snapshotted.</summary>
		public FlowSimulation Simulation { set { simulation = value; } get { return simulation; } } [SerializeField] private FlowSimulation simulation;

		/// <summary>This allows you to set the filename used when saving and loading from file, where {0} is the simulation GameObject name.</summary>
		public string Title { set { title = value; } get { return title; } } [SerializeField] private string title = "Flow{0}.json";

		[System.NonSerialized]
		private FlowSnapshotData snapshotData = new FlowSnapshotData();

		private string FinalPath
		{
			get
			{
				return System.IO.Path.Combine(Application.persistentDataPath, string.Format(title, simulation.name));
			}
		}

		/// <summary>This method stores a temporary snapshot of the fluid data to this component. This will be erased when you close the app, change the scene, or otherwise cause this component to be destroyed.</summary>
		[ContextMenu("Save To Temp")]
		public void SaveToTemp()
		{
			if (simulation != null)
			{
				snapshotData.SimulationToRaw(simulation);
			}
		}

		/// <summary>This method reverts the fluid data to the previously stored snapshot.</summary>
		[ContextMenu("Load From Temp")]
		public void LoadFromTemp()
		{
			if (simulation != null)
			{
				snapshotData.RawToSimulation(simulation);
			}
		}

		/// <summary>This method stores a persistent snapshot of the fluid data to this device.
		/// NOTE: Depending on your fluid simulation resolution this can be a lot of data, so it may not always be possible to save.
		/// NOTE: Depending on your fluid simulation resolution this can be slow to call, because it must copy data from GPU to CPU memory.</summary>
		[ContextMenu("Save To File")]
		public void SaveToFile()
		{
			if (simulation != null)
			{
				snapshotData.SimulationToRaw(simulation);
				snapshotData.ConvertRawToReadable();
				snapshotData.ConvertReadableToPng();

				var json = JsonUtility.ToJson(snapshotData);

				System.IO.File.WriteAllText(FinalPath, json);
			}
		}

		/// <summary>This method reverts the fluid data to the previously stored snapshot.</summary>
		[ContextMenu("Load From File")]
		public void LoadFromFile()
		{
			if (simulation != null)
			{
				var json = System.IO.File.ReadAllText(FinalPath);

				if (string.IsNullOrEmpty(json) == false)
				{
					JsonUtility.FromJsonOverwrite(json, snapshotData);

					snapshotData.ConvertCompressedToReadable();
					snapshotData.ConvertReadableToRaw();
					snapshotData.RawToSimulation(simulation);
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace FLOW
{
	using UnityEditor;
	using TARGET = FlowSnapshot;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class FlowSnapshot_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Simulation == null));
				Draw("simulation", "The simulation that will be snapshotted.");
			EndError();
			Draw("title", "This allows you to set the filename used when saving and loading from file, where {0} is the simulation GameObject name.");

			Separator();

			if (Button("Save To Temp") == true)
			{
				Each(tgts, t => t.SaveToTemp());
			}

			if (Button("Load From Temp") == true)
			{
				Each(tgts, t => t.LoadFromTemp());
			}

			Separator();

			if (Button("Write To File") == true)
			{
				Each(tgts, t => t.SaveToFile());
			}

			if (Button("Read From File") == true)
			{
				Each(tgts, t => t.LoadFromFile());
			}
		}
	}
}
#endif