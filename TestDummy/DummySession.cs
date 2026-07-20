using System.Buffers;
using System.Diagnostics;
using Core.Network;
using MemoryPack;
using Shared;
using Shared.Protocol;

namespace TestDummy;

public class DummySession : PacketSession
{
	public static int LoginOkCount;
	public static int EnterCount;
	public static int EnterNtfCount;
	public static int LeaveNtfCount;
	public static int MoveNtfCount;

	private static readonly MoveFlag[] s_dirs =
	{
		MoveFlag.Up,
		MoveFlag.Down,
		MoveFlag.Left,
		MoveFlag.Right,
		MoveFlag.Up | MoveFlag.Right,
		MoveFlag.Down | MoveFlag.Left,
	};

	private int _id;

	protected override void OnConnected()
	{
	}

	public void SendLogin(string name)
	{
		var req = new LoginReq { Name = name };
		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(req);
		SendBody(bytes);
	}

	public void SendEnterZone()
	{
		var req = new EnterZoneReq();
		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(req);
		SendBody(bytes);
	}

	public void SendRandomMove(Random rng)
	{
		var req = new MoveReq { Flags = s_dirs[rng.Next(s_dirs.Length)] };
		SendBody(MemoryPackSerializer.Serialize<Packet>(req));
	}

	public void SendPing()
	{
		var ping = new Ping { Tick = Stopwatch.GetTimestamp() };

		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(ping);
		SendBody(bytes);
	}

	public override void OnMessage(ReadOnlySequence<byte> payload)
	{
		Packet? packet = MemoryPackSerializer.Deserialize<Packet>(payload);

		if (packet is Pong pong)
		{
			long elapsedTicks = Stopwatch.GetTimestamp() - pong.Tick;
			double rttMs = elapsedTicks * 1000.0 / Stopwatch.Frequency;

			Metrics.Record(rttMs);
		}
		else if (packet is LoginRes res)
		{
			if (res.ErrorCode == ErrorCode.OK)
			{
				_id = res.id;
				Interlocked.Increment(ref LoginOkCount);
			}
		}
		else if (packet is EnterZoneRes)
		{
			Interlocked.Increment(ref EnterCount);
		}
		else if (packet is EnterZoneNtf)
		{
			Interlocked.Increment(ref EnterNtfCount);
		}
		else if (packet is LeaveZoneNtf)
		{
			Interlocked.Increment(ref LeaveNtfCount);
		}
		else if (packet is MoveNtf)
		{
			Interlocked.Increment(ref MoveNtfCount);
		}
	}

	protected override void OnDisconnected()
	{
	}
}
