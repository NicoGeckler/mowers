using System.Text;
using Mowers;
using Mowers.Robots;

Lawn lawn = new();

StringBuilder buffer = new(lawn.Height * (lawn.Width + 1));
_ = buffer.Append("\x1b[?1049h") // use alternate screen-buffer
    .Append("\x1b[2J") // clear screen
    .Append("\x1b[1;1H") // set cursor position to 1/1
    .Append("\x1b[?25l") // hide cursor
;

lawn.AddRobot(new RandomRobot());
lawn.AddRobot(new RandomRobot());
lawn.AddRobot(new LineRobot());
lawn.AddRobot(new LineRobot());

bool isLawnInited = false;

ConsoleKeyInfo k = default;
do
{
    if (Console.KeyAvailable)
    {
        k = Console.ReadKey(true);
        if (k.Key is ConsoleKey.Enter)
        {
            isLawnInited = false;
        }
    }
    if (isLawnInited is false)
    {
        _ = lawn.InitLawn();
        isLawnInited = true;
    }

    foreach (ReadOnlySpan<Cell> row in lawn.CellRows)
    {
        foreach (Cell cell in row)
        {
            _ = buffer.Append(MapCell(cell));
        }
        _ = buffer.Append('\n');
    }
    _ = buffer.Append("\x1b[1;1H"); // set cursor position to 1/1
    Console.Write(buffer);
    _ = buffer.Clear();
    lawn.Step();

    Thread.Sleep(200);
}
while (k.Key is not ConsoleKey.Escape);

_ = buffer.Clear()
    .Append("\x1b[?25h") // restore cursor
    .Append("\x1b[?1049l") // restore main screen-buffer
;
Console.Write(buffer);

static ReadOnlySpan<char> MapCell(Cell cell) => cell switch
{
    Cell.Unmown => "~",
    Cell.Mown => ".",
    Cell.Forbidden => "X",
    Cell.Robot => "O",
    Cell.ForbiddenRobot => "!",
    Cell.ForbiddenMown => "x",
    _ => throw new InvalidOperationException(),
};