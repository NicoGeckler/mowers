namespace Mowers.Robots;

public sealed class LineRobot : IRobot
{
    public int Column { get; private set; }

    public int Row { get; private set; }

    private enum Direction : byte
    {
        None,
        Up,
        Left,
        Down,
        Right
    }
    private Direction lastDir = Direction.None;

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
                    int count = 0;
                    Span<Direction> available = stackalloc Direction[4];

                    // try only unmown tiles first
                    if (above is Cell.Unmown)
                    {
                        available[count++] = Direction.Up;
                    }
                    if (left is Cell.Unmown)
                    {
                        available[count++] = Direction.Left;
                    }
                    if (below is Cell.Unmown)
                    {
                        available[count++] = Direction.Down;
                    }
                    if (right is Cell.Unmown)
                    {
                        available[count++] = Direction.Right;
                    }

                    // no unmown candidates, allow mown tiles
                    if (count is 0)
                    {
                        if (above is Cell.Mown)
                        {
                            available[count++] = Direction.Up;
                        }
                        if (left is Cell.Mown)
                        {
                            available[count++] = Direction.Left;
                        }
                        if (below is Cell.Mown)
                        {
                            available[count++] = Direction.Down;
                        }
                        if (right is Cell.Mown)
                        {
                            available[count++] = Direction.Right;
                        }
                    }
                    
                    if (count is 0)
                    {
                        // we're blocked, there's nothing more to do
                        this.lastDir = Direction.None;
                        return;
                    }

                    this.lastDir = available[..count][Random.Shared.Next(count)];
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
        }
    }

    public void Reset(int initialColumn, int initialRow)
    {
        this.Column = initialColumn;
        this.Row = initialRow;
        this.lastDir = Direction.None;
    }
}