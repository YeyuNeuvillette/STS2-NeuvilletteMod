using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(UnrestSite), "GenerateInitialOptions")]
internal static class UnrestSitePatch
{
    [HarmonyPostfix]
    public static void Postfix(UnrestSite __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var killOption = mutable.FirstOrDefault(o => o.TextKey == "UNREST_SITE.pages.INITIAL.options.KILL");
        if (killOption != null)
        {
            mutable.Remove(killOption);
        }
        
        var cardHoverTip = HoverTipFactory.FromCardWithCardHoverTips<Peck>();
        mutable.Add(new EventOption(__instance, () => RescueHostage(__instance), "UNREST_SITE.pages.INITIAL.options.RESCUE_HOSTAGE", cardHoverTip));
        
        __result = mutable;
    }

    private static async Task RescueHostage(UnrestSite @event)
    {
        if (@event.Owner == null)
            return;

        var cardModel = (await CardSelectCmd.FromDeckForTransformation(@event.Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1))).FirstOrDefault();
        if (cardModel != null)
        {
            await CardCmd.TransformTo<Peck>(cardModel, CardPreviewStyle.EventLayout);
        }
        
        @event.SetEventFinished(@event.L10NLookup("UNREST_SITE.pages.RESCUE_HOSTAGE.description"));
    }
}