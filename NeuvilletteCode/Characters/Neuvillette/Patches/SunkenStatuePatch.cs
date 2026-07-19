using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Events;
using Neuvillette.Characters.Neuvillette.Relics;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(SunkenStatue), "GenerateInitialOptions")]
internal static class SunkenStatuePatch
{
    [HarmonyPostfix]
    public static void Postfix(SunkenStatue __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var relicHoverTip = HoverTipFactory.FromRelic<StatueFragment>();
        mutable.Add(new EventOption(__instance, () => Repair(__instance), "SUNKEN_STATUE.pages.INITIAL.options.REPAIR", relicHoverTip));
        
        __result = mutable;
    }

    private static async Task Repair(SunkenStatue @event)
    {
        if (@event.Owner == null)
            return;

        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), @event.Owner.Creature, 5m, isFromCard: false);
        await RelicCmd.Obtain<StatueFragment>(@event.Owner);
        
        @event.SetEventFinished(@event.L10NLookup("SUNKEN_STATUE.pages.REPAIR.description"));
    }
}