using MemoryPack;

namespace Shared;


[Flags]
public enum MoveFlag : byte
{
	None = 0,
	Up = 1,
	Down = 2,
	Left = 4,
	Right = 8,
}
