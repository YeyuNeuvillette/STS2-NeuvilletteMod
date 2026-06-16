using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using Neuvillette.Characters.Neuvillette.Relics;
using System.Collections.Generic;
using System.Linq;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(SpiritGrafter), "GenerateInitialOptions")]
public static class SpiritGrafterPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(SpiritGrafter __instance, ref IReadOnlyList<EventOption> __result)
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
        
        mutable = mutable.Where(option => option.TextKey != "SPIRIT_GRAFTER.pages.INITIAL.options.LET_IT_IN").ToList();
        
        var relicHoverTip = HoverTipFactory.FromRelic<SoulGraftRelic>();
        mutable.Add(new EventOption(__instance, () => Imprison(__instance), "SPIRIT_GRAFTER.pages.INITIAL.options.IMPRISON", relicHoverTip));
        
        __result = mutable;
    }

    private static async Task Imprison(SpiritGrafter @event)
    {
        if (@event.Owner == null)
            return;

        await RelicCmd.Obtain<SoulGraftRelic>(@event.Owner);
        
        @event.SetEventFinished(@event.L10NLookup("SPIRIT_GRAFTER.pages.IMPRISON.description"));
    }
}