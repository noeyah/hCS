using MemoryPack;

namespace Shared.Protocol;

[MemoryPackable]
public partial class EnterZoneReq : Packet
{
}

[MemoryPackable]
public partial class EnterZoneRes : Response
{
	public float X { get; set; }
	public float Y { get; set; }
}

[MemoryPackable]
public partial class EnterZoneNtf : Packet
{
	public EnterInfo[] Enters { get; set; } = [];
}

[MemoryPackable]
public partial class LeaveZoneNtf : Packet
{
	public int[] Leaves { get; set; } = [];
}

[MemoryPackable]
public partial class MoveReq : Packet
{
	public MoveFlag Flags { get; set; }
}

[MemoryPackable]
public partial class MoveNtf : Packet
{
	public MoveInfo[] Moves { get; set; } = [];
}
