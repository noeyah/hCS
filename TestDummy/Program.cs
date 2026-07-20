using System.Net;
using Core.Network;

namespace TestDummy;

internal class Program
{
	private static async Task Main(string[] args)
	{
		int connections = args.Length > 0 ? int.Parse(args[0]) : 100;
		int pingsPerSecond = args.Length > 1 ? int.Parse(args[1]) : 1;
		int port = args.Length > 2 ? int.Parse(args[2]) : 7777;

		var endPoint = new IPEndPoint(IPAddress.Loopback, port);
		var client = new ClientService();

		Console.WriteLine($"[Dummy] connecting {connections} sessions -> {endPoint} ({pingsPerSecond} ping/s each)");

		var sessions = new List<DummySession>(connections);
		for (int i = 0; i < connections; i++)
		{
			var session = (DummySession)await client.ConnectAsync(endPoint, () => new DummySession());
			session.SendLogin($"dummy{i}");
			session.SendEnterZone();
			sessions.Add(session);
		}

		await Task.Delay(500);
		Console.WriteLine(
			$"[Dummy] login ok: {DummySession.LoginOkCount}/{connections}, "
			+ $"enter: {DummySession.EnterCount}, enterNtf: {DummySession.EnterNtfCount}");

		Console.WriteLine($"[Dummy] sending moves (ping {pingsPerSecond}/s)...");

		if (pingsPerSecond > 0)
		{
			int intervalMs = Math.Max(1, 1000 / pingsPerSecond);

			_ = Task.Run(async () =>
			{
				while (true)
				{
					foreach (DummySession session in sessions)
					{
						session.SendPing();
					}

					await Task.Delay(intervalMs);
				}
			});
		}

		_ = Task.Run(async () =>
		{
			var rng = new Random();
			while (true)
			{
				foreach (DummySession session in sessions)
				{
					session.SendRandomMove(rng);
				}

				await Task.Delay(1000);
			}
		});

		int lastMoveNtf = 0;
		while (true)
		{
			await Task.Delay(1000);

			Metrics.ReportAndReset(connections);

			int moveNtf = DummySession.MoveNtfCount;
			Console.WriteLine($"[Dummy] moveNtf/s={moveNtf - lastMoveNtf} (total={moveNtf})");
			lastMoveNtf = moveNtf;
		}
	}
}
