using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Core.Network;

public abstract class Session
{
	private Socket _socket = null!;
	private EndPoint _remoteEndPoint = null!;
	private int _disconnected = 0;

	// send
	private readonly Channel<ReadOnlyMemory<byte>> _sendChannel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions { SingleReader = true });
	//private readonly List<ArraySegment<byte>> _pending = new();

	protected abstract int OnRecv(ReadOnlySequence<byte> data);
	protected abstract void OnConnected();
	protected abstract void OnDisconnected();


	public void Start(Socket socket)
	{
		_socket = socket;
		_remoteEndPoint = socket.RemoteEndPoint!;
		
		OnConnected();
		_ = RunAsync();
	}

	public void Send(ReadOnlyMemory<byte> data)
	{
		_sendChannel.Writer.TryWrite(data);
	}

	public void Disconnect()
	{
		if (Interlocked.Exchange(ref _disconnected, 1) == 1)
		{
			return;
		}

		_sendChannel.Writer.TryComplete();
		OnDisconnected();

		try
		{
			_socket.Shutdown(SocketShutdown.Both);
		}
		catch
		{
		}

		_socket.Close();
	}

	private async Task RunAsync()
	{
		var pipe = new Pipe();

		Task send = ProcessSendAsync();

		Task recv = ProcessRecvAsync(pipe.Writer);
		Task read = ProcessReadAsync(pipe.Reader);

		await Task.WhenAll(recv, read);
		Disconnect();

		await send;
	}

	private async Task ProcessRecvAsync(PipeWriter writer)
	{
		// TODO. 밖으로 빼기
		const int MinBufferSize = 4096;

		try
		{
			while (true)
			{
				var memory = writer.GetMemory(MinBufferSize);
				int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None);
				if (bytesRead == 0)
				{
					// 연결 종료
					break;
				}
				writer.Advance(bytesRead);

				FlushResult result = await writer.FlushAsync();
				if (result.IsCompleted)
				{
					// reader 종료됨
					break;
				}
			}
		}
		catch
		{
			// log
		}
		finally
		{
			await writer.CompleteAsync();
		}
	}

	private async Task ProcessReadAsync(PipeReader reader)
	{
		try
		{
			while (true)
			{
				var result = await reader.ReadAsync();

				ReadOnlySequence<byte> buffer = result.Buffer;
				int processed = OnRecv(buffer);

				var readPos = buffer.GetPosition(processed);
				reader.AdvanceTo(readPos, buffer.End);
				if (result.IsCompleted)
				{
					// writer 종료됨
					break;
				}
			}
		}
		catch
		{
			// log
		}
		finally
		{
			await reader.CompleteAsync();
		}
	}

	private async Task ProcessSendAsync()
	{
		try
		{
			await foreach(var data in _sendChannel.Reader.ReadAllAsync())
			{
				await SendSingleAsync(data);
			}

			//while (await _sendChannel.Reader.WaitToReadAsync())
			//{
			//	while (_sendChannel.Reader.TryRead(out var more))
			//	{
			//	}
			//}
		}
		catch
		{
			// log
		}
	}

	private async Task SendSingleAsync(ReadOnlyMemory<byte> data)
	{
		int totalSent = 0;
		while (totalSent < data.Length)
		{
			int sent = await _socket.SendAsync(data[totalSent..], SocketFlags.None);
			if (sent == 0)
			{
				return;
			}

			totalSent += sent;
		}
	}

}
