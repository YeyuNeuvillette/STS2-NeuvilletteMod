using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using System.Collections.Generic;
using System.Linq;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(Wellspring), "GenerateInitialOptions")]
public static class WellspringPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(Wellspring __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
        {
            return;
        }

        if (__instance.Owner.Character?.Id.Entry != "NEUVILLETTE_CHARACTER_NEUVILLETTE")
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