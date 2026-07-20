using System.Buffers;
using Core.Network;
using MemoryPack;
using Shared.Protocol;

namespace TestServer;

public class GameSession : PacketSession
{
	public World World { get; }
	public Zone? Zone {  get; private set; }

	public int Uid { get; set; }
	public string Name { get; set; } = string.Empty;

	public GameSession(World world)
	{
		World = world;
	}

	protected override void OnConnected()
	{
	}

	public override void OnMessage(ReadOnlySequence<byte> payload)
	{
		Packet? packet = MemoryPackSerializer.Deserialize<Packet>(payload);
		PacketHandler.Handle(this, packet);
	}

	protected override void OnDisconnected()
	{
		Zone? zone = Zone;
		if (zone is null)
		{
			return;
		}

		int uid = Uid;
		zone.Push(() => zone.Despawn(uid));
		LeaveZone();
	}

	public bool EnterZone(Zone zone)
	{
		if (Zone is not null)
		{
			return false;
		}

		Zone = zone;
		return true;
	}

	public void LeaveZone()
	{
		Zone = null;
	}
}
