namespace Core.Network;

public interface ISender
{
	public void SendPacket(ReadOnlyMemory<byte> packet);
}
