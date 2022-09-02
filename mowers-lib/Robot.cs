namespace Mowers;

public sealed class RandomRobot : IRobot
{
    public int Column { get; private set; }
    public int Row { get; private set; }

    public void Reset(int initialColumn, int initialRow)
    {
        this.Column = initialColumn;
        this.Row = initialRow;
    }

    public void Move(Cell above, Cell right, Cell below, Cell left)
    {
        const byte UP = 1, RIGHT = 2, DOWN = 3, LEFT = 4;

        byte count = 0;
        Span<byte> directions = stackalloc byte[4];
        if (above is Cell.Unmown or Cell.Mown)
        {
            directions[count++] = UP;
        }
        if (right is Cell.Unmown or Cell.Mown)
        {
            directions[count++] = RIGHT;
        }
        if (below is Cell.Unmown or Cell.Mown)
        {
            directions[count++] = DOWN;
        }
        if (left is Cell.Unmown or Cell.Mown)
        {
            directions[count++] = LEFT;
        }

        if (count is 0)
        {
            // we are blocked on all sides
            return;
        }

        byte dir = directions[..count][Random.Shared.Next(count)];

        switch (dir)
        {
            case UP:
                this.Row -= 1;
                return;
            case RIGHT:
                this.Column += 1;
                return;
            case DOWN:
                this.Row += 1;
                return;
            case LEFT:
                this.Column -= 1;
                return;
            default:
                throw new InvalidOperationException($"impossible case, direction: {dir}");
        }
    }
}
