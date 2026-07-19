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

	public void SendPacket(ReadOnlySpan<byte> body)
	{
		// TODO. 매번 할당??? 수정 필요
		// 패킷 사이즈 체크 필요
		byte[] buffer = new byte[HeaderSize + body.Length];
		BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)body.Length);
		body.CopyTo(buffer.AsSpan(HeaderSize));

		Send(buffer);
	}

}
