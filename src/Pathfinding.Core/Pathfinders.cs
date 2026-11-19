using System.Diagnostics;

namespace Pathfinding.Core;

/// <summary>The outcome of a search: the order cells were explored in, and the path found (if any).</summary>
public sealed record SearchResult(
    string Algorithm,
    IReadOnlyList<Point> Visited,
    IReadOnlyList<Point> Path,
    int Cost,
    TimeSpan Elapsed)
{
    public bool Found => Path.Count > 0;
}

public interface IPathfinder
{
    string Name { get; }
    /// <summary>Whether the algorithm always returns a lowest-cost path on weighted grids.</summary>
    bool GuaranteesShortestPath { get; }
    SearchResult FindPath(Grid grid);
}

/// <summary>Shared bookkeeping: timing, parent pointers and path reconstruction.</summary>
public abstract class PathfinderBase : IPathfinder
{
    public abstract string Name { get; }
    public abstract bool GuaranteesShortestPath { get; }

    public SearchResult FindPath(Grid grid)
    {
        var sw = Stopwatch.StartNew();
        var visited = new List<Point>();
        var parents = new Dictionary<Point, Point>();
        var found = Search(grid, visited, parents);
        sw.Stop();

        if (!found) return new SearchResult(Name, visited, [], 0, sw.Elapsed);
        var path = Reconstruct(parents, grid.Start, grid.Goal);
        var cost = path.Skip(1).Sum(grid.CostToEnter);
        return new SearchResult(Name, visited, path, cost, sw.Elapsed);
    }

    /// <summary>Runs the search, recording visit order and parent links. Returns true if the goal was reached.</summary>
    protected abstract bool Search(Grid grid, List<Point> visited, Dictionary<Point, Point> parents);

    private static List<Point> Reconstruct(Dictionary<Point, Point> parents, Point start, Point goal)
    {
        var path = new List<Point> { goal };
        var current = goal;
        while (current != start)
        {
            current = parents[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    protected static int Manhattan(Point a, Point b) => Math.Abs(a.Row - b.Row) + Math.Abs(a.Col - b.Col);
}

/// <summary>Breadth-first search: fewest steps, but ignores weights.</summary>
public sealed class BreadthFirstSearch : PathfinderBase
{
    public override string Name => "Breadth-First Search";
    public override bool GuaranteesShortestPath => false;

    protected override bool Search(Grid grid, List<Point> visited, Dictionary<Point, Point> parents)
    {
        var queue = new Queue<Point>([grid.Start]);
        var seen = new HashSet<Point> { grid.Start };
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            visited.Add(current);
            if (current == grid.Goal) return true;
            foreach (var n in grid.Neighbors(current))
            {
                if (!seen.Add(n)) continue;
                parents[n] = current;
                queue.Enqueue(n);
            }
        }
        return false;
    }
}

/// <summary>Depth-first search: finds *a* path quickly, usually not a short one.</summary>
public sealed class DepthFirstSearch : PathfinderBase
{
    public override string Name => "Depth-First Search";
    public override bool GuaranteesShortestPath => false;

    protected override bool Search(Grid grid, List<Point> visited, Dictionary<Point, Point> parents)
    {
        var stack = new Stack<Point>([grid.Start]);
        var seen = new HashSet<Point>();
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!seen.Add(current)) continue;
            visited.Add(current);
            if (current == grid.Goal) return true;
            // Push in reverse so the first direction is explored first
            foreach (var n in grid.Neighbors(current).Reverse())
            {
                if (seen.Contains(n)) continue;
                parents[n] = current;
                stack.Push(n);
            }
        }
        return false;
    }
}

/// <summary>
/// Best-first search on f(n) = g(n) + w·h(n). With w = 0 this is Dijkstra, with w = 1 it is A*,
/// and with g ignored it becomes greedy best-first search.
/// </summary>
public abstract class PriorityPathfinder : PathfinderBase
{
    protected abstract int Priority(int costSoFar, int heuristic);

    protected override bool Search(Grid grid, List<Point> visited, Dictionary<Point, Point> parents)
    {
        var open = new PriorityQueue<Point, (int f, int h)>();
        var best = new Dictionary<Point, int> { [grid.Start] = 0 };
        var closed = new HashSet<Point>();
        open.Enqueue(grid.Start, (0, Manhattan(grid.Start, grid.Goal)));

        while (open.TryDequeue(out var current, out _))
        {
            if (!closed.Add(current)) continue; // stale queue entry
            visited.Add(current);
            if (current == grid.Goal) return true;

            foreach (var n in grid.Neighbors(current))
            {
                if (closed.Contains(n)) continue;
                var g = best[current] + grid.CostToEnter(n);
                if (best.TryGetValue(n, out var known) && known <= g) continue;
                best[n] = g;
                parents[n] = current;
                var h = Manhattan(n, grid.Goal);
                // Tie-break on h so searches head toward the goal on equal f
                open.Enqueue(n, (Priority(g, h), h));
            }
        }
        return false;
    }
}

public sealed class Dijkstra : PriorityPathfinder
{
    public override string Name => "Dijkstra";
    public override bool GuaranteesShortestPath => true;
    protected override int Priority(int costSoFar, int heuristic) => costSoFar;
}

public sealed class AStar : PriorityPathfinder
{
    public override string Name => "A* (Manhattan)";
    public override bool GuaranteesShortestPath => true;
    protected override int Priority(int costSoFar, int heuristic) => costSoFar + heuristic;
}

public sealed class GreedyBestFirst : PriorityPathfinder
{
    public override string Name => "Greedy Best-First";
    public override bool GuaranteesShortestPath => false;
    protected override int Priority(int costSoFar, int heuristic) => heuristic;
}

public static class Pathfinders
{
    public static IReadOnlyList<IPathfinder> All { get; } =
        [new AStar(), new Dijkstra(), new BreadthFirstSearch(), new GreedyBestFirst(), new DepthFirstSearch()];

    public static IPathfinder ByName(string name) =>
        All.FirstOrDefault(p => p.Name == name) ?? throw new ArgumentException($"Unknown algorithm '{name}'.");
}
