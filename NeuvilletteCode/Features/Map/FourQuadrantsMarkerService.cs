using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Extensions;
using Neuvillette.Api;
using Neuvillette.Characters.Neuvillette.Events;

namespace Neuvillette.Features.Map;

internal static class FourQuadrantsMarkerService
{
    private const int StandardActCount = 3;

    internal static bool EnsureMarked(RunState state)
    {
        if (state.CurrentActIndex < 0 || state.CurrentActIndex >= StandardActCount)
            return false;

        var eventId = ModelDb.GetId<FourQuadrantsLand>();
        if (state.VisitedEventIds.Contains(eventId)
            || state.Map.GetAllMapPoints().Any(point => point.Quests.Any(quest => quest.Id == eventId)))
            return false;

        var allPoints = state.Map.GetAllMapPoints().OrderBy(MapRouteService.PointKey).ToArray();
        MapPoint? currentPoint = state.CurrentMapPoint;
        HashSet<MapPoint>? reachable = currentPoint != null && allPoints.Contains(currentPoint)
            ? MapRouteService.GetReachablePoints(currentPoint)
            : null;
        int minimumRow = reachable == null ? -1 : currentPoint!.coord.row;

        var candidates = allPoints
            .Where(point => point.coord.row > minimumRow
                && (reachable == null || reachable.Contains(point))
                && point.PointType == MapPointType.Unknown
                && point.CanBeModified)
            .ToList();
        if (candidates.Count == 0)
            return false;

        var rng = new Rng(state.Rng.Seed, $"FourQuadrantsLand:{state.CurrentActIndex}");
        candidates.UnstableShuffle(rng);
        var eventModel = ModelDb.GetByIdOrNull<EventModel>(eventId);
        if (eventModel == null)
            return false;

        candidates[0].AddQuest(eventModel);
        NeuvilletteApi.PublishMarkerCreated(new(
            NeuvilletteMapMarkerKind.FourQuadrantsLand,
            state,
            state.CurrentActIndex));
        return true;
    }

    internal static bool IsCurrentTarget(RunState? state)
    {
        if (state?.CurrentMapPoint == null)
            return false;

        var eventId = ModelDb.GetId<FourQuadrantsLand>();
        return !state.VisitedEventIds.Contains(eventId)
            && state.CurrentMapPoint.Quests.Any(quest => quest.Id == eventId);
    }
}
