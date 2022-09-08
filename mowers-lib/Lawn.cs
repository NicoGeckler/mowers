using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mowers;

public sealed class Lawn : ILawn
{
    private const int DEFAULT_WIDTH = 40;
    private const int DEFAULT_HEIGHT = 25;
    private const int DEFAULT_BLOCK_COUNT = 3;
    private const int MAX_RANDOM_LOOP_RETRIES = 10_000;

    private readonly Cell[,] cells;
    private readonly List<Robot> robots;

    public int Width => this.cells.GetLength(1);
    public int Height => this.cells.GetLength(0);
    public IEnumerable<(int row, int col)> Robots => this.robots.Where(r => r.Active).Select(r => (r.Logic.Row, r.Logic.Column));
    public RowEnumerator CellRows => new(this.cells);

    public Lawn(int width = DEFAULT_WIDTH, int height = DEFAULT_HEIGHT)
    {
        if (width < 0)
        {
            width = 0;
        }
        if (height < 0)
        {
            height = 0;
        }
        this.cells = new Cell[height, width];
        this.robots = new();
    }

    public bool AddRobot(IRobot robot)
    {
        if (this.robots.Count + 1 > this.MaxRobotCount)
        {
            return false;
        }

        if (this.robots.Any(r => r.Logic == robot))
        {
            return false;
        }

        this.robots.Add(new(robot, false));
        return true;
    }

    public bool RemoveRobot(IRobot robot)
    {
        return this.robots.RemoveAll(r => r.Logic == robot) > 0;
    }

    public int InitLawn(int blockCount = DEFAULT_BLOCK_COUNT)
    {
        if (blockCount < 0)
        {
            blockCount = 0;
        }
        Span<Block> blocks = blockCount < 64 ? stackalloc Block[blockCount] : new Block[blockCount];
        foreach (ref Block block in blocks)
        {
            int left = Random.Shared.Next(1, this.Width - 2);
            int right = Random.Shared.Next(left, this.Width - 1);

            int top = Random.Shared.Next(1, this.Height - 2);
            int bottom = Random.Shared.Next(top, this.Height - 1);

            block = new(left, top, right, bottom);
        }

        for (int row = 0; row < this.Height; row++)
        {
            for (int col = 0; col < this.Width; col++)
            {
                this.cells[row, col] = this.IsEdge(col, row) || Lawn.IsInBlock(col, row, blocks)
                    ? Cell.Forbidden
                    : Cell.Unmown
                ;
            }
        }

        int robotsPlaced = 0;
        int tries = 0;
        foreach (ref Robot robot in CollectionsMarshal.AsSpan(this.robots)) // NOTE: DO NOT MODIFY `this.robots` IN THIS LOOP
        {
            (int row, int col)? pos = InitRobot(ref tries);
            if (pos.HasValue)
            {
                this.cells[pos.Value.row, pos.Value.col] = Cell.Robot;
                robot.Logic.Reset(pos.Value.col, pos.Value.row);
                robot.Active = true;
                robotsPlaced++;
            }
            else
            {
                robot.Active = false;
            }
        }
        ValidateRobots();

        return robotsPlaced;
    }

    public void MoveRobots()
    {
        foreach (Robot robot in this.robots)
        {
            if (robot.Active is false)
            {
                continue;
            }

            // FIX: be robust against malicious robots (that set col/row out of bounds)

            ref Cell cell = ref this.cells[robot.Logic.Row, robot.Logic.Column];
            cell = cell is Cell.ForbiddenRobot ? Cell.ForbiddenMown : Cell.Mown;

            robot.Logic.Move(
                above: Get(robot.Logic.Row - 1, robot.Logic.Column).NullableValue ?? Cell.Forbidden,
                right: Get(robot.Logic.Row, robot.Logic.Column + 1).NullableValue ?? Cell.Forbidden,
                below: Get(robot.Logic.Row + 1, robot.Logic.Column).NullableValue ?? Cell.Forbidden,
                left: Get(robot.Logic.Row, robot.Logic.Column - 1).NullableValue ?? Cell.Forbidden
            );

            cell = ref this.cells[robot.Logic.Row, robot.Logic.Column];
            cell = cell is Cell.Forbidden ? Cell.ForbiddenRobot : Cell.Robot;
        }
        ValidateRobots();
    }

    [Conditional("DEBUG")]
    private void ValidateRobots()
    {
        for (int row = 0; row < this.Height; row++)
        {
            for (int col = 0; col < this.Width; col++)
            {
                Debug.Assert(
                    (this.cells[row, col] is Cell.Robot or Cell.ForbiddenRobot)
                    == this.robots.Any(robot => robot.Logic.Row == row && robot.Logic.Column == col)
                );
            }
        }
    }

    private static bool IsInBlock(int col, int row, ReadOnlySpan<Block> blocks)
    {
        foreach (Block block in blocks)
        {
            if (col >= block.Left && col <= block.Right && row >= block.Top && row <= block.Bottom)
            {
                return true;
            }
        }
        return false;
    }

    private (int row, int col)? InitRobot(ref int tries)
    {
        if (tries >= MAX_RANDOM_LOOP_RETRIES)
        {
            return null;
        }
        int col;
        int row;
        do
        {
            if (++tries >= MAX_RANDOM_LOOP_RETRIES)
            {
                return null;
            }
            col = Random.Shared.Next(1, this.Width - 1);
            row = Random.Shared.Next(1, this.Height - 1);
        }
        while (this.cells[row, col] is Cell.Forbidden or Cell.Robot);

        return (row, col);
    }

    private bool IsEdge(int col, int row) => row is 0 || col is 0 || row == this.Height - 1 || col == this.Width - 1;
    private int MaxRobotCount => this.cells.GetLength(0) * this.cells.GetLength(1) / 8;

    private NullableRef<Cell> Get(int row, int col)
    {
        return (uint)row < (uint)this.Height && (uint)col < (uint)this.Width
            ? new(ref this.cells[row, col])
            : default;
    }

    private struct Robot
    {
        public IRobot Logic { get; }
        public bool Active { get; set; }

        public Robot(IRobot robot, bool active)
        {
            this.Logic = robot;
            this.Active = active;
        }

        public readonly bool Equals(Robot other) => this.Logic == other.Logic;
        public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is Robot that && this.Equals(that);
        public static bool operator ==(Robot a, Robot b) => a.Equals(b);
        public static bool operator !=(Robot a, Robot b) => !(a == b);
        public readonly override int GetHashCode() => this.Logic.GetHashCode();
    }

    private record struct Block(int Left, int Top, int Right, int Bottom);
}
