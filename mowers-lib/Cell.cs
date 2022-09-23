namespace Mowers;

public enum Cell : byte
{
    Unmown = 0,
    Mown = 1,
    Forbidden = 2,
    Robot = 4,

    ForbiddenMown = Forbidden | Mown,
    ForbiddenRobot = Forbidden | Robot,
}