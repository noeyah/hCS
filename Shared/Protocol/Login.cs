using MemoryPack;

namespace Shared.Protocol;

[MemoryPackable]
public partial class LoginReq : Packet
{
	public string? Name { get; set; }
}

[MemoryPackable]
public partial class LoginRes : Response
{
	public int id { get; set; }
}
