using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Characters.Neuvillette.Relics;
using System.Collections.Generic;
using System.Linq;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(DrowningBeacon), "GenerateInitialOptions")]
public static class DrowningBeaconPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(DrowningBeacon __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var climbOption = mutable.FirstOrDefault(o => o.TextKey == "DROWNING_BEACON.pages.INITIAL.options.CLIMB");
        if (climbOption != null)
        {
            mutable.Remove(climbOption);
        }
        
        mutable.Add(new EventOption(__instance, () => Salute(__instance), "DROWNING_BEACON.pages.INITIAL.options.SALUTE").ThatDecreasesMaxHp(6));
        
        __result = mutable;
    }

    private static async Task Salute(DrowningBeacon @event)
    {
        if (@event.Owner == null)
            return;

        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), @event.Owner.Creature, 6, isFromCard: false);
        
        var waterFairyRelics = new List<RelicModel>
        {
            ModelDb.Relic<Monocle>().ToMutable(),
            ModelDb.Relic<Narcissus>().ToMutable(),
            ModelDb.Relic<Plumule>().ToMutable(),
            ModelDb.Relic<StoppedPocketWatch>().ToMutable()
        };
        
        var randomRelic = @event.Owner.PlayerRng.Rewards.NextItem(waterFairyRelics);
        if (randomRelic != null)
        {
            await RelicCmd.Obtain(randomRelic, @event.Owner);
        }
        
        @event.SetEventFinished(@event.L10NLookup("DROWNING_BEACON.pages.SALUTE.description"));
    }
}