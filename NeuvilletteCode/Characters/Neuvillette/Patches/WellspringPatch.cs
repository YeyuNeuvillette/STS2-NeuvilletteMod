using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(Wellspring), "GenerateInitialOptions")]
internal static class WellspringPatch
{
    [HarmonyPostfix]
    public static void Postfix(Wellspring __instance, ref IReadOnlyList<EventOption> __result)
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
        
        mutable.Add(new EventOption(__instance, () => Taste(__instance), "WELLSPRING.pages.INITIAL.options.TASTE"));
        
        __result = mutable;
    }

    private static async Task Taste(Wellspring @event)
    {
        if (@event.Owner == null)
            return;

        await CreatureCmd.GainMaxHp(@event.Owner.Creature, 4);
        @event.SetEventFinished(@event.L10NLookup("WELLSPRING.pages.TASTE.description"));
    }
}