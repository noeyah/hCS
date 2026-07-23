using System.Buffers;
using System.Collections.Concurrent;
using System.Drawing;
using Core.Network;
using MemoryPack;
using Shared;
using Shared.Protocol;

namespace TestClient;

public class ClientSession : PacketSession
{
	public int MyUid { get; private set; }
	public ConcurrentDictionary<int, PointF> Players { get; } = new();

	protected override void OnConnected()
	{
	}

	protected override void OnDisconnected()
	{
	}

	public void SendLogin(string name)
	{
		SendBody(MemoryPackSerializer.Serialize<Packet>(new LoginReq { Name = name }));
	}

	public void SendEnterZone()
	{
		SendBody(MemoryPackSerializer.Serialize<Packet>(new EnterZoneReq()));
	}

	public void SendMove(MoveFlag flags)
	{
		SendBody(MemoryPackSerializer.Serialize<Packet>(new MoveReq { Flags = flags }));
	}

	public override void OnMessage(ReadOnlySequence<byte> payload)
	{
		Packet? packet = MemoryPackSerializer.Deserialize<Packet>(payload);

		switch (packet)
		{
			case LoginRes loginRes:
				if (loginRes.ErrorCode == ErrorCode.OK)
				{
					MyUid = loginRes.id;
				}
				break;

			case EnterZoneRes enterRes:
				Players[MyUid] = new PointF(enterRes.X, enterRes.Y);
				break;

			case EnterZoneNtf enterNtf:
				foreach (EnterInfo info in enterNtf.Enters)
				{
					Players[info.Uid] = new PointF(info.X, info.Y);
				}
				break;

			case LeaveZoneNtf leaveNtf:
				foreach (int uid in leaveNtf.Leaves)
				{
					Players.TryRemove(uid, out _);
				}
				break;

			case MoveNtf moveNtf:
				foreach (MoveInfo m in moveNtf.Moves)
				{
					Players[m.Uid] = new PointF(m.X, m.Y);
				}
				break;
		}
	}
}
