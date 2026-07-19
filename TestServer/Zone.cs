using Core.Network;
using MemoryPack;
using Shared;
using Shared.Protocol;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TestServer;

public class Zone
{
	private readonly ConcurrentQueue<Action> _jobs = new();

	private readonly Dictionary<int, Player> _players = new();
	private readonly ZoneMetrics _metrics;
	private readonly int _tickHz;
	private readonly Thread _thread;
	private volatile bool _running;

	private const float Speed = 5f;
	private const float MapMin = 0f;
	private const float MapMax = 100f;

	public Zone(int tickHz = 20)
	{
		_tickHz = tickHz;
		_metrics = new ZoneMetrics(tickHz);
		_thread = new Thread(Run)
		{
			Name = "ZoneThread",
			IsBackground = true,
		};
	}

	public void Start()
	{
		_running = true;
		_thread.Start();
	}

	public void Stop()
	{
		_running = false;
	}

	public void Push(Action job)
	{
		_jobs.Enqueue(job);
	}

	[Conditional("DEBUG")]
	private void AssertZoneThread()
	{
		Debug.Assert(Thread.CurrentThread == _thread, "Zone 처리는 Zone 스레드에서.");
	}

	public void SetMove(int uid, MoveFlag flags)
	{
		AssertZoneThread();

		if (_players.TryGetValue(uid, out Player? p))
		{
			p.Move = flags;
		}
	}

	public void Spawn(int uid, string name, ISender session)
	{
		AssertZoneThread();

		var enterRes = new EnterZoneRes();

		if (_players.ContainsKey(uid))
		{
			enterRes.ErrorCode = ErrorCode.FAIL;
			Unicast(session, enterRes);
			return;
		}

		var player = new Player(uid, name, session)
		{
			X = 0,
			Y = 0,
		};

		enterRes.ErrorCode = ErrorCode.OK;
		enterRes.X = player.X;
		enterRes.Y = player.Y;
		Unicast(player.Session, enterRes);

		if (_players.Count > 0)
		{
			var existing = new PlayerInfo[_players.Count];
			int i = 0;
			foreach (Player p in _players.Values)
			{
				existing[i++] = ToInfo(p);
			}

			var listNtf = new EnterZoneNtf { Players = existing };
			Unicast(player.Session, listNtf);
		}

		_players[uid] = player;

		var spawnNtf = new EnterZoneNtf { Players = [ToInfo(player)] };
		Broadcast(spawnNtf, uid);

		Console.WriteLine($"[Zone] spawn {uid}({name}) (players {_players.Count})");
	}

	public void Despawn(int uid)
	{
		AssertZoneThread();

		if (!_players.Remove(uid))
		{
			return;
		}

		var leave = new LeaveZoneNtf { Uid = uid };
		Broadcast(leave);

		Console.WriteLine($"[Zone] despawn {uid} (players {_players.Count})");
	}

	private void Unicast(ISender sender, Packet packet)
	{
		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(packet);
		Unicast(sender, bytes);
	}

	private void Unicast(ISender sender, byte[] bytes)
	{
		sender.SendPacket(bytes);
		_metrics.OnSent(bytes.Length);
	}

	private void Broadcast(Packet packet, int exceptUid = 0)
	{
		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(packet);
		Broadcast(bytes, exceptUid);
	}

	private void Broadcast(byte[] bytes, int exceptUid = 0)
	{
		foreach (Player p in _players.Values)
		{
			if (p.Uid == exceptUid)
			{
				continue;
			}

			Unicast(p.Session, bytes);
		}
	}

	private static PlayerInfo ToInfo(Player p)
	{
		return new PlayerInfo { Uid = p.Uid, Name = p.Name, X = p.X, Y = p.Y };
	}

	private static (float x, float y) ToDir(MoveFlag f)
	{
		float x = 0f;
		float y = 0f;

		if ((f & MoveFlag.Right) != 0)
		{
			x += 1f;
		}
		if ((f & MoveFlag.Left) != 0)
		{
			x -= 1f;
		}
		if ((f & MoveFlag.Up) != 0)
		{
			y += 1f;
		}
		if ((f & MoveFlag.Down) != 0)
		{
			y -= 1f;
		}

		float len = MathF.Sqrt(x * x + y * y);
		if (len > 0f)
		{
			x /= len;
			y /= len;
		}

		return (x, y);
	}

	private void Run()
	{
		var timer = new TickTimer(_tickHz);

		while (_running)
		{
			Tick();
			_metrics.OnLate(timer.WaitNextTick());
		}
	}

	private void Tick()
	{
		long allocStart = GC.GetAllocatedBytesForCurrentThread();

		ProcessJobs();
		Update();

		_metrics.OnTick(GC.GetAllocatedBytesForCurrentThread() - allocStart);
		_metrics.Report(_players.Count);
	}

	private void ProcessJobs()
	{
		while (_jobs.TryDequeue(out Action? job))
		{
			job();
			_metrics.OnJob();
		}
	}

	private void Update()
	{
		AssertZoneThread();

		float dt = 1f / _tickHz;
		List<MoveInfo>? moves = null;

		foreach (Player p in _players.Values)
		{
			if (p.Move == MoveFlag.None)
			{
				continue;
			}

			(float dx, float dy) = ToDir(p.Move);
			p.X = Math.Clamp(p.X + dx * Speed * dt, MapMin, MapMax);
			p.Y = Math.Clamp(p.Y + dy * Speed * dt, MapMin, MapMax);

			moves ??= new List<MoveInfo>();
			moves.Add(new MoveInfo { Uid = p.Uid, X = p.X, Y = p.Y });
		}

		if (moves != null)
		{
			var ntf = new MoveNtf { Moves = moves.ToArray() };
			Broadcast(ntf);
		}
	}
}
