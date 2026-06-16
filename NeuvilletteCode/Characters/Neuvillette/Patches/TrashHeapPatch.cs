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

[HarmonyPatch(typeof(TrashHeap), "GenerateInitialOptions")]
public static class TrashHeapPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(TrashHeap __instance, ref IReadOnlyList<EventOption> __result)
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
        
        mutable.Add(new EventOption(__instance, () => ThrowAwayTrash(__instance), "TRASH_HEAP.pages.INITIAL.options.THROW_AWAY_TRASH"));
        
        __result = mutable;
    }

    private static async Task ThrowAwayTrash(TrashHeap @event)
    {
        if (@event.Owner == null)
            return;

        var card = (await CardSelectCmd.FromDeckForRemoval(@event.Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
        
        @event.SetEventFinished(@event.L10NLookup("TRASH_HEAP.pages.THROW_AWAY_TRASH.description"));
    }
}