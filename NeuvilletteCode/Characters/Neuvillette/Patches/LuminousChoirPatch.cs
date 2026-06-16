using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using Neuvillette.Characters.Neuvillette.Cards;
using System.Collections.Generic;
using System.Linq;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(LuminousChoir), "GenerateInitialOptions")]
public static class LuminousChoirPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(LuminousChoir __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var reachIntoTheFleshOption = mutable.FirstOrDefault(o => o.TextKey == "LUMINOUS_CHOIR.pages.INITIAL.options.REACH_INTO_THE_FLESH");
        if (reachIntoTheFleshOption != null)
        {
            mutable.Remove(reachIntoTheFleshOption);
        }
        
        var cardHoverTip = HoverTipFactory.FromCard<TravelingSpore>();
        mutable.Add(new EventOption(__instance, () => EntrustedSpores(__instance), "LUMINOUS_CHOIR.pages.INITIAL.options.ENTRUSTED_SPORES", cardHoverTip));
        
        __result = mutable;
    }

    private static async Task EntrustedSpores(LuminousChoir @event)
    {
        if (@event.Owner == null)
            return;

        var card = @event.Owner.RunState.CreateCard<TravelingSpore>(@event.Owner);
        if (card != null)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
        }
        
        @event.SetEventFinished(@event.L10NLookup("LUMINOUS_CHOIR.pages.ENTRUSTED_SPORES.description"));
    }
}