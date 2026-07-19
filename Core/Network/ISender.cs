namespace Core.Network;

public interface ISender
{
	public void SendPacket(ReadOnlySpan<byte> body);
}
