using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using Neuvillette.Characters.Neuvillette.Relics;
using System.Collections.Generic;
using System.Linq;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(PotionCourier), "GenerateInitialOptions")]
public static class PotionCourierPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(PotionCourier __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var ransackOption = mutable.FirstOrDefault(o => o.TextKey == "POTION_COURIER.pages.INITIAL.options.RANSACK");
        if (ransackOption != null)
        {
            mutable.Remove(ransackOption);
        }
        
        var potionHoverTip = HoverTipFactory.FromPotion<FoulPotion>();
        var relicHoverTip = HoverTipFactory.FromRelic<PotionSpirit>();
        var combinedHoverTips = new[] { potionHoverTip }.Concat(relicHoverTip);
        mutable.Add(new EventOption(__instance, () => Bury(__instance), "POTION_COURIER.pages.INITIAL.options.BURY", combinedHoverTips));
        
        __result = mutable;
    }

    private static async Task Bury(PotionCourier @event)
    {
        if (@event.Owner == null)
            return;

        var list = new List<Reward>();
        list.Add(new PotionReward(ModelDb.Potion<FoulPotion>().ToMutable(), @event.Owner));
        await RewardsCmd.OfferCustom(@event.Owner, list);
        await RelicCmd.Obtain<PotionSpirit>(@event.Owner);
        
        @event.SetEventFinished(@event.L10NLookup("POTION_COURIER.pages.BURY.description"));
    }
}