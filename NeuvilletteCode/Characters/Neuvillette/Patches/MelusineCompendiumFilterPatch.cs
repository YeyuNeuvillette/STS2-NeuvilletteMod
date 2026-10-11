using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Patches;

/// <summary>
/// Keeps Melusine Stickers exclusive to their dedicated compendium filter instead
/// of also listing them under the vanilla miscellaneous/derived-card filter.
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
internal static class MelusineCompendiumFilterPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCardLibrary __instance)
    {
        var miscFilter = AccessTools.Field(typeof(NCardLibrary), "_miscPoolFilter")
            ?.GetValue(__instance) as NCardPoolFilter;
        var poolFilters = AccessTools.Field(typeof(NCardLibrary), "_poolFilters")
            ?.GetValue(__instance) as Dictionary<NCardPoolFilter, Func<CardModel, bool>>;

        if (miscFilter == null || poolFilters == null ||
            !poolFilters.TryGetValue(miscFilter, out var originalFilter))
        {
            return;
        }

        poolFilters[miscFilter] = card =>
            card is not MelusineStickerCard && originalFilter(card);
    }
}
