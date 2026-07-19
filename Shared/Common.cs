using MemoryPack;

namespace Shared;

[MemoryPackable]
public partial struct PlayerInfo
{
	public int Uid { get; set; }
	public string? Name { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
}

[MemoryPackable]
public partial struct MoveInfo
{
	public int Uid { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
}
