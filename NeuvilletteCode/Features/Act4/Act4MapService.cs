using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Infrastructure;
using Neuvillette.Api;

namespace Neuvillette.Features.Act4;

internal static class Act4MapService
{
    internal static bool TryApplyLinearLayout(StandardActMap map, RunState state)
    {
        if (!GameCompatibility.IsNeuvilletteAct(state))
            return false;

        var grid = GameCompatibility.GetGrid(map);
        if (grid == null)
            return false;

        for (int r = 1; r < grid.GetLength(1); r++)
            for (int c = 0; c < 7; c++)
                grid[c, r] = null!;

        SetPoint(grid, 3, 1, MapPointType.RestSite);
        SetPoint(grid, 3, 2, MapPointType.Treasure);
        SetPoint(grid, 3, 3, MapPointType.Shop);
        SetPoint(grid, 3, 4, MapPointType.RestSite);

        map.StartingMapPoint.PointType = MapPointType.Ancient;
        map.BossMapPoint.PointType = MapPointType.Boss;

        map.StartingMapPoint.Children.Clear();
        map.StartingMapPoint.AddChildPoint(grid[3, 1]);

        grid[3, 1].Children.Clear();
        grid[3, 1].AddChildPoint(grid[3, 2]);
        grid[3, 2].Children.Clear();
        grid[3, 2].AddChildPoint(grid[3, 3]);
        grid[3, 3].Children.Clear();
        grid[3, 3].AddChildPoint(grid[3, 4]);
        grid[3, 4].Children.Clear();
        grid[3, 4].AddChildPoint(map.BossMapPoint);

        ApiRegistry.ConfigureAct4Map(map);

        return true;
    }

    private static void SetPoint(MapPoint[,] grid, int col, int row, MapPointType type)
    {
        MapPoint p = new MapPoint(col, row);
        p.PointType = type;
        p.CanBeModified = false;
        grid[col, row] = p;
    }
}
