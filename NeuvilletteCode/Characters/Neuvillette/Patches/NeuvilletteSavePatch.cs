using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Models;
using System.Linq;
using System.Collections.Generic;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(RoomSet), nameof(RoomSet.FromSave))]
public static class NeuvilletteSavePatch
{
    [HarmonyPrefix]
    public static void Prefix(SerializableRoomSet save)
    {
        if (save.NormalEncounterIds == null) save.NormalEncounterIds = new List<ModelId>();
        if (save.EliteEncounterIds == null) save.EliteEncounterIds = new List<ModelId>();
        if (save.EventIds == null) save.EventIds = new List<ModelId>();

        save.NormalEncounterIds = save.NormalEncounterIds
            .Where(id => ModelDb.GetByIdOrNull<EncounterModel>(id) != null)
            .ToList();

        save.EliteEncounterIds = save.EliteEncounterIds
            .Where(id => ModelDb.GetByIdOrNull<EncounterModel>(id) != null)
            .ToList();

        save.EventIds = save.EventIds
            .Where(id => ModelDb.GetByIdOrNull<EventModel>(id) != null)
            .ToList();
    }
}