using Mowers;
using System.Diagnostics;

using MoveAction = Mowers.Robots.CustomizableRobot.CustomMoveAction;
using Neighbors = Mowers.Robots.CustomizableRobot.Neighbors;

namespace CustomMower;

internal static class Customize
{
    private enum Key : int
    {
        Strat,
        Map,
    }

    private static readonly List<(MoveAction strategy, int weight)> strategies = new()
    {
        (Mapped, 100),
        (Malicious, 1),
        (Nothing, 1),
    };

    internal static void RandomizeStrategy(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells)
    {
        if (data.TryGetAs((int)Key.Strat, out MoveAction? storedStrat))
        {
            storedStrat(data, ref row, ref col, cells);
        }
        else
        {
            int totalWeight = strategies.Sum(s => s.weight);
            int roll = Random.Shared.Next(totalWeight) + 1;

            for (int idx = 0; idx < strategies.Count; idx++)
            {
                roll -= strategies[idx].weight;
                if (roll <= 0)
                {
                    (MoveAction selectedStrat, _) = strategies[idx];
                    data[(int)Key.Strat] = selectedStrat;
                    selectedStrat(data, ref row, ref col, cells);
                    break;
                }
            }

            Debug.Assert(data[(int)Key.Strat] is MoveAction);
        }
    }

    private static void Mapped(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells)
    {
        // TODO: decide on map type and impl
        if (data.TryGetAs((int)Key.Map, out List<object>? map) is false)
        {
            map = new();
            data[(int)Key.Map] = map;
        }

        Debug.Assert(map is not null);// REMOVE: replace `not null` with final type

        // TODO: finish impl
    }

    private static void Malicious(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells)
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

    private static void Nothing(Dictionary<int, object> data, ref int row, ref int col, Neighbors cells)
    { }
}
