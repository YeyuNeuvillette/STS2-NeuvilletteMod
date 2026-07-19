using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Neuvillette.Characters.Neuvillette.Cards;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(InfestedAutomaton), "GenerateInitialOptions")]
internal static class InfestedAutomatonPatch
{
    [HarmonyPostfix]
    public static void Postfix(InfestedAutomaton __instance, ref IReadOnlyList<EventOption> __result)
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
        
        mutable = mutable.Where(option => option.TextKey != "INFESTED_AUTOMATON.pages.INITIAL.options.TOUCH_CORE").ToList();
        
        var cardHoverTip = HoverTipFactory.FromCard<Rebirth>();
        mutable.Add(new EventOption(__instance, () => Reshape(__instance), "INFESTED_AUTOMATON.pages.INITIAL.options.RESHAPE", cardHoverTip));
        
        __result = mutable;
    }

    private static async Task Reshape(InfestedAutomaton @event)
    {
        if (@event.Owner == null)
            return;

        CardModel cardModel = @event.Owner.RunState.CreateCard<Rebirth>(@event.Owner);
        
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(cardModel, PileType.Deck), 1.2f, CardPreviewStyle.EventLayout);
        
        @event.SetEventFinished(@event.L10NLookup("INFESTED_AUTOMATON.pages.RESHAPE.description"));
    }
}