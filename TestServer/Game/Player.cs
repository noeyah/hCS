using Core.Network;
using Shared;

namespace TestServer;

public class Player
{
	public int Uid { get; }
	public string Name { get; }

	public float X { get; set; }
	public float Y { get; set; }

	public MoveFlag Move { get; set; }

	public int CellIndex { get; set; } = -1;

	public ISender Session { get; }

	public Player(int uid, string name, ISender session)
	{
		Uid = uid;
		Name = name;
		Session = session;
	}

}
