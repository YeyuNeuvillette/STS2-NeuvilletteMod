using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(RoomSet), nameof(RoomSet.FromSave))]
internal static class NeuvilletteSavePatch
{
    [HarmonyPrefix]
    public static void Prefix(SerializableRoomSet save)
    {
        // Empty room lists are omitted from JSON, but the base loader assumes they are non-null.
        save.EventIds ??= [];
        save.NormalEncounterIds ??= [];
        save.EliteEncounterIds ??= [];
    }
}
