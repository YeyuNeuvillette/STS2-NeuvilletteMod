using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(Symbiote), "GenerateInitialOptions")]
internal static class SymbiotePatch
{
    [HarmonyPostfix]
    public static void Postfix(Symbiote __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var killWithFireOption = mutable.FirstOrDefault(o => o.TextKey == "SYMBIOTE.pages.INITIAL.options.KILL_WITH_FIRE");
        if (killWithFireOption != null)
        {
            mutable.Remove(killWithFireOption);
        }
        
        mutable.Add(new EventOption(__instance, () => Purify(__instance), "SYMBIOTE.pages.INITIAL.options.PURIFY"));
        
        __result = mutable;
    }

    private static async Task Purify(Symbiote @event)
    {
        if (@event.Owner == null)
            return;

        var cardModel = (await CardSelectCmd.FromDeckForUpgrade(@event.Owner, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (cardModel != null)
        {
            CardCmd.Upgrade(cardModel, CardPreviewStyle.EventLayout);
        }
        
        @event.SetEventFinished(@event.L10NLookup("SYMBIOTE.pages.PURIFY.description"));
    }
}