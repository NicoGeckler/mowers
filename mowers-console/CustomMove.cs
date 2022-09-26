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
        LastDirection,
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
        ref object? mapRef = ref CollectionsMarshal.GetValueRefOrAddDefault(data, (int)Key.Map, out mapExisted);
        if (mapExisted is false)
        {
            mapRef = Map.GetClearedCached(42 * 27);
        }

        Debug.Assert(mapRef is not null);
        Map map = (Map)mapRef;

        Debug.Assert(map is not null);
        Debug.Assert(data[(int)Key.Map] is Map);

        map[row, col] = Cell.Mown;
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

        _ = data.TryGetAs((int)Key.LastDirection, out Direction? value);

        Direction next = value switch
        {
            Direction.Up when cells.Above is Cell.Unmown => Direction.Up,
            Direction.Right when cells.Right is Cell.Unmown => Direction.Right,
            Direction.Down when cells.Below is Cell.Unmown => Direction.Down,
            Direction.Left when cells.Left is Cell.Unmown => Direction.Left,
            _ => cells.TowardsAny(Cell.Unmown),
        };
        if (next.Move(ref row, ref col))
        {
            data[(int)Key.LastDirection] = next;
            _ = data.Remove((int)Key.MapPath);
            return;
        }

        bool pathExisted;
        ref object? pathRef = ref CollectionsMarshal.GetValueRefOrAddDefault(data, (int)Key.MapPath, out pathExisted);
        if (pathExisted is false)
        {
            pathRef = map.GetPathToClosestFrom(row, col);
        }

        Debug.Assert(pathRef is not null);
        Stack<Direction> path = (Stack<Direction>)pathRef;

        Debug.Assert(path is not null);
        Debug.Assert(data[(int)Key.MapPath] is Stack<Direction>);
        Debug.Assert(path.Count > 0);

        next = path.Pop();
        _ = next.Move(ref row, ref col);
        data[(int)Key.LastDirection] = next;
        if (path.Count is 0)
        {
            _ = data.Remove((int)Key.MapPath);
        }
    }
    private sealed class Map
    {
        private static Map? cached;

        private readonly Dictionary<(int row, int col), Cell?> map;
        private readonly Stack<Direction> path = new();
        private readonly PriorityQueue<(int row, int col), int> pathCoords = new();
        private readonly Dictionary<(int row, int col), (int row, int col, int distance, Cell? type)?> checkedCoords = new();

        private Map(int capacity)
        {
            this.map = new(capacity);
        }

        public static Map GetClearedCached(int capacity)
        {
            // if (cached is null)
            // {
            //     cached = new(capacity);
            // }
            // cached.map.Clear();
            // _ = cached.map.EnsureCapacity(capacity);
            // return cached;
            return new(capacity);
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

        public Stack<Direction> GetPathToClosestFrom(int initialRow, int initialColumn)
        {
            Dictionary<(int row, int col), (int row, int col, int distance, Cell? type)?> done = this.checkedCoords;
            done.Clear();
            done.Add((initialRow, initialColumn), null);

            PriorityQueue<(int row, int col), int> todo = this.pathCoords;
            todo.Clear();
            todo.Enqueue((initialRow, initialColumn), 0);

            Span<(int row, int col)> neighbors = stackalloc (int, int)[4];
            while (todo.Count > 0)
            {
                (int row, int col) coords;
                int distance;
                _ = todo.TryDequeue(out coords, out distance);

                neighbors[0] = (coords.row + 1, coords.col);
                neighbors[1] = (coords.row - 1, coords.col);
                neighbors[2] = (coords.row, coords.col + 1);
                neighbors[3] = (coords.row, coords.col - 1);

                foreach ((int row, int col) neighbor in neighbors)
                {
                    Cell? type = this[neighbor.row, neighbor.col];

                    if (type is null or Cell.Unmown)
                    {
                        Stack<Direction> path = this.path;
                        path.Clear();

                        static Direction fromCoords(int startRow, int startCol, int endRow, int endCol) => endRow == startRow + 1
                            ? Direction.Down
                            : endRow == startRow - 1
                            ? Direction.Up
                            : endCol == startCol + 1
                            ? Direction.Right
                            : endCol == startCol - 1
                            ? Direction.Left
                            : Direction.None;

                        int endRow;
                        int endCol;
                        int startRow;
                        int startCol;
                        if (type is null)
                        {
                            // skip found cell incase its a forbidden one
                            endRow = coords.row;
                            endCol = coords.col;

                            (int row, int col, int distance, Cell? type)? prev = done[(coords.row, coords.col)];
                            if (prev is null)
                            {
                                path.Push(Direction.None);
                                return path;
                            }
                            startRow = prev.Value.row;
                            startCol = prev.Value.col;
                        }
                        else
                        {
                            endRow = neighbor.row;
                            endCol = neighbor.col;
                            startRow = coords.row;
                            startCol = coords.col;
                        }

                        while (true)
                        {
                            path.Push(fromCoords(startRow, startCol, endRow, endCol));

                            endRow = startRow;
                            endCol = startCol;

                            (int row, int col, int distance, Cell? type)? prev = done[(startRow, startCol)];
                            if (prev is null)
                            {
                                return path;
                            }

                            startRow = prev.Value.row;
                            startCol = prev.Value.col;
                        }
                    }
                    else if (type.Value is Cell.Forbidden or Cell.ForbiddenMown or Cell.ForbiddenRobot)
                    {
                        continue;
                    }

                    if (done.TryGetValue(neighbor, out (int row, int col, int distance, Cell? type)? value))
                    {
                        if (value?.distance > distance + 1)
                        {
                            done[neighbor] = (coords.row, coords.col, distance + 1, type);
                        }
                    }
                    else
                    {
                        todo.Enqueue(neighbor, distance + 1);
                        done[neighbor] = (coords.row, coords.col, distance + 1, type);
                    }

                }
            }

            this.path.Clear();
            this.path.Push(Direction.None);
            return path;
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
