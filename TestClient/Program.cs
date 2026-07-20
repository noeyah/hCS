using System.Net;
using Core.Network;

namespace TestClient;

internal static class Program
{
	private const int Port = 7777;
	private const string Name = "me";

	[STAThread]
	private static void Main()
	{
		var endPoint = new IPEndPoint(IPAddress.Loopback, Port);
		var client = new ClientService();
		var session = (ClientSession)client
			.ConnectAsync(endPoint, () => new ClientSession())
			.GetAwaiter().GetResult();

		session.SendLogin(Name);
		session.SendEnterZone();

		ApplicationConfiguration.Initialize();
		Application.Run(new GameForm(session));
	}
}
