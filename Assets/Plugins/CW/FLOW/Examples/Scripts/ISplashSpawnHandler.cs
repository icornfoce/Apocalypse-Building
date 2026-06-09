/// <summary>This interface can be implemented by components attached to the prefabs spawned by the <b>FlowSplash</b> component, allowing them to change their behavior based on the splash force.</summary>
public interface ISplashSpawnHandler
{
	void HandleSplashSpawn(float strength);
}