using System.Net;
using System.Net.Sockets;

namespace Core.Network;

public class ServerService
{
	private Socket _listenSocket = null!;
	private Func<Session> _sessionFactory = null!;

	public void Start(IPEndPoint endPoint, Func<Session> sessionFactory, int backlog = 100)
	{
		_sessionFactory = sessionFactory;

		_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		_listenSocket.Bind(endPoint);
		_listenSocket.Listen(backlog);

		_ = ProcessAcceptAsync();
	}

	private async Task ProcessAcceptAsync()
	{
		while (true)
		{
			try
			{
				Socket clientSocket = await _listenSocket.AcceptAsync();

				Session session = _sessionFactory();
				session.Start(clientSocket);
			}
			catch
			{
				// log
			}
		}
	}
}
