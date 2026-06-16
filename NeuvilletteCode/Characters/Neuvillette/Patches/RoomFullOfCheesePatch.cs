using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(RoomFullOfCheese), "GenerateInitialOptions")]
public static class RoomFullOfCheesePatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(RoomFullOfCheese __instance, ref IReadOnlyList<EventOption> __result)
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
        
        var gorgeOption = mutable.FirstOrDefault(o => o.TextKey == "ROOM_FULL_OF_CHEESE.pages.INITIAL.options.GORGE");
        if (gorgeOption != null)
        {
            mutable.Remove(gorgeOption);
        }
        
        if (!__instance.DynamicVars.ContainsKey("PoliteTasteGold"))
        {
            var politeTasteGold = new GoldVar("PoliteTasteGold", 0);
            var varsField = typeof(DynamicVarSet).GetField("_vars", BindingFlags.NonPublic | BindingFlags.Instance);
            if (varsField != null)
            {
                var varsDict = varsField.GetValue(__instance.DynamicVars) as Dictionary<string, DynamicVar>;
                if (varsDict != null)
                {
                    varsDict["PoliteTasteGold"] = politeTasteGold;
                    politeTasteGold.SetOwner(__instance);
                }
            }
        }

        __instance.DynamicVars["PoliteTasteGold"].BaseValue = __instance.Rng.NextInt(47, 100);
        
        var requiredGold = __instance.DynamicVars["PoliteTasteGold"].IntValue;
        if (__instance.Owner.Gold >= requiredGold)
        {
            mutable.Add(new EventOption(__instance, () => PoliteTaste(__instance), "ROOM_FULL_OF_CHEESE.pages.INITIAL.options.POLITE_TASTE"));
        }
        else
        {
            mutable.Add(new EventOption(__instance, null, "ROOM_FULL_OF_CHEESE.pages.INITIAL.options.POLITE_TASTE_LOCKED"));
        }
        
        __result = mutable;
    }

    private static async Task PoliteTaste(RoomFullOfCheese @event)
    {
        if (@event.Owner == null)
            return;

        var goldLoss = @event.DynamicVars["PoliteTasteGold"].IntValue;
        await PlayerCmd.LoseGold(goldLoss, @event.Owner, GoldLossType.Spent);
        
        var owner = @event.Owner;
        var options = CardCreationOptions.ForNonCombatWithDefaultOdds(new List<CardPoolModel> { owner.Character.CardPool });
        
        var rewards = new List<Reward>
        {
            new CardReward(options, 3, owner),
            new CardReward(options, 3, owner)
        };
        
        await RewardsCmd.OfferCustom(owner, rewards);
        
        @event.SetEventFinished(@event.L10NLookup("ROOM_FULL_OF_CHEESE.pages.POLITE_TASTE.description"));
    }
}