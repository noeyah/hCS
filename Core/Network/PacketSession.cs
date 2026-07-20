using System.Buffers;
using System.Buffers.Binary;

namespace Core.Network;

public abstract class PacketSession : Session, ISender
{
	// [ushort][payload]
	private const int HeaderSize = sizeof(ushort);

	public abstract void OnMessage(ReadOnlySequence<byte> payload);

	protected sealed override int OnRecv(ReadOnlySequence<byte> buffer)
	{
		int totalRecvSize = 0;

		while (true)
		{
			if (buffer.Length < HeaderSize)
			{
				// 헤더 크기보다는 커야함
				break;
			}

			var reader = new SequenceReader<byte>(buffer);
			if (!reader.TryReadLittleEndian(out short len))
			{
				// 헤더 부족..?
				break;
			}
			ushort payloadSize = (ushort)len;
			if (reader.Remaining < payloadSize)
			{
				// 바디 부족
				break;
			}

			int packetSize = HeaderSize + payloadSize;

			ReadOnlySequence<byte> payload = buffer.Slice(HeaderSize, payloadSize);
			OnMessage(payload);

			totalRecvSize += packetSize;
			buffer = buffer.Slice(packetSize);
		}

		return totalRecvSize;
	}

	public static ReadOnlyMemory<byte> MakePacket(ReadOnlySpan<byte> body)
	{
		byte[] buffer = new byte[HeaderSize + body.Length];
		BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)body.Length);
		body.CopyTo(buffer.AsSpan(HeaderSize));
		return buffer;
	}

	public void SendBody(ReadOnlySpan<byte> body)
	{
		Send(MakePacket(body));
	}

	public void SendPacket(ReadOnlyMemory<byte> packet)
	{
		Send(packet);
	}
}
