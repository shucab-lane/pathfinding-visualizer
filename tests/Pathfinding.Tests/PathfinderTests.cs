using Pathfinding.Core;

namespace Pathfinding.Tests;

public class PathfinderTests
{
    public static TheoryData<string> AllAlgorithms() => new(Pathfinders.All.Select(p => p.Name));
    public static TheoryData<string> OptimalAlgorithms() =>
        new(Pathfinders.All.Where(p => p.GuaranteesShortestPath).Select(p => p.Name));

    private const string OpenMap = """
        S.....
        ......
        .....G
        """;

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void Every_algorithm_finds_a_valid_path(string name)
    {
        var grid = Grid.Parse(OpenMap);
        var result = Pathfinders.ByName(name).FindPath(grid);

        Assert.True(result.Found);
        Assert.Equal(grid.Start, result.Path[0]);
        Assert.Equal(grid.Goal, result.Path[^1]);
        // Each step moves to an adjacent, passable cell
        for (var i = 1; i < result.Path.Count; i++)
        {
            var (a, b) = (result.Path[i - 1], result.Path[i]);
            Assert.Equal(1, Math.Abs(a.Row - b.Row) + Math.Abs(a.Col - b.Col));
            Assert.True(grid.IsPassable(b));
        }
    }

    [Theory]
    [MemberData(nameof(AllAlgorithms))]
    public void Reports_no_path_when_the_goal_is_walled_off(string name)
    {
        var grid = Grid.Parse("""
            S.#...
            ..#...
            ..#..G
            """);
        var result = Pathfinders.ByName(name).FindPath(grid);
        Assert.False(result.Found);
        Assert.Empty(result.Path);
        Assert.NotEmpty(result.Visited);
    }

    [Fact]
    public void Bfs_finds_the_fewest_steps_on_an_unweighted_grid()
    {
        var result = new BreadthFirstSearch().FindPath(Grid.Parse(OpenMap));
        Assert.Equal(7, result.Cost); // 2 down + 5 right
    }

    [Theory]
    [MemberData(nameof(OptimalAlgorithms))]
    public void Optimal_algorithms_route_around_expensive_cells(string name)
    {
        // Going straight costs 5 per '~'; the detour through row 0 is cheaper.
        var grid = Grid.Parse("""
            ......
            S~~~~G
            """);
        var result = Pathfinders.ByName(name).FindPath(grid);
        Assert.Equal(7, result.Cost);
        Assert.DoesNotContain(result.Path, p => grid[p] == CellType.Weight);
    }

    [Fact]
    public void Bfs_ignores_weights_and_pays_for_it()
    {
        var grid = Grid.Parse("""
            ......
            S~~~~G
            """);
        Assert.Equal(21, new BreadthFirstSearch().FindPath(grid).Cost); // 4 weights * 5 + goal
    }

    [Fact]
    public void AStar_matches_Dijkstra_cost_but_explores_fewer_cells()
    {
        var random = new Random(7);
        for (var trial = 0; trial < 40; trial++)
        {
            var grid = new Grid(25, 50);
            new RandomScatter(0.25, 0.15).Generate(grid, random);
            var dijkstra = new Dijkstra().FindPath(grid);
            var astar = new AStar().FindPath(grid);

            Assert.Equal(dijkstra.Found, astar.Found);
            Assert.Equal(dijkstra.Cost, astar.Cost);
            if (dijkstra.Found) Assert.True(astar.Visited.Count <= dijkstra.Visited.Count);
        }
    }

    [Fact]
    public void Dijkstra_is_never_beaten_by_any_other_algorithm()
    {
        var random = new Random(11);
        for (var trial = 0; trial < 40; trial++)
        {
            var grid = new Grid(20, 40);
            new RandomScatter(0.2, 0.2).Generate(grid, random);
            var best = new Dijkstra().FindPath(grid);
            foreach (var algo in Pathfinders.All.Where(a => a.FindPath(grid).Found))
                Assert.True(algo.FindPath(grid).Cost >= best.Cost, $"{algo.Name} beat Dijkstra");
        }
    }
}

public class GridTests
{
    [Fact]
    public void Parse_reads_walls_weights_start_and_goal()
    {
        var grid = Grid.Parse("""
            S#~
            ..G
            """);
        Assert.Equal((2, 3), (grid.Rows, grid.Cols));
        Assert.Equal(new Point(0, 0), grid.Start);
        Assert.Equal(new Point(1, 2), grid.Goal);
        Assert.Equal(CellType.Wall, grid[new Point(0, 1)]);
        Assert.Equal(Grid.WeightCost, grid.CostToEnter(new Point(0, 2)));
    }

    [Fact]
    public void Start_and_goal_cannot_become_walls()
    {
        var grid = new Grid(5, 5);
        grid[grid.Start] = CellType.Wall;
        grid[grid.Goal] = CellType.Wall;
        Assert.True(grid.IsPassable(grid.Start));
        Assert.True(grid.IsPassable(grid.Goal));
    }

    [Fact]
    public void Neighbors_skip_walls_and_edges()
    {
        var grid = Grid.Parse("""
            S#
            ..
            """);
        Assert.Equal([new Point(1, 0)], grid.Neighbors(new Point(0, 0)).ToList());
    }
}

public class MazeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Generated_mazes_are_always_solvable(int seed)
    {
        var grid = new Grid(25, 51);
        new RecursiveBacktracker().Generate(grid, new Random(seed));
        Assert.True(new BreadthFirstSearch().FindPath(grid).Found);
    }

    [Fact]
    public void Backtracker_produces_a_perfect_maze_with_walls()
    {
        var grid = new Grid(21, 41);
        new RecursiveBacktracker().Generate(grid, new Random(1));
        var walls = grid.AllPoints().Count(p => grid[p] == CellType.Wall);
        Assert.InRange(walls, grid.Rows * grid.Cols / 3, grid.Rows * grid.Cols * 2 / 3);
    }
}
