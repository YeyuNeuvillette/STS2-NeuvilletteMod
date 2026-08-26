using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Characters.Neuvillette.Events;
using Neuvillette.Api;
using Neuvillette.Features.Map;
using Neuvillette.Infrastructure;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch]
internal static class FourQuadrantsLandPatch
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateMap))]
    [HarmonyPostfix]
    public static void Postfix_GenerateMap(RunManager __instance, ref Task __result)
    {
        __result = RestoreMarkersAfterMapGeneration(__result, __instance);
    }

    private static async Task RestoreMarkersAfterMapGeneration(Task generation, RunManager manager)
    {
        await generation;
        var state = GameCompatibility.GetRunState(manager);
        if (state != null)
            EnsureMarked(state);
    }

    public static void EnsureMarked(RunState state)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled())
        {
            FourQuadrantsMarkerService.RemoveAll(state);
            PersonaEliteMarkerService.RemoveAll(state, state.Map);
            return;
        }
        FourQuadrantsMarkerService.EnsureMarked(state);
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
    [HarmonyPostfix]
    public static void Postfix_GenerateRooms(ActModel __instance)
    {
        var rooms = GameCompatibility.GetRooms(__instance);
        if (rooms == null) return;

        var fqId = ModelDb.GetId<FourQuadrantsLand>();
        rooms.events.RemoveAll(e => e.Id == fqId);
    }

    [HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
    [HarmonyPrefix]
    public static bool Prefix_RollRoomTypeFor(RunManager __instance, MapPointType pointType, IEnumerable<RoomType> blacklist, ref RoomType __result)
    {
        if (pointType != MapPointType.Unknown) return true;

        var state = GameCompatibility.GetRunState(__instance);
        if (!FourQuadrantsMarkerService.IsCurrentTarget(state)) return true;

        __result = RoomType.Event;
        return false;
    }

    [HarmonyPatch(typeof(RunManager), "CreateRoom")]
    [HarmonyPrefix]
    public static bool Prefix_CreateRoom(RunManager __instance, RoomType roomType, MapPointType mapPointType, ref AbstractRoom __result)
    {
        if (roomType != RoomType.Event) return true;

        var state = GameCompatibility.GetRunState(__instance);
        if (!FourQuadrantsMarkerService.IsCurrentTarget(state)) return true;

        var fqEvent = ModelDb.GetByIdOrNull<EventModel>(ModelDb.GetId<FourQuadrantsLand>());
        if (fqEvent == null) return true;

        __result = new EventRoom(fqEvent);
        state!.AddVisitedEvent(fqEvent);
        FourQuadrantsMarkerService.MarkCompleted(state);
        NeuvilletteApi.PublishMarkerEntered(new(
            NeuvilletteMapMarkerKind.FourQuadrantsLand,
            state,
            state.CurrentActIndex));
        NeuvilletteApi.PublishMarkerCompleted(new(
            NeuvilletteMapMarkerKind.FourQuadrantsLand,
            state,
            state.CurrentActIndex));
        return false;
    }
}
