using System.Net;
using Core.Network;

namespace TestServer;

internal class Program
{
	private static void Main(string[] args)
	{
		int port = args.Length > 0 ? int.Parse(args[0]) : 7777;
		var endPoint = new IPEndPoint(IPAddress.Any, port);

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
}
