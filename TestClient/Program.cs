using System.Net;
using Microsoft.Extensions.Configuration;
using Core.Network;

namespace TestClient;

internal static class Program
{
	private sealed class Config
	{
		public string Host { get; set; } = "127.0.0.1";
		public int Port { get; set; } = 7777;
		public string Name { get; set; } = "me";
	}

	[STAThread]
	private static void Main()
	{
		Config config = LoadConfig();
		var endPoint = new IPEndPoint(IPAddress.Parse(config.Host), config.Port);
		var client = new ClientService();
		var session = (ClientSession)client
			.ConnectAsync(endPoint, () => new ClientSession())
			.GetAwaiter().GetResult();

		session.SendLogin(config.Name);
		session.SendEnterZone();

		ApplicationConfiguration.Initialize();
		Application.Run(new GameForm(session));
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
			MessageBox.Show("appsettings.json 없음 — 기본값 사용", "Config", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			config = new Config();
		}

		return config;
	}
}
