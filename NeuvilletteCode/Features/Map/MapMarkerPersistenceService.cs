using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

namespace Neuvillette.Features.Map;

/// <summary>
/// The authoritative, save-backed snapshot for every Neuvillette map marker.
/// Map quests themselves are not serialized by the game, so they must always be
/// reconstructed from this snapshot after the saved map has been restored.
/// </summary>
internal static class MapMarkerPersistenceService
{
    private const int CurrentVersion = 2;
    private const string SavedMarkerKey = "four_quadrants_marker";

    public sealed class MarkerRecord
    {
        public bool Active { get; set; }
        public int ActIndex { get; set; } = -1;
        public int Column { get; set; } = -1;
        public int Row { get; set; } = -1;
        public int PointType { get; set; } = -1;

        public bool TryGetCoord(int actIndex, out MapCoord coord)
        {
            if (Active && ActIndex == actIndex && Column >= 0 && Row >= 0)
            {
                coord = new MapCoord(Column, Row);
                return true;
            }

            coord = default;
            return false;
        }
    }

    public sealed class MarkerSnapshot
    {
        public int Version { get; set; } = CurrentVersion;
        public MarkerRecord FourQuadrants { get; set; } = new();
        public MarkerRecord PersonaElite { get; set; } = new();

        // Version 1 stored the Four Quadrants coordinate directly at the root.
        // Keep these properties so an in-progress run made by the previous build
        // can be migrated without changing the selected room.
        public int ActIndex { get; set; } = -1;
        public int Column { get; set; } = -1;
        public int Row { get; set; } = -1;
    }

    private static readonly RunSavedData<MarkerSnapshot> MarkerState =
        RunSavedDataStore.For(MainFile.ModId).Register<MarkerSnapshot>(
            key: SavedMarkerKey,
            defaultFactory: static () => new MarkerSnapshot(),
            options: new RunSavedDataOptions
            {
                WritePolicy = RunSavedDataWritePolicy.AlwaysWhenRegistered,
            });

    internal static void RegisterSavedData() => _ = MarkerState;

    internal static bool TryGetFourQuadrants(RunState state, out MarkerRecord marker) =>
        TryGet(state, static snapshot => snapshot.FourQuadrants, out marker);

    internal static bool TryGetPersonaElite(IRunState runState, int actIndex, out MarkerRecord marker)
    {
        if (runState is RunState state
            && TryGet(state, static snapshot => snapshot.PersonaElite, out marker)
            && marker.ActIndex == actIndex)
            return true;

        marker = new MarkerRecord();
        return false;
    }

    internal static void RememberFourQuadrants(RunState state, MapCoord coord) =>
        Update(state, snapshot => Set(snapshot.FourQuadrants, state.CurrentActIndex, coord, MapPointType.Unknown));

    internal static void RememberPersonaElite(IRunState runState, int actIndex, MapCoord coord)
    {
        if (runState is RunState state)
            Update(state, snapshot => Set(snapshot.PersonaElite, actIndex, coord, MapPointType.Elite));
    }

    internal static void ClearFourQuadrants(RunState state) =>
        Update(state, static snapshot => snapshot.FourQuadrants = new MarkerRecord());

    internal static void ClearPersonaElite(IRunState runState)
    {
        if (runState is RunState state)
            Update(state, static snapshot => snapshot.PersonaElite = new MarkerRecord());
    }

    internal static MapPointType GetExpectedPointType(MarkerRecord marker, MapPointType fallback) =>
        Enum.IsDefined(typeof(MapPointType), marker.PointType)
            ? (MapPointType)marker.PointType
            : fallback;

    private static bool TryGet(
        RunState state,
        Func<MarkerSnapshot, MarkerRecord> selector,
        out MarkerRecord marker)
    {
        MarkerSnapshot snapshot = GetSnapshot(state);
        marker = selector(snapshot);
        return marker.Active;
    }

    private static MarkerSnapshot GetSnapshot(RunState state)
    {
        MarkerSnapshot snapshot = MarkerState.TryGet(state, out MarkerSnapshot saved)
            ? saved
            : new MarkerSnapshot();

        snapshot.FourQuadrants ??= new MarkerRecord();
        snapshot.PersonaElite ??= new MarkerRecord();

        bool migrated = false;
        if (!snapshot.FourQuadrants.Active
            && snapshot.ActIndex >= 0
            && snapshot.Column >= 0
            && snapshot.Row >= 0)
        {
            Set(
                snapshot.FourQuadrants,
                snapshot.ActIndex,
                new MapCoord(snapshot.Column, snapshot.Row),
                MapPointType.Unknown);
            migrated = true;
        }

        if (snapshot.Version != CurrentVersion
            || snapshot.ActIndex != -1
            || snapshot.Column != -1
            || snapshot.Row != -1)
        {
            snapshot.Version = CurrentVersion;
            snapshot.ActIndex = -1;
            snapshot.Column = -1;
            snapshot.Row = -1;
            migrated = true;
        }

        if (migrated)
            MarkerState.Set(state, snapshot);
        return snapshot;
    }

    private static void Update(RunState state, Action<MarkerSnapshot> update)
    {
        MarkerSnapshot snapshot = GetSnapshot(state);
        update(snapshot);
        snapshot.Version = CurrentVersion;
        MarkerState.Set(state, snapshot);
    }

    private static void Set(
        MarkerRecord marker,
        int actIndex,
        MapCoord coord,
        MapPointType pointType)
    {
        marker.Active = true;
        marker.ActIndex = actIndex;
        marker.Column = coord.col;
        marker.Row = coord.row;
        marker.PointType = (int)pointType;
    }
}
