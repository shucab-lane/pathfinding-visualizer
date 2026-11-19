# Pathfinding Visualizer

[![CI/CD](https://github.com/shucab-lane/pathfinding-visualizer/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/shucab-lane/pathfinding-visualizer/actions/workflows/ci-cd.yml)

Graph search algorithms written in **C# / .NET 10** and running in the browser with **Blazor WebAssembly**. Draw walls and weighted terrain, then watch A*, Dijkstra, breadth-first, depth-first and greedy best-first search explore the grid, or run them all at once and compare.

**Live demo:** https://shucab-lane.github.io/pathfinding-visualizer/

## Features

- **5 algorithms** behind one `IPathfinder` interface: A* (Manhattan heuristic), Dijkstra, BFS, DFS and greedy best-first
- **Weighted terrain**: weighted cells cost 5 to enter, which shows why BFS isn't enough and why Dijkstra and A* are needed
- **Maze generation** with a randomized depth-first (recursive backtracker) algorithm, plus random wall and weight scatter
- **Compare mode** runs every algorithm on the same grid and ranks them by path cost, cells explored and run time
- Interactive editing: draw walls and weights, erase, move the start and goal, and animate at adjustable speeds
- **25 xUnit tests**, including property-style checks over 80 random grids (A* always matches Dijkstra's cost and never explores more cells; no algorithm ever beats Dijkstra)
- **CI/CD**: GitHub Actions runs the tests on every push, publishes the WebAssembly app and deploys it to GitHub Pages

## Design

```
Pathfinding.Core   (class library, no UI dependencies)
  Grid              cells, costs, neighbours, ASCII map parser
  IPathfinder       FindPath(Grid) → SearchResult(visited order, path, cost, elapsed)
  PathfinderBase    template method: timing, parent links, path reconstruction
  PriorityPathfinder  one best-first search; subclasses only choose the priority:
                      Dijkstra = g,  A* = g + h,  Greedy = h
  BreadthFirstSearch, DepthFirstSearch
  RecursiveBacktracker, RandomScatter   (IMazeGenerator)

Pathfinding.Web    (Blazor WebAssembly UI, references Core)
Pathfinding.Tests  (xUnit, references Core)
```

- **Dijkstra, A\* and greedy best-first share one implementation.** They differ only in the priority function, so `PriorityPathfinder` implements the search once with .NET's `PriorityQueue<TElement, TPriority>` and each subclass overrides a one-line `Priority(g, h)` (the template method pattern).
- **Lazy deletion:** rather than a decrease-key operation, improved entries are pushed again and stale ones are skipped when dequeued (the `closed` set).
- **Tie-breaking on h** makes A* head toward the goal when f-scores are equal, which cuts the number of explored cells sharply on open grids.
- The core library has no UI dependencies, so the same code runs in the browser, in tests or in a console app.

## Running locally

Requires the .NET 10 SDK.

```bash
dotnet test                                   # run the test suite
dotnet run --project src/Pathfinding.Web      # start the app locally
```

## Project structure

```
src/
  Pathfinding.Core/   Grid.cs, Pathfinders.cs, MazeGenerators.cs
  Pathfinding.Web/    Pages/Home.razor (UI), wwwroot/css/app.css
tests/
  Pathfinding.Tests/  PathfinderTests.cs
.github/workflows/ci-cd.yml
```
