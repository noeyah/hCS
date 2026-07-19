using System.Diagnostics;

namespace TestServer;

public class ZoneMetrics
{
	private const double ReportIntervalMs = 1000.0;

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
	private readonly double _intervalMs;

	private long _lateTicks;
	private long _skippedTicks;
	private double _maxLateMs;

	private long _tickCount;
	private long _jobCount;
	private long _sentPackets;
	private long _sentBytes;
	private long _allocBytes;

	private double _lastReportMs;
	private long _lastTicks;
	private long _lastJobs;
	private long _lastPackets;
	private long _lastBytes;
	private long _lastAlloc;
	private int _lastGen0 = GC.CollectionCount(0);
	private int _lastGen1 = GC.CollectionCount(1);
	private int _lastGen2 = GC.CollectionCount(2);

	public ZoneMetrics(int tickHz)
	{
		_intervalMs = 1000.0 / tickHz;
	}

	public void OnJob()
	{
		_jobCount++;
	}

	public void OnLate(double lateMs)
	{
		if (lateMs <= 0.0)
		{
			return;
		}

		_lateTicks++;
		if (lateMs > _maxLateMs)
		{
			_maxLateMs = lateMs;
		}

		if (lateMs > _intervalMs)
		{
			_skippedTicks++;
		}
	}

	public void OnSent(int bytes)
	{
		_sentPackets++;
		_sentBytes += bytes;
	}

	public void OnTick(long allocBytes)
	{
		_tickCount++;
		_allocBytes += allocBytes;
	}

	public void Report(int playerCount)
	{
		double now = _stopwatch.Elapsed.TotalMilliseconds;
		if (now - _lastReportMs < ReportIntervalMs)
		{
			return;
		}

		long ticks = _tickCount - _lastTicks;
		long allocDelta = _allocBytes - _lastAlloc;
		int gen0 = GC.CollectionCount(0);
		int gen1 = GC.CollectionCount(1);
		int gen2 = GC.CollectionCount(2);

		Console.WriteLine(
			$"[Zone] Tick/s : {ticks} | Job/s : {_jobCount - _lastJobs} | Player : {playerCount}" +
			$" || PacketCount/s : {_sentPackets - _lastPackets} | SendBytes/s : {(_sentBytes - _lastBytes) / 1024.0:F1}KB" +
			$" || Alloc/Tick : {(ticks > 0 ? allocDelta / ticks : 0)}B | Alloc/s : {allocDelta / 1024.0:F1}KB | AllocTotal : {_allocBytes / (1024.0 * 1024.0):F1}MB" +
			$" || GC Gen0/1/2 : {gen0 - _lastGen0}/{gen1 - _lastGen1}/{gen2 - _lastGen2} | Heap : {GC.GetTotalMemory(false) / (1024.0 * 1024.0):F1}MB");

		if (_lateTicks > 0)
		{
			Console.WriteLine(
				$"[Zone][WARN] 틱 밀림 : {_lateTicks}회 | 최대 {_maxLateMs:F1}ms" +
				$" | 한 틱 이상 밀려 건너뜀 : {_skippedTicks}회");
		}

		_lastReportMs = now;
		_lastTicks = _tickCount;
		_lastJobs = _jobCount;
		_lastPackets = _sentPackets;
		_lastBytes = _sentBytes;
		_lastAlloc = _allocBytes;
		_lastGen0 = gen0;
		_lastGen1 = gen1;
		_lastGen2 = gen2;
		_lateTicks = 0;
		_skippedTicks = 0;
		_maxLateMs = 0.0;
	}
}
