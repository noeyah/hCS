using System.Net;
using System.Net.Sockets;

namespace Core.Network;

public class ClientService
{
	public async Task<Session> ConnectAsync(IPEndPoint endPoint, Func<Session> sessionFactory)
	{
		var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		await socket.ConnectAsync(endPoint);

		Session session = sessionFactory();
		session.Start(socket);

		return session;
	}
}
