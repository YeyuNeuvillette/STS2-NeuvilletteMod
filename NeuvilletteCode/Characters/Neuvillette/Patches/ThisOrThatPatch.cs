using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(ThisOrThat), "CalculateVars")]
public static class ThisOrThatCalculateVarsPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(ThisOrThat __instance)
    {
        if (__instance.Owner == null)
        {
            return;
        }

        if (__instance.Owner.Character?.Id.Entry != "NEUVILLETTE_CHARACTER_NEUVILLETTE")
        {
            return;
        }

        if (!__instance.DynamicVars.ContainsKey("ScoldGold"))
        {
            var scoldGold = new GoldVar("ScoldGold", 0);
            var varsField = typeof(DynamicVarSet).GetField("_vars", BindingFlags.NonPublic | BindingFlags.Instance);
            if (varsField != null)
            {
                var varsDict = varsField.GetValue(__instance.DynamicVars) as Dictionary<string, DynamicVar>;
                if (varsDict != null)
                {
                    varsDict["ScoldGold"] = scoldGold;
                    scoldGold.SetOwner(__instance);
                }
            }
        }

        __instance.DynamicVars["ScoldGold"].BaseValue = __instance.Rng.NextInt(10, 21);
    }
}

[HarmonyPatch(typeof(ThisOrThat), "GenerateInitialOptions")]
public static class ThisOrThatPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(ThisOrThat __instance, ref IReadOnlyList<EventOption> __result)
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
        mutable.Add(new EventOption(__instance, () => ScoldAway(__instance), "THIS_OR_THAT.pages.INITIAL.options.SCOLD_AWAY"));
        __result = mutable;
    }

    private static async Task ScoldAway(ThisOrThat @event)
    {
        if (@event.Owner == null)
            return;

        var goldAmount = @event.DynamicVars["ScoldGold"].IntValue;
        await PlayerCmd.GainGold(goldAmount, @event.Owner);
        @event.SetEventFinished(@event.L10NLookup("THIS_OR_THAT.pages.SCOLD_AWAY.description"));
    }
}