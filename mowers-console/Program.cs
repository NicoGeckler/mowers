using System.Text;

using Mowers;
using Mowers.Robots;

const int LAWN_WIDTH = 40;
const int LAWN_HEIGHT = 25;
const int SLEEP_DURATION = 200;

// FIX: Windows-Terminal entire terminal blinking when robot leaves (test cmd)

Lawn lawn = new(LAWN_WIDTH, LAWN_HEIGHT);

StringBuilder buffer = new(lawn.Height * (lawn.Width + 1) * 4);
_ = buffer.Append("\x1b[?1049h") // use alternate screen-buffer
    .Append("\x1b[2J") // clear screen
    .Append("\x1b[1;1H") // set cursor position to 1/1
    .Append("\x1b[?25l") // hide cursor
;

lawn.AddRobot(new LineRobot());
lawn.AddRobot(new LineRobot());
lawn.AddRobot(new LineRobot());
lawn.AddRobot(new CustomizableRobot() { MoveAction = CustomMove });

bool isLawnInited = false;
bool isPaused = false;

ConsoleKeyInfo k = default;
do
{
    if (Console.KeyAvailable)
    {
        k = Console.ReadKey(true);

#pragma warning disable IDE0010 // Populate switch; reason: only some values relevant
        switch (k.Key)
#pragma warning restore IDE0010
        {
            case ConsoleKey.Enter:
                isLawnInited = false;
                break;
            case ConsoleKey.Spacebar:
                isPaused ^= true;
                break;
        }
    }
    if (isLawnInited is false)
    {
        _ = lawn.InitLawn();
        isLawnInited = true;
    }

    if (isPaused is false)
    {
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
        lawn.MoveRobots();
    }

    Thread.Sleep(SLEEP_DURATION);
}
while (k.Key is not ConsoleKey.Escape);

_ = buffer.Clear()
    .Append("\x1b[0m") // restore text format
    .Append("\x1b[?25h") // restore cursor
    .Append("\x1b[?1049l") // restore main screen-buffer
;
Console.Write(buffer);

static ReadOnlySpan<char> MapCell(Cell cell) => cell switch
{
    Cell.Unmown => "\x1b[32m;",                     // green ;
    Cell.Mown => "\x1b[1;32m.\x1b[22m",             // bright green . un-bright
    Cell.Forbidden => "\x1b[31mX",                  // red X
    Cell.Robot => "\x1b[34mO",                      // blue O
    Cell.ForbiddenRobot => "\x1b[5m!",              // magenta !
    Cell.ForbiddenMown => "\x1b[1;31m~\x1b[22m",    // bright red ~ un-bright
    _ => throw new InvalidOperationException(),
};

static void CustomMove(Dictionary<int, object> data, ref int row, ref int col, CustomizableRobot.Neighbors cells)
{
    if (Random.Shared.NextDouble() >= 0.5)
    {
        row++;
    }
    else
    {
        col++;
    }
}