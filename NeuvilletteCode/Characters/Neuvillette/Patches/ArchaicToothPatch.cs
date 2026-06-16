using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(ArchaicTooth), "get_TranscendenceUpgrades")]
public static class ArchaicToothPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Dictionary<ModelId, CardModel> __result)
    {
        __result[ModelDb.Card<Tide>().Id] = ModelDb.Card<GodOfLife>();
    }
}
