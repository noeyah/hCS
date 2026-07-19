using System.Diagnostics;

namespace TestServer;

public class TickTimer
{
	private readonly double _intervalMs;
	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
	private double _nextTickMs;

	public double ElapsedMs => _stopwatch.Elapsed.TotalMilliseconds;

	public TickTimer(int tickHz)
	{
		_intervalMs = 1000.0 / tickHz; // 50
		_nextTickMs = ElapsedMs;
	}

	// 밀린 시간 반환
	public double WaitNextTick()
	{
		_nextTickMs += _intervalMs;

		double sleepMs = _nextTickMs - ElapsedMs;
		if (sleepMs > 1.0)
		{
			Thread.Sleep((int)sleepMs);
			return 0.0;
		}

		if (sleepMs >= 0.0)
		{
			return 0.0;
		}

		if (sleepMs < -_intervalMs)
		{
			// 한 틱 넘게 밀림 = 최적화 필요
			_nextTickMs = ElapsedMs;
		}

		return -sleepMs;
	}
}
