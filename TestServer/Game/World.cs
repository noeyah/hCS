namespace TestServer;

public class World
{
	// 지금은 존 하나. 나중에 Dictionary<int, Zone> ...
	private readonly Zone _zone;

	public World(int tickHz = 20)
	{
		_zone = new Zone(tickHz);
	}

	public void Start()
	{
		_zone.Start();
	}

	public void Stop()
	{
		_zone.Stop();
	}

	public Zone PickZone(int uid)
	{
		// 어느 존에 넣을지? 지금은 존하나
		return _zone;
	}
}
