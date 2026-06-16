using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette.Relics;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(StoneOfAllTime), "GenerateInitialOptions")]
public class StoneOfAllTimePatch
{
    [HarmonyPostfix]
    public static void Postfix(StoneOfAllTime __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
            return;

        if (__instance.Owner.Character?.Id.Entry != "NEUVILLETTE_CHARACTER_NEUVILLETTE")
            return;

        var mutable = __result as List<EventOption> ?? __result.ToList();
        
        mutable.Clear();
        var relicHoverTip = HoverTipFactory.FromRelic<EternalStone>();
        mutable.Add(new EventOption(__instance, () => TakeStone(__instance), "STONE_OF_ALL_TIME.pages.INITIAL.options.TAKE_STONE", relicHoverTip)
                .ThatDecreasesMaxHp(6m));
        mutable.Add(new EventOption(__instance, () => Leave(__instance), "STONE_OF_ALL_TIME.pages.INITIAL.options.LEAVE"));
        
        __result = mutable;
    }

    private static async Task TakeStone(StoneOfAllTime @event)
    {
        if (@event.Owner == null)
            return;

        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), @event.Owner.Creature, 6m, isFromCard: false);
        await RelicCmd.Obtain<EternalStone>(@event.Owner);
        
        @event.SetEventFinished(@event.L10NLookup("STONE_OF_ALL_TIME.pages.TAKE_STONE.description"));
    }

    private static async Task Leave(StoneOfAllTime @event)
    {
        if (@event.Owner == null)
            return;

        await CreatureCmd.GainMaxHp(@event.Owner.Creature, 4m);
        
        @event.SetEventFinished(@event.L10NLookup("STONE_OF_ALL_TIME.pages.LEAVE.description"));
    }
}