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

[HarmonyPatch(typeof(SpiralingWhirlpool), "GenerateInitialOptions")]
public static class SpiralingWhirlpoolPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(SpiralingWhirlpool __instance, ref IReadOnlyList<EventOption> __result)
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
        
        mutable.Add(new EventOption(__instance, () => TakeAway(__instance), "SPIRALING_WHIRLPOOL.pages.INITIAL.options.TAKE_AWAY", HoverTipFactory.FromCard<ConvergingTides>()));
        
        __result = mutable;
    }

    private static async Task TakeAway(SpiralingWhirlpool @event)
    {
        if (@event.Owner == null)
            return;

        var card = @event.Owner.RunState.CreateCard<ConvergingTides>(@event.Owner);
        if (card != null)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
        }
        
        @event.SetEventFinished(@event.L10NLookup("SPIRALING_WHIRLPOOL.pages.TAKE_AWAY.description"));
    }
}