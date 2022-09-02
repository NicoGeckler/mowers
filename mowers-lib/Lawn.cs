namespace Mowers;

public class Lawn
{
    private const int DEFAULT_WIDTH = 40;
    private const int DEFAULT_HEIGHT = 25;
    private const int DEFAULT_BLOCK_COUNT = 3;
    private const int MAX_RANDOM_LOOP_RETRIES = 10_000;

    private readonly Cell[,] cells;
    private readonly Block[] blocks;
    private readonly List<IRobot> robots;
    private int activeRobots;

    public int Width => this.cells.GetLength(1);
    public int Height => this.cells.GetLength(0);
    public IEnumerable<(int row, int col)> Robots => this.robots.Take(this.activeRobots).Select(r => (r.Row, r.Column));

    public Lawn(int width = DEFAULT_WIDTH, int height = DEFAULT_HEIGHT, int blockCount = DEFAULT_BLOCK_COUNT)
    {
        if (width < 0)
        {
            width = 0;
        }
        if (height < 0)
        {
            height = 0;
        }
        if (blockCount < 0)
        {
            blockCount = 0;
        }
        this.cells = new Cell[width, height];
        this.robots = new();
        this.blocks = new Block[blockCount];
    }

    public bool AddRobot(IRobot robot)
    {
        if (this.robots.Count + 1 > this.MaxRobotCount)
        {
            return false;
        }

        this.robots.Add(robot);
        return true;
    }

    public int InitLawn()
    {
        for (int idx = 0; idx < this.blocks.Length; idx++)
        {
            int left = Random.Shared.Next(1, this.Width - 2);
            int right = Random.Shared.Next(left, this.Width - 1);

            int top = Random.Shared.Next(1, this.Height - 2);
            int bottom = Random.Shared.Next(top, this.Height - 1);

            this.blocks[idx] = new(left, top, right, bottom);
        }

        for (int row = 0; row < this.Height; row++)
        {
            for (int col = 0; col < this.Width; col++)
            {
                this.cells[row, col] = this.IsEdge(col, row) || this.IsInBlock(col, row)
                    ? Cell.Forbidden
                    : Cell.Unmown
                ;
            }
        }

        int robotsPlaced = 0;
        int tries = 0;
        foreach (IRobot robot in this.robots)
        {
            int col;
            int row;
            do
            {
                if (++tries is >= MAX_RANDOM_LOOP_RETRIES)
                {
                    goto DONE_PLACING_ROBOTS; // break outer loop
                }
                col = Random.Shared.Next(1, this.Width - 1);
                row = Random.Shared.Next(1, this.Height - 1);
            } while (this.cells[row, col] is Cell.Forbidden);

            robot.Reset(col, row);
            robotsPlaced++;
        }
        DONE_PLACING_ROBOTS:
        ValidateRobots();

        this.activeRobots = robotsPlaced;
        return robotsPlaced;
    }

    public void Step()
    {
        foreach (IRobot robot in this.robots)
        {
            // TODO
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
                    == this.robots.Any(robot => robot.Row == row && robot.Column == col)
                );
            }
        }
    }

    private bool IsInBlock(int col, int row)
    {
        foreach (Block block in this.blocks)
        {
            if (col > block.Left && col < block.Right && row > block.Top && row < block.Bottom)
            {
                return false;
            }
        }
        return true;
    }
    private bool IsEdge(int col, int row) => row is 0 || col is 0 || row == this.Width - 1 || col == this.Height - 1;
    private int MaxRobotCount => this.cells.GetLength(0) * this.cells.GetLength(1) / 8;

    private record struct Block(int Left, int Top, int Right, int Bottom);
}
