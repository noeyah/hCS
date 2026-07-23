using System.Net;
using Microsoft.Extensions.Configuration;
using Core.Network;

namespace TestServer;

internal class Program
{
	private sealed class Config
	{
		public int Port { get; set; } = 7777;
	}

	private static void Main()
	{
		Config config = LoadConfig();
		var endPoint = new IPEndPoint(IPAddress.Any, config.Port);

		var world = new World(tickHz: 20);
		world.Start();

		var service = new ServerService();
		service.Start(endPoint, () => new GameSession(world));

		Console.WriteLine($"[Server] Start : {endPoint}");

		if (Console.IsInputRedirected)
		{
			Thread.Sleep(Timeout.Infinite);
		}
		else
		{
			Console.WriteLine("Press Enter to exit.");
			Console.ReadLine();
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
