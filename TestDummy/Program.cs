using System.Net;
using Microsoft.Extensions.Configuration;
using Core.Network;

namespace TestDummy;

internal class Program
{
	private sealed class Config
	{
		public string Host { get; set; } = "127.0.0.1";
		public int Port { get; set; } = 7777;
		public int Connections { get; set; } = 100;
		public int PingsPerSecond { get; set; } = 1;
	}

	private static async Task Main()
	{
		Config config = LoadConfig();
		int connections = config.Connections;
		int pingsPerSecond = config.PingsPerSecond;

		var endPoint = new IPEndPoint(IPAddress.Parse(config.Host), config.Port);
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

	private static Config LoadConfig()
	{
		IConfiguration configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
			.Build();

		Config? config = configuration.Get<Config>();
		if (config is null)
		{
			Console.WriteLine("[Config][WARN] appsettings.json 없음 — 기본값 사용");
			config = new Config();
		}

		return config;
	}
}
