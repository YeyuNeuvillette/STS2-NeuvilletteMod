using MegaCrit.Sts2.Core.Map;

namespace Neuvillette.Features.Map;

internal static class MapRouteService
{
    internal static HashSet<MapPoint> GetReachablePoints(MapPoint origin)
    {
        var reachable = new HashSet<MapPoint>();
        var pending = new Stack<MapPoint>(origin.Children.OrderBy(PointKey));
        while (pending.TryPop(out var point))
        {
            if (!reachable.Add(point))
                continue;

            foreach (var child in point.Children.OrderBy(PointKey))
                pending.Push(child);
        }

        return reachable;
    }

    internal static long PointKey(MapPoint point) =>
        ((long)point.coord.row << 32) | (uint)point.coord.col;
}
