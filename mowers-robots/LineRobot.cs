namespace Mowers.Robots;

public sealed class LineRobot : IRobot
{
    private const double KEEP_WALKING_FORWARD_CHANCE = 0.95;

    public int Column { get; private set; }

    public int Row { get; private set; }

    private Direction lastDir = Direction.None;

    private bool TryMoveToward(Cell target, Neighbors neighbors)
    {
        int count = 0;
        Span<Direction> available = stackalloc Direction[4];

        if (neighbors.Above == target)
        {
            available[count++] = Direction.Up;
        }
        if (neighbors.Left == target)
        {
            available[count++] = Direction.Left;
        }
        if (neighbors.Below == target)
        {
            available[count++] = Direction.Down;
        }
        if (neighbors.Right == target)
        {
            available[count++] = Direction.Right;
        }

        if (count > 0)
        {
            this.lastDir = available[..count].PickRandom();
            MoveInLine();
            return true;
        }
        return false;
    }

    private void MoveInLine()
    {
        switch (this.lastDir)
        {
            case Direction.None:
                return;
            case Direction.Up:
                this.Row -= 1;
                return;
            case Direction.Right:
                this.Column += 1;
                return;
            case Direction.Down:
                this.Row += 1;
                return;
            case Direction.Left:
                this.Column -= 1;
                return;
            default:
                throw new InvalidOperationException($"impossible case, direction: {this.lastDir}");
        }
    }

    public void Move(Cell above, Cell right, Cell below, Cell left)
    {
        switch (this.lastDir)
        {
            case Direction.Up when above is Cell.Unmown:
                this.Row -= 1;
                return;
            case Direction.Left when left is Cell.Unmown:
                this.Column -= 1;
                return;
            case Direction.Down when below is Cell.Unmown:
                this.Row += 1;
                return;
            case Direction.Right when right is Cell.Unmown:
                this.Column += 1;
                return;
            case Direction.None:
            default:
                {
                    Neighbors neighbors = new(above, right, below, left);

                    if (TryMoveToward(Cell.Unmown, neighbors))
                    {
                        return;
                    }

                    if (this.lastDir is not Direction.None
                        && this.lastDir.IsWalkable(neighbors)
                        && Random.Shared.NextDouble() <= KEEP_WALKING_FORWARD_CHANCE)
                    {
                        MoveInLine();
                        return;
                    }

                    if (TryMoveToward(Cell.Mown, neighbors))
                    {
                        return;
                    }

                    this.lastDir = Direction.None;
                    return;
                }
        }
    }

    public void Reset(int initialColumn, int initialRow)
    {
        this.Column = initialColumn;
        this.Row = initialRow;
        this.lastDir = Direction.None;
    }
}
