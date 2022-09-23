namespace Mowers.Robots;

public sealed class CustomizableRobot : IRobot
{
    public int Column => this.column;
    public int Row => this.row;
    public void Move(Cell above, Cell right, Cell below, Cell left)
    {
        this.MoveAction?.Invoke(
            this.data,
            ref this.row,
            ref this.column,
            new(above, right, below, left)
        );
    }
    public void Reset(int initialColumn, int initialRow)
    {
        this.data.Clear();
        this.column = initialColumn;
        this.row = initialRow;
    }

    public CustomMoveAction? MoveAction { get; set; }

    private int column;
    private int row;
    private readonly Dictionary<int, object> data = new();

    public delegate void CustomMoveAction(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells);
}