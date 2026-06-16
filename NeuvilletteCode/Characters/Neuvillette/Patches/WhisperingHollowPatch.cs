using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Characters.Neuvillette.Relics;
using System.Collections.Generic;
using System.Linq;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(WhisperingHollow), "GenerateInitialOptions")]
public static class WhisperingHollowPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(WhisperingHollow __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var relicHoverTip = HoverTipFactory.FromRelic<CreepyBranch>();
        mutable.Add(new EventOption(__instance, () => GiveLife(__instance), "WHISPERING_HOLLOW.pages.INITIAL.options.GIVE_LIFE", relicHoverTip));
        
        __result = mutable;
    }

    private static async Task GiveLife(WhisperingHollow @event)
    {
        if (@event.Owner == null)
            return;

        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), @event.Owner.Creature, 11m, isFromCard: false);
        await RelicCmd.Obtain<CreepyBranch>(@event.Owner);
        
        @event.SetEventFinished(@event.L10NLookup("WHISPERING_HOLLOW.pages.GIVE_LIFE.description"));
    }
}