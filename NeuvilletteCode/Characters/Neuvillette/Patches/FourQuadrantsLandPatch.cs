using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Characters.Neuvillette.Events;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch]
public static class FourQuadrantsLandPatch
{
    private static MapCoord? _targetCoord;
    private static bool _hasSpawned;

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateMap))]
    [HarmonyPostfix]
    public static void Postfix_GenerateMap(RunManager __instance)
    {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
        if (state == null) return;

        if (state.CurrentActIndex != 0)
        {
            _targetCoord = null;
            return;
        }

        _targetCoord = null;
        _hasSpawned = false;

        var map = state.Map;
        var rng = new Rng(state.Rng.Seed, "FourQuadrantsLand");

        var unknownPoints = map.GetAllMapPoints()
            .Where(p => p.PointType == MapPointType.Unknown && p.CanBeModified)
            .ToList();

        if (unknownPoints.Count == 0) return;

        unknownPoints.UnstableShuffle(rng);
        var targetPoint = unknownPoints[0];
        _targetCoord = targetPoint.coord;

        var fqEvent = ModelDb.GetByIdOrNull<EventModel>(ModelDb.GetId<FourQuadrantsLand>());
        if (fqEvent != null)
        {
            targetPoint.AddQuest(fqEvent);
        }
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
    [HarmonyPostfix]
    public static void Postfix_GenerateRooms(ActModel __instance)
    {
        var rooms = AccessTools.Field(typeof(ActModel), "_rooms").GetValue(__instance) as RoomSet;
        if (rooms == null) return;

        var fqId = ModelDb.GetId<FourQuadrantsLand>();
        rooms.events.RemoveAll(e => e.Id == fqId);
    }

    [HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
    [HarmonyPrefix]
    public static bool Prefix_RollRoomTypeFor(RunManager __instance, MapPointType pointType, IEnumerable<RoomType> blacklist, ref RoomType __result)
    {
        if (pointType != MapPointType.Unknown || !_targetCoord.HasValue || _hasSpawned) return true;

        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
        if (state == null) return true;

        var currentCoord = state.CurrentMapCoord;
        if (!currentCoord.HasValue || currentCoord.Value != _targetCoord.Value) return true;

        __result = RoomType.Event;
        return false;
    }

    [HarmonyPatch(typeof(RunManager), "CreateRoom")]
    [HarmonyPrefix]
    public static bool Prefix_CreateRoom(RunManager __instance, RoomType roomType, MapPointType mapPointType, ref AbstractRoom __result)
    {
        if (roomType != RoomType.Event || !_targetCoord.HasValue || _hasSpawned) return true;

        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
        if (state == null) return true;

        var currentCoord = state.CurrentMapCoord;
        if (!currentCoord.HasValue || currentCoord.Value != _targetCoord.Value) return true;

        var fqEvent = ModelDb.GetByIdOrNull<EventModel>(ModelDb.GetId<FourQuadrantsLand>());
        if (fqEvent == null) return true;

        __result = new EventRoom(fqEvent);
        _hasSpawned = true;
        state.AddVisitedEvent(fqEvent);
        return false;
    }
}