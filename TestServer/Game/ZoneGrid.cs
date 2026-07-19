namespace TestServer;

public class ZoneGrid
{
	private readonly float _cellSize;
	private readonly int _gridSize;

	private readonly List<Player>[] _cells;
	private readonly int[][] _neighbors;

	public int CellCount => _cells.Length;

	public ZoneGrid(float mapSize, float cellSize)
	{
		_cellSize = cellSize;
		_gridSize = (int)MathF.Ceiling(mapSize / cellSize);

		int cellCount = _gridSize * _gridSize;
		_cells = new List<Player>[cellCount];
		_neighbors = new int[cellCount][];

		for (int i = 0; i < cellCount; i++)
		{
			_cells[i] = new List<Player>();
		}

		BuildNeighbors();
	}

	public int ToIndex(float x, float y)
	{
		int col = Math.Clamp((int)(x / _cellSize), 0, _gridSize - 1);
		int row = Math.Clamp((int)(y / _cellSize), 0, _gridSize - 1);
		return (row * _gridSize) + col;
	}

	public List<Player> GetPlayers(int cellIndex)
	{
		return _cells[cellIndex];
	}

	public void CollectNeighborPlayers(int cellIndex, List<Player> results)
	{
		results.Clear();

		foreach (int neighborIndex in _neighbors[cellIndex])
		{
			results.AddRange(_cells[neighborIndex]);
		}
	}

	public int[] GetNeighbors(int cellIndex)
	{
		return _neighbors[cellIndex];
	}

	public bool IsNeighbor(int indexA, int indexB)
	{
		int colA = indexA % _gridSize;
		int rowA = indexA / _gridSize;
		int colB = indexB % _gridSize;
		int rowB = indexB / _gridSize;

		return Math.Abs(colA - colB) <= 1 && Math.Abs(rowA - rowB) <= 1;
	}

	public void Add(Player player)
	{
		int index = ToIndex(player.X, player.Y);
		player.CellIndex = index;
		_cells[index].Add(player);
	}

	public void Remove(Player player)
	{
		if (player.CellIndex < 0)
		{
			return;
		}

		_cells[player.CellIndex].Remove(player);
		player.CellIndex = -1;
	}

	public bool TryUpdateCell(Player player, out int oldCellIndex)
	{
		oldCellIndex = player.CellIndex;

		int newIndex = ToIndex(player.X, player.Y);
		if (newIndex == oldCellIndex)
		{
			return false;
		}

		_cells[oldCellIndex].Remove(player);
		_cells[newIndex].Add(player);
		player.CellIndex = newIndex;
		return true;
	}

	private void BuildNeighbors()
	{
		var buffer = new List<int>(9);

		for (int row = 0; row < _gridSize; row++)
		{
			for (int col = 0; col < _gridSize; col++)
			{
				buffer.Clear();

				for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
				{
					for (int colOffset = -1; colOffset <= 1; colOffset++)
					{
						int neighborRow = row + rowOffset;
						int neighborCol = col + colOffset;
						if (neighborCol < 0 || neighborCol >= _gridSize
							|| neighborRow < 0 || neighborRow >= _gridSize)
						{
							continue;
						}

						buffer.Add((neighborRow * _gridSize) + neighborCol);
					}
				}

				_neighbors[(row * _gridSize) + col] = buffer.ToArray();
			}
		}
	}
}
