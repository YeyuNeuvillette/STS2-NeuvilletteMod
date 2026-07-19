using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(TeaMaster), "GenerateInitialOptions")]
internal static class TeaMasterPatch
{
    [HarmonyPostfix]
    public static void Postfix(TeaMaster __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
        {
            return;
        }

        if (!GameCompatibility.IsNeuvillette(__instance.Owner))
        {
            return;
        }

        var mutable = __result as List<EventOption> ?? __result.ToList();
        
        mutable.Add(new EventOption(__instance, () => HalfPriceThreeCups(__instance), "TEA_MASTER.pages.INITIAL.options.HALF_PRICE_THREE_CUPS"));
        
        __result = mutable;
    }

    private static async Task HalfPriceThreeCups(TeaMaster @event)
    {
        if (@event.Owner == null)
            return;

        await PlayerCmd.LoseGold(100, @event.Owner, GoldLossType.Spent);
        await RelicCmd.Obtain<BoneTea>(@event.Owner);
        await RelicCmd.Obtain<EmberTea>(@event.Owner);
        await RelicCmd.Obtain<TeaOfDiscourtesy>(@event.Owner);
        @event.SetEventFinished(@event.L10NLookup("TEA_MASTER.pages.HALF_PRICE_THREE_CUPS.description"));
    }
}