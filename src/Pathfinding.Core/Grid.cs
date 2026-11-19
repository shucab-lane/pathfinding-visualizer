namespace Pathfinding.Core;

/// <summary>A cell coordinate on the grid.</summary>
public readonly record struct Point(int Row, int Col)
{
    public override string ToString() => $"({Row}, {Col})";
}

public enum CellType
{
    Empty,
    Wall,
    /// <summary>Passable but expensive to cross (cost <see cref="Grid.WeightCost"/>).</summary>
    Weight,
}

/// <summary>
/// A rectangular grid of cells with a start and a goal.
/// Movement is 4-directional; entering a cell costs 1, or <see cref="WeightCost"/> for weighted cells.
/// </summary>
public sealed class Grid
{
    public const int WeightCost = 5;

    private static readonly (int dr, int dc)[] Directions = [(-1, 0), (0, 1), (1, 0), (0, -1)];

    private readonly CellType[,] _cells;

    public Grid(int rows, int cols)
    {
        if (rows < 2 || cols < 2) throw new ArgumentOutOfRangeException(nameof(rows), "Grid must be at least 2x2.");
        Rows = rows;
        Cols = cols;
        _cells = new CellType[rows, cols];
        Start = new Point(rows / 2, cols / 5);
        Goal = new Point(rows / 2, cols - 1 - cols / 5);
    }

    public int Rows { get; }
    public int Cols { get; }
    public Point Start { get; set; }
    public Point Goal { get; set; }

    public CellType this[Point p]
    {
        get => _cells[p.Row, p.Col];
        set
        {
            // Start and goal must always stay passable
            if (value == CellType.Wall && (p == Start || p == Goal)) return;
            _cells[p.Row, p.Col] = value;
        }
    }

    public bool InBounds(Point p) => p.Row >= 0 && p.Row < Rows && p.Col >= 0 && p.Col < Cols;

    public bool IsPassable(Point p) => InBounds(p) && _cells[p.Row, p.Col] != CellType.Wall;

    public int CostToEnter(Point p) => _cells[p.Row, p.Col] == CellType.Weight ? WeightCost : 1;

    public IEnumerable<Point> Neighbors(Point p)
    {
        foreach (var (dr, dc) in Directions)
        {
            var n = new Point(p.Row + dr, p.Col + dc);
            if (IsPassable(n)) yield return n;
        }
    }

    public void Clear(CellType? only = null)
    {
        for (var r = 0; r < Rows; r++)
        for (var c = 0; c < Cols; c++)
            if (only is null || _cells[r, c] == only) _cells[r, c] = CellType.Empty;
    }

    public void Fill(CellType type)
    {
        for (var r = 0; r < Rows; r++)
        for (var c = 0; c < Cols; c++)
            _cells[r, c] = type;
        _cells[Start.Row, Start.Col] = CellType.Empty;
        _cells[Goal.Row, Goal.Col] = CellType.Empty;
    }

    public IEnumerable<Point> AllPoints()
    {
        for (var r = 0; r < Rows; r++)
        for (var c = 0; c < Cols; c++)
            yield return new Point(r, c);
    }

    /// <summary>Parses an ASCII map: '#' wall, '~' weight, 'S' start, 'G' goal, anything else empty.</summary>
    public static Grid Parse(string map)
    {
        var lines = map.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var grid = new Grid(lines.Length, lines.Max(l => l.Length));
        for (var r = 0; r < lines.Length; r++)
        for (var c = 0; c < lines[r].Length; c++)
        {
            var p = new Point(r, c);
            switch (lines[r][c])
            {
                case '#': grid._cells[r, c] = CellType.Wall; break;
                case '~': grid._cells[r, c] = CellType.Weight; break;
                case 'S': grid.Start = p; break;
                case 'G': grid.Goal = p; break;
            }
        }
        return grid;
    }
}
