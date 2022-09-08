namespace Mowers;

public interface ILawn
{
    int Width { get; }
    int Height { get; }
    
    IEnumerable<(int row, int col)> Robots { get; }
    RowEnumerator CellRows { get; }

    bool AddRobot(IRobot robot);
    int InitLawn(int blockCount);
    void MoveRobots();
}
