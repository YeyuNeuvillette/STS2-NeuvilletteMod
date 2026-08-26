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
        if (!NeuvilletteSettingsStore.IsAct4Enabled())
            return false;
        if (state.CurrentActIndex < 0 || state.CurrentActIndex >= StandardActCount)
            return false;

        var eventId = ModelDb.GetId<FourQuadrantsLand>();
        if (state.VisitedEventIds.Contains(eventId))
        {
            MapMarkerPersistenceService.ClearFourQuadrants(state);
            return false;
        }

        MapPoint? existingPoint = state.Map.GetAllMapPoints()
            .Where(point => point.Quests.Any(quest => quest.Id == eventId))
            .OrderBy(MapRouteService.PointKey)
            .FirstOrDefault();
        if (existingPoint != null)
        {
            RememberCoord(state, existingPoint.coord);
            return false;
        }

        var eventModel = ModelDb.GetByIdOrNull<EventModel>(eventId);
        if (eventModel == null)
            return false;

        if (MapMarkerPersistenceService.TryGetFourQuadrants(state, out var savedMarker)
            && savedMarker.TryGetCoord(state.CurrentActIndex, out MapCoord savedCoord))
        {
            MapPoint? savedPoint = state.Map.GetPoint(savedCoord);
            if (savedPoint != null)
            {
                MapPointType expectedType = MapMarkerPersistenceService.GetExpectedPointType(
                    savedMarker,
                    MapPointType.Unknown);
                if (savedPoint.PointType != expectedType)
                {
                    MainFile.Logger.Warn(
                        $"[Map] Restoring Four Quadrants point type from {savedPoint.PointType} to {expectedType}: act={state.CurrentActIndex}, coord={savedCoord}.");
                    savedPoint.PointType = expectedType;
                }
                savedPoint.AddQuest(eventModel);
                MainFile.Logger.Info(
                    $"[Map] Restored Four Quadrants marker: act={state.CurrentActIndex}, coord={savedCoord}.");
                return true;
            }

            MainFile.Logger.Error(
                $"[Map] Saved Four Quadrants coordinate is absent from the restored map: act={state.CurrentActIndex}, coord={savedCoord}. The snapshot is retained and will not be rerolled.");
            return false;
        }

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

        MapPoint chosen = candidates[0];
        chosen.AddQuest(eventModel);
        RememberCoord(state, chosen.coord);
        MainFile.Logger.Info(
            $"[Map] Created Four Quadrants marker: act={state.CurrentActIndex}, coord={chosen.coord}.");
        NeuvilletteApi.PublishMarkerCreated(new(
            NeuvilletteMapMarkerKind.FourQuadrantsLand,
            state,
            state.CurrentActIndex));
        return true;
    }

    internal static bool IsCurrentTarget(RunState? state)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled() || state?.CurrentMapPoint == null)
            return false;

        var eventId = ModelDb.GetId<FourQuadrantsLand>();
        return !state.VisitedEventIds.Contains(eventId)
            && state.CurrentMapPoint.Quests.Any(quest => quest.Id == eventId);
    }

    internal static void MarkCompleted(RunState state)
    {
        MapMarkerPersistenceService.ClearFourQuadrants(state);
    }

    internal static int RemoveAll(RunState state)
    {
        MapMarkerPersistenceService.ClearFourQuadrants(state);
        var eventId = ModelDb.GetId<FourQuadrantsLand>();
        int removed = 0;
        foreach (MapPoint point in state.Map.GetAllMapPoints())
        {
            foreach (var marker in point.Quests.Where(quest => quest.Id == eventId).ToArray())
            {
                point.RemoveQuest(marker);
                removed++;
            }
        }

        if (removed > 0)
            MainFile.Logger.Info($"[Map] Removed {removed} Four Quadrants marker(s) because Act 4 is disabled.");
        return removed;
    }

    private static void RememberCoord(RunState state, MapCoord coord)
        => MapMarkerPersistenceService.RememberFourQuadrants(state, coord);
}
