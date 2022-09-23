namespace Mowers.Robots;

public enum Direction : byte
{
    None,
    Up,
    Left,
    Down,
    Right
}

public readonly record struct Neighbors(Cell Above, Cell Right, Cell Below, Cell Left);

public static class RobotHelpers
{
    public static bool IsWalkable(this Direction dir, Neighbors neighbors) => dir switch
    {
        Direction.None => true,
        Direction.Up => neighbors.Above is Cell.Mown or Cell.Unmown,
        Direction.Right => neighbors.Right is Cell.Mown or Cell.Unmown,
        Direction.Down => neighbors.Below is Cell.Mown or Cell.Unmown,
        Direction.Left => neighbors.Left is Cell.Mown or Cell.Unmown,
        _ => throw new InvalidOperationException(),
    };

    public static bool IsWalkable(this Cell cell) => cell is Cell.Mown or Cell.Unmown;

    public static Direction PickRandom(this ReadOnlySpan<Direction> availableDirections)
    {
        return availableDirections[Random.Shared.Next(availableDirections.Length)];
    }
    public static Direction PickRandom(this Span<Direction> availableDirections)
    {
        return availableDirections[Random.Shared.Next(availableDirections.Length)];
    }

    public static Direction TowardsAny(this Neighbors neighbors, Cell target)
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

        return count > 0
            ? available[..count].PickRandom()
            : Direction.None;
    }
}