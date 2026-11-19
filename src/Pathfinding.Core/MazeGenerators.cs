namespace Pathfinding.Core;

public interface IMazeGenerator
{
    string Name { get; }
    void Generate(Grid grid, Random random);
}

/// <summary>Perfect maze via randomized depth-first search (recursive backtracker), implemented with an explicit stack.</summary>
public sealed class RecursiveBacktracker : IMazeGenerator
{
    public string Name => "Recursive backtracker";

    public void Generate(Grid grid, Random random)
    {
        grid.Fill(CellType.Wall);
        // Carve on odd coordinates so walls sit between cells
        var start = new Point(1, 1);
        Carve(grid, start);
        var stack = new Stack<Point>([start]);
        (int dr, int dc)[] steps = [(-2, 0), (0, 2), (2, 0), (0, -2)];

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var options = steps
                .Select(s => new Point(current.Row + s.dr, current.Col + s.dc))
                .Where(p => p.Row > 0 && p.Col > 0 && p.Row < grid.Rows - 1 && p.Col < grid.Cols - 1 && grid[p] == CellType.Wall)
                .ToList();
            if (options.Count == 0)
            {
                stack.Pop();
                continue;
            }
            var next = options[random.Next(options.Count)];
            Carve(grid, new Point((current.Row + next.Row) / 2, (current.Col + next.Col) / 2));
            Carve(grid, next);
            stack.Push(next);
        }
        MazeHelpers.ConnectEndpoints(grid);
    }

    private static void Carve(Grid grid, Point p) => grid[p] = CellType.Empty;
}

/// <summary>Scatters walls and weights randomly at the given densities.</summary>
public sealed class RandomScatter(double wallDensity = 0.28, double weightDensity = 0.0) : IMazeGenerator
{
    public string Name => weightDensity > 0 ? "Random walls + weights" : "Random walls";

    public void Generate(Grid grid, Random random)
    {
        grid.Clear();
        foreach (var p in grid.AllPoints())
        {
            if (p == grid.Start || p == grid.Goal) continue;
            var roll = random.NextDouble();
            if (roll < wallDensity) grid[p] = CellType.Wall;
            else if (roll < wallDensity + weightDensity) grid[p] = CellType.Weight;
        }
    }
}

internal static class MazeHelpers
{
    /// <summary>Opens the cells around the start and goal so they join the carved maze.</summary>
    public static void ConnectEndpoints(Grid grid)
    {
        foreach (var endpoint in new[] { grid.Start, grid.Goal })
        {
            grid[endpoint] = CellType.Empty;
            // Walk toward the nearest carved (odd, odd) cell
            var target = new Point(endpoint.Row | 1, endpoint.Col | 1);
            if (!grid.InBounds(target) || target.Row >= grid.Rows - 1 || target.Col >= grid.Cols - 1)
                target = new Point(Math.Max(1, (endpoint.Row - 1) | 1), Math.Max(1, (endpoint.Col - 1) | 1));
            for (var r = Math.Min(endpoint.Row, target.Row); r <= Math.Max(endpoint.Row, target.Row); r++)
                grid[new Point(r, endpoint.Col)] = CellType.Empty;
            for (var c = Math.Min(endpoint.Col, target.Col); c <= Math.Max(endpoint.Col, target.Col); c++)
                grid[new Point(target.Row, c)] = CellType.Empty;
        }
    }
}
