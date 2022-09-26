using System.Text;

using Mowers;
using Mowers.Robots;
using CustomMower;

const int LAWN_WIDTH = 40;
const int LAWN_HEIGHT = 25;
const int SLEEP_DURATION = 200;

// BUG: Windows-Terminal: entire terminal blinking when robot leaves

Lawn lawn = new(LAWN_WIDTH, LAWN_HEIGHT);

StringBuilder buffer = new(lawn.Height * (lawn.Width + 1) * 10);
_ = buffer.Append("\x1b[?1049h") // use alternate screen-buffer
    .Append("\x1b[2J") // clear screen
    .Append("\x1b[1;1H") // set cursor position to 1/1
    .Append("\x1b[?25l") // hide cursor
;

lawn.AddRobot(new CustomizableRobot() { MoveAction = Customize.RandomizeStrategy });
lawn.AddRobot(new CustomizableRobot() { MoveAction = Customize.RandomizeStrategy });
lawn.AddRobot(new CustomizableRobot() { MoveAction = Customize.RandomizeStrategy });
lawn.AddRobot(new CustomizableRobot() { MoveAction = Customize.RandomizeStrategy });

bool isLawnInited = false;
bool isPaused = false;

ConsoleKeyInfo k = default;
do
{
#if !NO_INPUT
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
#endif
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
    .Append("\x1b[0m") // restore text format/color
    .Append("\x1b[?25h") // restore cursor
    .Append("\x1b[?1049l") // restore main screen-buffer
;
Console.Write(buffer);

static ReadOnlySpan<char> MapCell(Cell cell) => cell switch
{
    Cell.Unmown => "\x1b[30;42m;",            // black-fg green-bg ;
    Cell.Mown => "\x1b[30;102m.",             // black-fg bright-green-bg .
    Cell.Forbidden => "\x1b[30;41mX",         // black-fg red-bg X
    Cell.Robot => "\x1b[97;44mO",             // bright-white-fg blue-bg O
    Cell.ForbiddenRobot => "\x1b[97;105m!",   // bright-white-fg bright-magenta-bg !
    Cell.ForbiddenMown => "\x1b[30;101m~",    // black-fg bright-red-bg ~
    _ => throw new InvalidOperationException(),
};
