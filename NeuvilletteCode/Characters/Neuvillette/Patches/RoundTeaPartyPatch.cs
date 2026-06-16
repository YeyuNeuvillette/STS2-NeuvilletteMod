using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette.Relics;
using System.Collections.Generic;
using System.Linq;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(RoundTeaParty), "GenerateInitialOptions")]
public static class RoundTeaPartyPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(RoundTeaParty __instance, ref IReadOnlyList<EventOption> __result)
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
        
        mutable.Clear();
        
        var relicHoverTip = HoverTipFactory.FromRelic<ShatteredCrown>();
        
        mutable.Add(new EventOption(__instance, () => DrinkGoodTea(__instance), "ROUND_TEA_PARTY.pages.INITIAL.options.DRINK_GOOD_TEA").ThatDoesDamage(8m));
        mutable.Add(new EventOption(__instance, () => AccuseMurder(__instance), "ROUND_TEA_PARTY.pages.INITIAL.options.ACCUSE_MURDER", relicHoverTip));
        
        __result = mutable;
    }

    private static async Task DrinkGoodTea(RoundTeaParty @event)
    {
        if (@event.Owner == null)
            return;

        var targetCreature = @event.Owner.Creature;
        await CreatureCmd.Heal(targetCreature, targetCreature.MaxHp - targetCreature.CurrentHp);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), targetCreature, 8m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        
        @event.SetEventFinished(@event.L10NLookup("ROUND_TEA_PARTY.pages.DRINK_GOOD_TEA.description"));
    }

    private static async Task AccuseMurder(RoundTeaParty @event)
    {
        if (@event.Owner == null)
            return;

        var targetCreature = @event.Owner.Creature;
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), targetCreature, 12m, false);
        await RelicCmd.Obtain<ShatteredCrown>(@event.Owner);
        
        @event.SetEventFinished(@event.L10NLookup("ROUND_TEA_PARTY.pages.ACCUSE_MURDER.description"));
    }
}