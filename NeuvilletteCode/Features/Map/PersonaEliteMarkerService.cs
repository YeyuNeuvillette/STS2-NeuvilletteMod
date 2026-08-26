using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Characters.Neuvillette.Relics;

namespace Neuvillette.Features.Map;

internal static class PersonaEliteMarkerService
{
    internal const int StandardActCount = 3;

    internal static bool IsCompleted(IRunState runState) =>
        runState.Players.Any(player =>
            player.GetRelic<Persona>()?.MarkedEliteCompleted == true);

    internal static bool EnsureMarked(
        Persona marker,
        IRunState runState,
        ActMap map,
        int actIndex,
        int minimumRow,
        MapPoint? routeOrigin)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled())
            return false;
        if (actIndex < 0 || actIndex >= StandardActCount)
            return false;
        if (Normalize(runState, map) != null)
            return false;

        HashSet<MapPoint>? reachablePoints = routeOrigin == null
            ? null
            : MapRouteService.GetReachablePoints(routeOrigin);
        if (TryGetSavedCoord(runState, actIndex, out MapCoord savedCoord))
        {
            MapPoint? savedPoint = map.GetPoint(savedCoord);
            if (savedPoint != null)
            {
                if (savedPoint.PointType != MapPointType.Elite)
                {
                    MainFile.Logger.Warn(
                        $"[Map] Restoring Persona point type from {savedPoint.PointType} to {MapPointType.Elite}: act={actIndex}, coord={savedCoord}.");
                    savedPoint.PointType = MapPointType.Elite;
                }
                AttachMarker(marker, runState, actIndex, savedPoint, restored: true);
                return true;
            }

            MainFile.Logger.Error(
                $"[Map] Saved Persona elite coordinate is absent from the restored map: act={actIndex}, coord={savedCoord}. The snapshot is retained and will not be rerolled.");
            return false;
        }

        if (runState.CurrentActIndex == actIndex
            && runState.CurrentMapCoord is { } currentCoord
            && map.GetPoint(currentCoord) is { PointType: MapPointType.Elite } currentElite)
        {
            AttachMarker(marker, runState, actIndex, currentElite, restored: true);
            return true;
        }

        var rng = new Rng(runState.Rng.Seed, $"PersonaElite:{actIndex}");
        var candidates = map.GetAllMapPoints()
            .Where(point => point.PointType == MapPointType.Elite
                && IsEligible(point, runState, actIndex, minimumRow, reachablePoints))
            .OrderBy(MapRouteService.PointKey)
            .ToList();
        candidates.UnstableShuffle(rng);

        MapPoint? chosen = candidates.FirstOrDefault();
        if (chosen == null)
            return false;

        AttachMarker(marker, runState, actIndex, chosen, restored: false);
        return true;
    }

    private static bool IsEligible(
        MapPoint point,
        IRunState runState,
        int actIndex,
        int minimumRow,
        HashSet<MapPoint>? reachablePoints)
    {
        if (runState.CurrentActIndex == actIndex
            && runState.CurrentMapCoord == point.coord)
            return true;

        return point.coord.row > minimumRow
            && (reachablePoints == null || reachablePoints.Contains(point));
    }

    private static void AttachMarker(
        Persona marker,
        IRunState runState,
        int actIndex,
        MapPoint point,
        bool restored)
    {
        point.AddQuest(marker);
        RememberCoord(runState, actIndex, point.coord);
        MainFile.Logger.Info(
            $"[Map] {(restored ? "Restored" : "Created")} single Persona elite marker: act={actIndex}, coord={point.coord}.");
    }

    private static bool TryGetSavedCoord(IRunState runState, int actIndex, out MapCoord coord)
    {
        if (MapMarkerPersistenceService.TryGetPersonaElite(runState, actIndex, out var persisted)
            && persisted.TryGetCoord(actIndex, out coord))
        {
            RememberCoord(runState, actIndex, coord);
            return true;
        }

        List<MapCoord> savedCoords = runState.Players
            .Select(player => player.GetRelic<Persona>())
            .Where(persona => persona != null)
            .Select(persona => persona!)
            .Select(persona => persona.TryGetMarkedEliteCoord(actIndex, out MapCoord saved)
                ? saved
                : (MapCoord?)null)
            .Where(saved => saved.HasValue)
            .Select(saved => saved!.Value)
            .Distinct()
            .OrderBy(saved => saved.row)
            .ThenBy(saved => saved.col)
            .ToList();

        if (savedCoords.Count == 0)
        {
            coord = default;
            return false;
        }

        coord = runState.CurrentMapCoord is { } current && savedCoords.Contains(current)
            ? current
            : savedCoords[0];
        if (savedCoords.Count > 1)
        {
            MainFile.Logger.Warn(
                $"[Map] Found {savedCoords.Count} conflicting saved Persona elite markers for act {actIndex}; keeping {coord}.");
        }
        RememberCoord(runState, actIndex, coord);
        return true;
    }

    private static void RememberCoord(IRunState runState, int actIndex, MapCoord coord)
    {
        MapMarkerPersistenceService.RememberPersonaElite(runState, actIndex, coord);
        foreach (var player in runState.Players)
            player.GetRelic<Persona>()?.SetMarkedEliteCoord(actIndex, coord);
    }

    internal static void MarkCompleted(IRunState runState)
    {
        MapMarkerPersistenceService.ClearPersonaElite(runState);
        foreach (var player in runState.Players)
            player.GetRelic<Persona>()?.ClearMarkedEliteCoord();
    }

    internal static Persona? GetCurrentMarker(IRunState? runState)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled() || runState == null)
            return null;
        Normalize(runState, runState.Map);
        return runState.CurrentMapPoint?.Quests.OfType<Persona>().FirstOrDefault();
    }

    internal static Persona? Normalize(IRunState runState, ActMap map)
    {
        var markedPoints = map.GetAllMapPoints()
            .Where(point => point.Quests.Any(quest => quest is Persona))
            .OrderBy(MapRouteService.PointKey)
            .ToList();
        if (markedPoints.Count == 0)
            return null;

        MapPoint? current = runState.CurrentMapCoord is { } currentCoord
            ? map.GetPoint(currentCoord)
            : null;
        MapPoint keeperPoint = current != null && markedPoints.Contains(current)
            ? current
            : markedPoints[0];
        Persona keeper = keeperPoint.Quests.OfType<Persona>().First();
        int removed = 0;
        foreach (MapPoint point in markedPoints)
        {
            foreach (Persona marker in point.Quests.OfType<Persona>().ToArray())
            {
                if (ReferenceEquals(point, keeperPoint) && ReferenceEquals(marker, keeper))
                    continue;
                point.RemoveQuest(marker);
                removed++;
            }
        }

        if (removed > 0)
        {
            MainFile.Logger.Warn(
                $"[Map] Collapsed {removed + 1} Persona elite marker quests to one at {keeperPoint.coord}.");
        }
        return keeper;
    }

    internal static int RemoveAll(IRunState runState, ActMap map)
    {
        MapMarkerPersistenceService.ClearPersonaElite(runState);
        int removed = 0;
        foreach (MapPoint point in map.GetAllMapPoints())
        {
            foreach (Persona marker in point.Quests.OfType<Persona>().ToArray())
            {
                point.RemoveQuest(marker);
                removed++;
            }
        }

        foreach (var player in runState.Players)
            player.GetRelic<Persona>()?.ClearMarkedEliteCoord();

        if (removed > 0)
            MainFile.Logger.Info($"[Map] Removed {removed} Persona elite marker(s) because Act 4 is disabled.");
        return removed;
    }
}
