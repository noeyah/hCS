using MemoryPack;
using Shared;
using Shared.Protocol;

namespace TestServer;

public static class PacketHandler
{
	private static int s_nextUid;

	public static void Handle(GameSession session, Packet? packet)
	{
		switch (packet)
		{
			case Ping ping:
				HandlePing(session, ping);
				break;

			case LoginReq req:
				HandleLoginReq(session, req);
				break;

			case EnterZoneReq:
				HandleEnterZoneReq(session);
				break;

			case MoveReq move:
				HandleMoveReq(session, move);
				break;
		}
	}

	private static void HandleMoveReq(GameSession session, MoveReq req)
	{
		if (session.Uid == 0)
		{
			return;
		}

		Zone? zone = session.Zone;
		if (zone is null)
		{
			return;
		}

		int uid = session.Uid;
		MoveFlag flags = req.Flags;
		zone.PushMove(uid, flags);
	}

	private static void HandlePing(GameSession session, Ping ping)
	{
		var tick = ping.Tick;

		Zone? zone = session.Zone;
		if (zone is null)
		{
			Send(session, new Pong { Tick = tick });
			return;
		}

		zone.PushPong(session, tick);
	}

	private static void HandleLoginReq(GameSession session, LoginReq req)
	{
		var res = new LoginRes();
		if (string.IsNullOrWhiteSpace(req.Name))
		{
			res.ErrorCode = ErrorCode.LOGIN_NAME_EMPTY;
			Send(session, res);
			return;
		}

		if (session.Uid != 0)
		{
			res.ErrorCode = ErrorCode.FAIL;
			Send(session, res);
			return;
		}

		session.Uid = Interlocked.Increment(ref s_nextUid);
		session.Name = req.Name;

		res.ErrorCode = ErrorCode.OK;
		res.id = session.Uid;
		Send(session, res);
	}

	private static void HandleEnterZoneReq(GameSession session)
	{
		if (session.Uid == 0)
		{
			var res = new EnterZoneRes();
			res.ErrorCode = ErrorCode.FAIL;
			Send(session, res);
			return;
		}

		// 집어넣을 존
		Zone zone = session.World.PickZone(session.Uid);
		if (!session.EnterZone(zone))
		{
			var res = new EnterZoneRes();
			res.ErrorCode = ErrorCode.FAIL;
			Send(session, res);
			return;
		}

		int uid = session.Uid;
		string name = session.Name;
		zone.PushSpawn(uid, name, session);
	}

	private static void Send(GameSession session, Packet packet)
	{
		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(packet);
		session.SendBody(bytes);
	}
}
