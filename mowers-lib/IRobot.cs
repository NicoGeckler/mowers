namespace Mowers;

public interface IRobot
{
    int Column { get; }
    int Row { get; }

    void Move(Cell above, Cell right, Cell below, Cell left);

    void Reset(int initialColumn, int initialRow);
}