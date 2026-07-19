using MemoryPack;

namespace Shared.Protocol;

public enum PacketID : ushort
{
	None = 0,
	Ping,
	Pong,

	LoginReq = 10,
	LoginRes,

	EnterZoneReq = 20,
	EnterZoneRes,
	EnterZoneNtf,
	LeaveZoneNtf,

	MoveReq = 30,
	MoveNtf,
}

[MemoryPackable]
[MemoryPackUnion((ushort)PacketID.Ping, typeof(Ping))]
[MemoryPackUnion((ushort)PacketID.Pong, typeof(Pong))]
[MemoryPackUnion((ushort)PacketID.LoginReq, typeof(LoginReq))]
[MemoryPackUnion((ushort)PacketID.LoginRes, typeof(LoginRes))]
[MemoryPackUnion((ushort)PacketID.EnterZoneReq, typeof(EnterZoneReq))]
[MemoryPackUnion((ushort)PacketID.EnterZoneRes, typeof(EnterZoneRes))]
[MemoryPackUnion((ushort)PacketID.EnterZoneNtf, typeof(EnterZoneNtf))]
[MemoryPackUnion((ushort)PacketID.LeaveZoneNtf, typeof(LeaveZoneNtf))]
[MemoryPackUnion((ushort)PacketID.MoveReq, typeof(MoveReq))]
[MemoryPackUnion((ushort)PacketID.MoveNtf, typeof(MoveNtf))]
public abstract partial class Packet
{
}


[MemoryPackable]
public partial class Response : Packet
{
	public ErrorCode ErrorCode { get; set; }
}