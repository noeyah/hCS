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
	private readonly ZoneGrid _grid;
	private readonly ZoneMetrics _metrics;
	private readonly int _tickHz;
	private readonly Thread _thread;
	private volatile bool _running;

	// update에서 사용
	private readonly List<MoveInfo>[] _cellMoves;
	private readonly HashSet<int> _dirtyCells = new();
	private readonly List<(Player p, int oldCellIdx)> _transitions = new();
	private readonly List<Player> _enterTmp;
	private readonly List<Player> _leaveTmp;

	// 할당해놓고 임의 데이터 넣어서 계속 씀
	private readonly List<Player> _spawnTmp;
	private readonly List<Player> _bcTmp;

	private const float Speed = 5f;
	private const float MapMin = 0f;
	private const float MapMax = 100f;
	private const float CellSize = 10f;

	public Zone(int tickHz = 20)
	{
		_tickHz = tickHz;
		_grid = new ZoneGrid(MapMax, CellSize);
		_metrics = new ZoneMetrics(tickHz);
		_thread = new Thread(Run)
		{
			Name = "ZoneThread",
			IsBackground = true,
		};

		_cellMoves = new List<MoveInfo>[_grid.CellCount];
		for (int i = 0; i < _cellMoves.Length; i++)
		{
			_cellMoves[i] = new List<MoveInfo>();
		}

		_spawnTmp = new List<Player>(200); // 셀당 최대치*9?
		_bcTmp = new List<Player>(200);
		_enterTmp = new List<Player>(200);
		_leaveTmp = new List<Player>(200);
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
			X = Random.Shared.NextSingle() * MapMax,
			Y = Random.Shared.NextSingle() * MapMax,
		};

		enterRes.ErrorCode = ErrorCode.OK;
		enterRes.X = player.X;
		enterRes.Y = player.Y;
		Unicast(player.Session, enterRes);

		_grid.CollectNeighborPlayers(_grid.ToIndex(player.X, player.Y), _spawnTmp);
		if (_spawnTmp.Count > 0)
		{
			var infos = new PlayerInfo[_spawnTmp.Count];
			int i = 0;

			foreach (Player p in _spawnTmp)
			{
				infos[i++] = ToInfo(p);
			}

			var listNtf = new EnterZoneNtf { Players = infos };
			Unicast(player.Session, listNtf);
		}

		_grid.Add(player);
		_players[uid] = player;

		var spawnNtf = new EnterZoneNtf { Players = [ToInfo(player)] };
		BroadcastToNeighbor(player.CellIndex, spawnNtf, uid);

		Console.WriteLine($"[Zone] spawn {uid}({name}) (players {_players.Count})");
	}

	public void Despawn(int uid)
	{
		AssertZoneThread();

		if (!_players.TryGetValue(uid, out var player))
		{
			return;
		}

		int cellIndex = player.CellIndex;

		_players.Remove(uid);
		_grid.Remove(player);

		var leave = new LeaveZoneNtf { Uids = [uid] };
		BroadcastToNeighbor(cellIndex, leave, uid);

		Console.WriteLine($"[Zone] despawn {uid} (players {_players.Count})");
	}

	private void Unicast(ISender sender, Packet packet)
	{
		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(packet);
		Unicast(sender, PacketSession.MakePacket(bytes));
	}

	private void Unicast(ISender sender, ReadOnlyMemory<byte> packet)
	{
		sender.SendPacket(packet);
		_metrics.OnSent(packet.Length);
	}

	private void Broadcast(Packet packet, int exceptUid = 0)
	{
		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(packet);
		Broadcast(PacketSession.MakePacket(bytes), exceptUid);
	}

	private void Broadcast(ReadOnlyMemory<byte> packet, int exceptUid = 0)
	{
		foreach (Player p in _players.Values)
		{
			if (p.Uid == exceptUid)
			{
				continue;
			}

			Unicast(p.Session, packet);
		}
	}

	private void BroadcastToNeighbor(int cellIndex, Packet packet, int exceptUid = 0)
	{
		byte[] bytes = MemoryPackSerializer.Serialize<Packet>(packet);
		BroadcastToNeighbor(cellIndex, PacketSession.MakePacket(bytes), exceptUid);
	}

	private void BroadcastToNeighbor(int cellIndex, ReadOnlyMemory<byte> packet, int exceptUid = 0)
	{
		_grid.CollectNeighborPlayers(cellIndex, _bcTmp);

		foreach (Player p in _bcTmp)
		{
			if (p.Uid == exceptUid)
			{
				continue;
			}

			Unicast(p.Session, packet);
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
			try
			{
				job();
				_metrics.OnJob();
			}
			catch
			{
				// log
			}
		}
	}

	private void Update()
	{
		AssertZoneThread();

		float dt = 1f / _tickHz;

		foreach (var idx in _dirtyCells)
		{
			_cellMoves[idx].Clear();
		}
		_dirtyCells.Clear();
		_transitions.Clear();

		foreach (Player p in _players.Values)
		{
			if (p.Move == MoveFlag.None)
			{
				continue;
			}

			(float dx, float dy) = ToDir(p.Move);
			p.X = Math.Clamp(p.X + dx * Speed * dt, MapMin, MapMax);
			p.Y = Math.Clamp(p.Y + dy * Speed * dt, MapMin, MapMax);

			if (_grid.TryUpdateCell(p, out var oldCellIndex))
			{
				_transitions.Add((p, oldCellIndex));
			}

			_dirtyCells.Add(p.CellIndex);
			_cellMoves[p.CellIndex].Add(new MoveInfo { Uid = p.Uid, X = p.X, Y = p.Y });
		}


		foreach ((Player p, int oldCellIdx) in _transitions)
		{
			HandleTransition(p, oldCellIdx);
		}

		foreach (var idx in _dirtyCells)
		{
			var ntf = new MoveNtf { Moves = _cellMoves[idx].ToArray() };
			BroadcastToNeighbor(idx, ntf);
		}
	}

	private void HandleTransition(Player mover, int oldCellIdx)
	{
		// 새이웃
		CollectCellDiff(mover.CellIndex, oldCellIdx, mover.Uid, _enterTmp);
		NotifyEnter(mover, _enterTmp);

		// 옛이웃
		CollectCellDiff(oldCellIdx, mover.CellIndex, mover.Uid, _leaveTmp);
		NotifyLeave(mover, _leaveTmp);
	}

	private void CollectCellDiff(int cellIndex, int otherCellIndex, int excludeUid, List<Player> results)
	{
		results.Clear();

		foreach (int neighbor in _grid.GetNeighbors(cellIndex))
		{
			if (_grid.IsNeighbor(otherCellIndex, neighbor))
			{
				continue;
			}

			foreach (Player p in _grid.GetPlayers(neighbor))
			{
				if (p.Uid == excludeUid)
				{
					continue;
				}

				results.Add(p);
			}
		}
	}

	private void NotifyEnter(Player mover, List<Player> others)
	{
		if (others.Count == 0)
		{
			return;
		}

		var infos = new PlayerInfo[others.Count];
		int i = 0;
		foreach (Player p in others)
		{
			infos[i++] = ToInfo(p);
		}
		Unicast(mover.Session, new EnterZoneNtf { Players = infos });

		var moverNtf = new EnterZoneNtf { Players = [ToInfo(mover)] };
		var bytes = MemoryPackSerializer.Serialize<Packet>(moverNtf);
		var packet = PacketSession.MakePacket(bytes);
		foreach (Player p in others)
		{
			Unicast(p.Session, packet);
		}
	}

	private void NotifyLeave(Player mover, List<Player> others)
	{
		if (others.Count == 0)
		{
			return;
		}

		var uids = new int[others.Count];
		int i = 0;
		foreach (Player p in others)
		{
			uids[i++] = p.Uid;
		}
		Unicast(mover.Session, new LeaveZoneNtf { Uids = uids });

		var moverNtf = new LeaveZoneNtf { Uids = [mover.Uid] };
		var bytes = MemoryPackSerializer.Serialize<Packet>(moverNtf);
		var packet = PacketSession.MakePacket(bytes);
		foreach (Player p in others)
		{
			Unicast(p.Session, packet);
		}
	}
}
