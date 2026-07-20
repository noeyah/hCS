namespace TestDummy;

public static class Metrics
{
	private const int Capacity = 1 << 18;
	private const int IndexMask = Capacity - 1;

	private static readonly long[] s_samples = new long[Capacity];
	private static readonly long[] s_sortBuffer = new long[Capacity];

	private static long s_count;
	private static long s_sumMicros;
	private static int s_writeIndex;

	public static void Record(double rttMs)
	{
		long micros = (long)(rttMs * 1000.0);

		Interlocked.Increment(ref s_count);
		Interlocked.Add(ref s_sumMicros, micros);

		int index = Interlocked.Increment(ref s_writeIndex) - 1;
		s_samples[index & IndexMask] = micros;
	}

	public static void ReportAndReset(int connections)
	{
		long count = Interlocked.Exchange(ref s_count, 0);
		long sumMicros = Interlocked.Exchange(ref s_sumMicros, 0);
		int written = Interlocked.Exchange(ref s_writeIndex, 0);

		if (count == 0)
		{
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] conn={connections} roundtrips/s=0");
			return;
		}

		double avgMs = sumMicros / (double)count / 1000.0;

		int n = Math.Min(written, Capacity);
		Array.Copy(s_samples, s_sortBuffer, n);
		Array.Sort(s_sortBuffer, 0, n);

		Console.WriteLine(
			$"[{DateTime.Now:HH:mm:ss}] conn={connections} roundtrips/s={count}" +
			$" | RTT avg={avgMs:F2}ms p50={ToMs(Percentile(n, 0.50)):F2}ms" +
			$" p95={ToMs(Percentile(n, 0.95)):F2}ms p99={ToMs(Percentile(n, 0.99)):F2}ms" +
			$" max={ToMs(Percentile(n, 1.00)):F2}ms");
	}

	private static long Percentile(int count, double p)
	{
		if (count <= 0)
		{
			return 0;
		}

		int index = (int)Math.Ceiling(p * count) - 1;
		index = Math.Clamp(index, 0, count - 1);
		return s_sortBuffer[index];
	}

	private static double ToMs(long micros)
	{
		return micros / 1000.0;
	}
}
