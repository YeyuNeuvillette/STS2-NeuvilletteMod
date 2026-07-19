using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Events;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(RanwidTheElder), "GenerateInitialOptions")]
internal static class RanwidTheElderPatch
{
    [HarmonyPostfix]
    public static void Postfix(RanwidTheElder __instance, ref IReadOnlyList<EventOption> __result)
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
        
        mutable.Add(new EventOption(__instance, () => ShowCare(__instance), "RANWID_THE_ELDER.pages.INITIAL.options.SHOW_CARE"));
        
        __result = mutable;
    }

    private static async Task ShowCare(RanwidTheElder @event)
    {
        if (@event.Owner == null)
            return;

        await PlayerCmd.LoseGold(100, @event.Owner, GoldLossType.Spent);
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), @event.Owner.Creature, 4m, isFromCard: false);
        
        for (int i = 0; i < 3; i++)
        {
            var relic = RelicFactory.PullNextRelicFromFront(@event.Owner).ToMutable();
            await RelicCmd.Obtain(relic, @event.Owner);
        }
        
        @event.SetEventFinished(@event.L10NLookup("RANWID_THE_ELDER.pages.SHOW_CARE.description"));
    }
}