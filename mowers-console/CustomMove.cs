using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CustomMower;

internal static class Customize
{
    private enum Key : int
    {
        Strat,
        Map,
        MapPath,
    }
    private static readonly List<(MoveAction strategy, int weight)> strategies = new()
    {
        (Mapped, 100),
        (Malicious, 1),
        (Nothing, 1),
    };

    private static bool Move(this Direction dir, ref int row, ref int col)
    {
        switch (dir)
        {
            case Direction.Up:
                row--;
                return true;
            case Direction.Left:
                col--;
                return true;
            case Direction.Down:
                row++;
                return true;
            case Direction.Right:
                col++;
                return true;
            case Direction.None:
            default:
                return false;
        }
    }

    internal static void RandomizeStrategy(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells)
    {
        if (data.TryGetAs((int)Key.Strat, out MoveAction? storedStrat))
        {
            storedStrat(data, ref row, ref col, cells);
        }
        else
        {
            int totalWeight = strategies.Sum(s => s.weight);
            int roll = Random.Shared.Next(totalWeight) + 1;

            for (int idx = 0; idx < strategies.Count; idx++)
            {
                roll -= strategies[idx].weight;
                if (roll <= 0)
                {
                    (MoveAction selectedStrat, _) = strategies[idx];
                    data[(int)Key.Strat] = selectedStrat;
                    selectedStrat(data, ref row, ref col, cells);
                    break;
                }
            }

            Debug.Assert(data[(int)Key.Strat] is MoveAction);
        }
    }

    private static void Mapped(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells)
    {
        bool mapExisted;
        Map? map = (Map?)CollectionsMarshal.GetValueRefOrAddDefault(data, (int)Key.Map, out mapExisted);
        if (mapExisted is false)
        {
            map = Map.GetClearedCached(42 * 27);
        }

        Debug.Assert(map is not null);
        Debug.Assert(data[(int)Key.Map] is Map);

        map[row - 1, col] = cells.Above;
        if (cells.Above.IsWalkable())
        {
            map.Add(null, row - 2, col);
            map.Add(null, row - 1, col + 1);
            map.Add(null, row - 1, col - 1);
        }
        map[row, col + 1] = cells.Right;
        if (cells.Right.IsWalkable())
        {
            map.Add(null, row, col + 2);
            map.Add(null, row + 1, col + 1);
            map.Add(null, row - 1, col + 1);
        }
        map[row + 1, col] = cells.Below;
        if (cells.Below.IsWalkable())
        {
            map.Add(null, row + 2, col);
            map.Add(null, row + 1, col + 1);
            map.Add(null, row + 1, col - 1);
        }
        map[row, col - 1] = cells.Left;
        if (cells.Left.IsWalkable())
        {
            map.Add(null, row, col - 2);
            map.Add(null, row - 1, col + 1);
            map.Add(null, row - 1, col - 1);
        }

        if (cells.TowardsAny(Cell.Unmown).Move(ref row, ref col))
        {
            _ = data.Remove((int)Key.MapPath);
            return;
        }

        bool pathExisted;
        Queue<Direction>? path = (Queue<Direction>?)CollectionsMarshal.GetValueRefOrAddDefault(data, (int)Key.MapPath, out pathExisted);
        if (pathExisted is false)
        {
            path = map.FindClosestFrom(row, col);
        }

        Debug.Assert(path is not null);
        Debug.Assert(data[(int)Key.MapPath] is Queue<Direction>);
        Debug.Assert(path.Count > 0);

        _ = path.Dequeue().Move(ref row, ref col);
    }
    private sealed class Map
    {
        private static Map? cached;

        private readonly Dictionary<(int row, int col), Cell?> map;
        private readonly Queue<Direction> path = new();
        private readonly PriorityQueue<(int row, int col), int> pathCoords = new();

        private Map(int capacity)
        {
            this.map = new(capacity);
        }

        public static Map GetClearedCached(int capacity)
        {
            if (cached is null)
            {
                cached = new(capacity);
            }
            cached.map.Clear();
            _ = cached.map.EnsureCapacity(capacity);
            return cached;
        }

        public Cell? this[int row, int column]
        {
            get => this.map.TryGetValue((row, column), out Cell? value)
                ? value
                : null;

            set => CollectionsMarshal.GetValueRefOrAddDefault(this.map, (row, column), out _) = value;
        }

        public void Add(Cell? value, int row, int column)
        {
            _ = this.map.TryAdd((row, column), value);
        }

        public Queue<Direction> FindClosestFrom(int row, int column)
        {
            #region Helpers

            static int distance(int startRow, int startCol, int endRow, int endCol) => Math.Abs(startRow - endRow) + Math.Abs(startCol - endCol);

            #endregion Helpers

            this.pathCoords.Clear();

            // TODO: find path to closest unmown or unexplored (null) cell
        }
    }

    private static void Malicious(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells)
    {
        if (Random.Shared.NextDouble() >= 0.5)
        {
            row++;
        }
        else
        {
            col++;
        }
    }

    private static void Nothing(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells)
    { }
}
