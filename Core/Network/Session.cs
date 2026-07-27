using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Core.Network;

public abstract class Session
{
	private Socket _socket = null!;
	private EndPoint _remoteEndPoint = null!;
	private int _disconnected = 0;

	// send
	private readonly Channel<ReadOnlyMemory<byte>> _sendChannel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions { SingleReader = true });

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

	protected void Send(ReadOnlyMemory<byte> data)
	{
		_sendChannel.Writer.TryWrite(data);
	}

	private async Task RunAsync()
	{
		var pipe = new Pipe();

		Task send = SendLoopAsync();

		Task recv = RecvLoopAsync(pipe.Writer);
		Task read = ReadLoopAsync(pipe.Reader);

		await Task.WhenAll(recv, read);
		Disconnect();

		await send;
	}

	private async Task RecvLoopAsync(PipeWriter writer)
	{
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
		catch (Exception e) when (IsDisconnect(e))
		{
			// 정상 종료
		}
		catch (Exception e)
		{
			LogError(e);
		}
		finally
		{
			await writer.CompleteAsync();
		}
	}

	private async Task ReadLoopAsync(PipeReader reader)
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
		catch (Exception e) when (IsDisconnect(e))
		{
			// 정상 종료
		}
		catch (Exception e)
		{
			LogError(e);
		}
		finally
		{
			await reader.CompleteAsync();
		}
	}

	private async Task SendLoopAsync()
	{
		try
		{
			await foreach(var data in _sendChannel.Reader.ReadAllAsync())
			{
				await _socket.SendAsync(data, SocketFlags.None);
			}
		}
		catch (Exception e) when (IsDisconnect(e))
		{
			// 정상 종료
		}
		catch (Exception e)
		{
			LogError(e);
		}
	}

	private static bool IsDisconnect(Exception e)
	{
		if (e is SocketException se)
		{
			return se.SocketErrorCode is SocketError.ConnectionReset
				or SocketError.ConnectionAborted
				or SocketError.OperationAborted;
		}

		return e is ObjectDisposedException or OperationCanceledException;
	}


	private static void LogError(Exception e, [CallerMemberName] string caller = "",  [CallerLineNumber] int line = 0)
	{
		Console.WriteLine($"[Session.{caller}:{line}] {e}");
	}
}
