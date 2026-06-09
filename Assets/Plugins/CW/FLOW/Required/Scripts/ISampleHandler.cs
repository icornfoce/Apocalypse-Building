using UnityEngine;
using System.Collections.Generic;

namespace FLOW
{
	public interface ISampleHandler
	{
		void HandleSamples(FlowSimulation simulation, List<Color> samples);
	}
}