using MemoryPack;

namespace Shared.Protocol;

[MemoryPackable]
public partial class Ping : Packet
{
	public long Tick { get; set; }
}

[MemoryPackable]
public partial class Pong : Packet
{
	public long Tick { get; set; }
}
