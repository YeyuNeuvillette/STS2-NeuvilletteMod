using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
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

[HarmonyPatch(typeof(AromaOfChaos), "GenerateInitialOptions")]
public static class AromaOfChaosPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(AromaOfChaos __instance, ref IReadOnlyList<EventOption> __result)
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
        
        mutable.Add(new EventOption(__instance, () => WashAwayChaos(__instance), "AROMA_OF_CHAOS.pages.INITIAL.options.WASH_AWAY_CHAOS"));
        
        __result = mutable;
    }

    private static async Task WashAwayChaos(AromaOfChaos @event)
    {
        if (@event.Owner == null)
            return;

        var card = (await CardSelectCmd.FromDeckForRemoval(@event.Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
        
        @event.SetEventFinished(@event.L10NLookup("AROMA_OF_CHAOS.pages.WASH_AWAY_CHAOS.description"));
    }
}