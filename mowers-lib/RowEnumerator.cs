using System.Runtime.InteropServices;

namespace Mowers;

public ref struct RowEnumerator
{
    private readonly Cell[,] cells;
    private int row;

    internal RowEnumerator(Cell[,] target)
    {
        this.cells = target;
        this.row = -1;
    }

    public ReadOnlySpan<Cell> Current => MemoryMarshal.CreateReadOnlySpan(ref this.cells[this.row, 0], this.cells.GetLength(1));

    public bool MoveNext()
    {
        unchecked
        {
            this.row++;
            return (uint)this.row < (uint)this.cells.GetLength(0);
        }
    }

    public RowEnumerator GetEnumerator() => this;
}
