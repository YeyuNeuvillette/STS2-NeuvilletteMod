using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(SunkenTreasury), "GenerateInitialOptions")]
internal static class SunkenTreasuryPatch
{
    [HarmonyPostfix]
    public static void Postfix(SunkenTreasury __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var secondChestOption = mutable.FirstOrDefault(o => o.TextKey == "SUNKEN_TREASURY.pages.INITIAL.options.SECOND_CHEST");
        if (secondChestOption != null)
        {
            mutable.Remove(secondChestOption);
        }
        
        mutable.Add(new EventOption(__instance, () => Leave(__instance), "SUNKEN_TREASURY.pages.INITIAL.options.LEAVE"));
        
        __result = mutable;
    }

    private static async Task Leave(SunkenTreasury @event)
    {
        if (@event.Owner == null)
            return;

        var cardModel = (await CardSelectCmd.FromDeckForUpgrade(@event.Owner, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (cardModel != null)
        {
            CardCmd.Upgrade(cardModel);
        }
        
        @event.SetEventFinished(@event.L10NLookup("SUNKEN_TREASURY.pages.LEAVE.description"));
    }
}