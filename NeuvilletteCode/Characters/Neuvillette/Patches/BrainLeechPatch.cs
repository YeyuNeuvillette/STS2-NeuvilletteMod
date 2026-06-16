using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Linq;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(BrainLeech), "GenerateInitialOptions")]
public static class BrainLeechPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(BrainLeech __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var shareKnowledgeOption = mutable.FirstOrDefault(o => o.TextKey == "BRAIN_LEECH.pages.INITIAL.options.SHARE_KNOWLEDGE");
        if (shareKnowledgeOption != null)
        {
            mutable.Remove(shareKnowledgeOption);
        }
        
        mutable.Add(new EventOption(__instance, () => VastKnowledge(__instance), "BRAIN_LEECH.pages.INITIAL.options.VAST_KNOWLEDGE"));
        
        __result = mutable;
    }

    private static async Task VastKnowledge(BrainLeech @event)
    {
        if (@event.Owner == null)
            return;

        Player owner = @event.Owner;
        CardCreationOptions options = CardCreationOptions.ForNonCombatWithDefaultOdds(new List<CardPoolModel> { owner.Character.CardPool });
        List<CardCreationResult> cards = CardFactory.CreateForReward(owner, 10, options).ToList();
        CardSelectorPrefs prefs = new CardSelectorPrefs(@event.L10NLookup("BRAIN_LEECH.pages.VAST_KNOWLEDGE.selectionScreenPrompt"), 1)
        {
            Cancelable = false
        };
        CardModel? cardModel = (await CardSelectCmd.FromSimpleGridForRewards(new BlockingPlayerChoiceContext(), cards, @event.Owner, prefs)).FirstOrDefault();
        if (cardModel != null)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(cardModel, PileType.Deck));
        }
        @event.SetEventFinished(@event.L10NLookup("BRAIN_LEECH.pages.VAST_KNOWLEDGE.description"));
    }
}