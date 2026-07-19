using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Neuvillette.Infrastructure;

internal static class GameCompatibility
{
    private static readonly PropertyInfo? RunManagerState = AccessTools.Property(typeof(RunManager), "State");
    private static readonly PropertyInfo? StandardActMapGrid = AccessTools.Property(typeof(StandardActMap), "Grid");
    private static readonly FieldInfo? ActRooms = AccessTools.Field(typeof(ActModel), "_rooms");
    private static readonly PropertyInfo? RunStateActsProperty = AccessTools.Property(typeof(RunState), nameof(RunState.Acts));
    private static readonly FieldInfo? MultiplayerHealthBar = AccessTools.Field(typeof(NMultiplayerPlayerState), "_healthBar");
    private static readonly HashSet<string> ReportedFailures = [];

    internal static bool IsNeuvillette(Player? player) =>
        player?.Character is Characters.Neuvillette.Neuvillette;

    internal static bool IsNeuvilletteAct(RunState? state) =>
        state?.Act is Characters.Neuvillette.Act.NeuvilletteAct;

    internal static bool IsRunAuthority(RunManager? runManager = null)
    {
        var manager = runManager ?? RunManager.Instance;
        return manager.IsSingleplayerOrFakeMultiplayer
            || manager.NetService is INetHostGameService;
    }

    internal static RunState? GetRunState(RunManager? runManager = null) =>
        Read<RunState>(RunManagerState, runManager ?? RunManager.Instance, "RunManager.State");

    internal static MapPoint[,]? GetGrid(StandardActMap map) =>
        Read<MapPoint[,]>(StandardActMapGrid, map, "StandardActMap.Grid");

    internal static RoomSet? GetRooms(ActModel act) =>
        Read<RoomSet>(ActRooms, act, "ActModel._rooms");

    internal static NHealthBar? GetMultiplayerHealthBar(NMultiplayerPlayerState playerState) =>
        Read<NHealthBar>(MultiplayerHealthBar, playerState, "NMultiplayerPlayerState._healthBar");

    internal static void SetActs(RunState runState, IReadOnlyList<ActModel> acts)
    {
        if (RunStateActsProperty == null) return;
        try
        {
            RunStateActsProperty.SetValue(runState, acts);
        }
        catch (Exception ex)
        {
            ReportOnce("RunState.Acts.set", $"Failed to set Acts: {ex.Message}");
        }
    }

    internal static void Validate()
    {
        ReportMissing(RunManagerState, "RunManager.State");
        ReportMissing(StandardActMapGrid, "StandardActMap.Grid");
        ReportMissing(ActRooms, "ActModel._rooms");
        ReportMissing(RunStateActsProperty, "RunState.Acts");
        ReportMissing(MultiplayerHealthBar, "NMultiplayerPlayerState._healthBar");
    }

    private static T? Read<T>(MemberInfo? member, object instance, string name) where T : class
    {
        try
        {
            return member switch
            {
                PropertyInfo property => property.GetValue(instance) as T,
                FieldInfo field => field.GetValue(instance) as T,
                _ => null,
            };
        }
        catch (Exception ex)
        {
            ReportOnce(name, $"Compatibility access '{name}' failed; related feature is disabled: {ex.Message}");
            return null;
        }
    }

    private static void ReportMissing(MemberInfo? member, string name)
    {
        if (member == null)
            ReportOnce(name, $"Compatibility member '{name}' was not found; related feature will be disabled.");
    }

    private static void ReportOnce(string key, string message)
    {
        lock (ReportedFailures)
        {
            if (ReportedFailures.Add(key))
                MainFile.Logger.Warn(message);
        }
    }
}
