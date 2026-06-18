using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Characters.Neuvillette.Relics;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

/// <summary>
///     Modifies Neow event options for Neuvillette character:
///     - Replaces the curse relic pool (third option) with one that excludes LeafyPoultice and PrecariousShears, and includes BraveTeaCup
///     - Reduces the probability of NutritiousOyster and StoneHumidifier by 40%
/// </summary>
[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
public static class NeowEventPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPrefix]
    public static bool Prefix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        Logger.Info("NeowEventPatch.Prefix called");

        if (__instance.Owner?.Character?.Id.Entry != "NEUVILLETTE_CHARACTER_NEUVILLETTE")
        {
            Logger.Info($"Character is not Neuvillette, skipping patch. Character: {__instance.Owner?.Character?.Id.Entry}");
            return true;
        }

        Logger.Info("Character is Neuvillette, applying patch");

        if (__instance.Owner.RunState.Modifiers.Count > 0)
        {
            Logger.Info("Modifiers present, skipping patch");
            return true;
        }

        Logger.Info("No modifiers, generating custom options");

        var curseOptions = new List<EventOption>
        {
            __instance.RelicOption<CursedPearl>("INITIAL", "NEOW.pages.DONE.CURSED.description"),
            __instance.RelicOption<HeftyTablet>("INITIAL", "NEOW.pages.DONE.CURSED.description"),
            __instance.RelicOption<LargeCapsule>("INITIAL", "NEOW.pages.DONE.CURSED.description"),
            __instance.RelicOption<SilverCrucible>("INITIAL", "NEOW.pages.DONE.CURSED.description"),
            __instance.RelicOption<NeowsBones>("INITIAL", "NEOW.pages.DONE.POSITIVE.description"),
            __instance.RelicOption<BraveTeaCup>("INITIAL", "NEOW.pages.DONE.POSITIVE.description")
        };

        curseOptions.RemoveAll(r => r.Relic != null && !r.Relic.IsAllowed(__instance.Owner.RunState));

        Logger.Info($"Curse options count after filtering: {curseOptions.Count}");

        if (curseOptions.Count == 0)
        {
            Logger.Warn("No curse options available, returning empty list");
            __result = Array.Empty<EventOption>();
            return false;
        }

        var thirdOption = __instance.Rng.NextItem(curseOptions);

        if (thirdOption == null || thirdOption.Relic == null)
        {
            Logger.Warn("Third option is null, returning empty list");
            __result = Array.Empty<EventOption>();
            return false;
        }

        Logger.Info($"Third option selected: {thirdOption.Relic?.Id.Entry}");

        var positiveOptions = __instance.PositiveOptions.ToList();

        if (thirdOption.Relic is CursedPearl)
        {
            positiveOptions.RemoveAll(o => o.Relic is GoldenPearl);
        }
        if (thirdOption.Relic is HeftyTablet)
        {
            positiveOptions.RemoveAll(o => o.Relic is ArcaneScroll);
        }
        if (thirdOption.Relic is LargeCapsule)
        {
            if (__instance.Rng.NextBool())
            {
                positiveOptions.Add(__instance.LavaRockOption);
            }
            else
            {
                positiveOptions.Add(__instance.SmallCapsuleOption);
            }
        }

        if (__instance.Rng.NextFloat() < 0.6f)
        {
            if (__instance.Rng.NextBool())
            {
                positiveOptions.Add(__instance.NutritiousOysterOption);
            }
            else
            {
                positiveOptions.Add(__instance.StoneHumidifierOption);
            }
        }

        if (__instance.Rng.NextBool())
        {
            positiveOptions.Add(__instance.NeowsTalismanOption);
        }
        else
        {
            positiveOptions.Add(__instance.PomanderOption);
        }

        positiveOptions.RemoveAll(r => r.Relic != null && !r.Relic.IsAllowed(__instance.Owner.RunState));

        Logger.Info($"Positive options count after filtering: {positiveOptions.Count}");

        var finalOptions = new List<EventOption>();
        finalOptions.AddRange(positiveOptions.UnstableShuffle(__instance.Rng).Take(2));
        finalOptions.Add(thirdOption);

        Logger.Info($"Final options count: {finalOptions.Count}");
        foreach (var option in finalOptions)
        {
            Logger.Info($"Final option: {option.Relic?.Id.Entry}");
        }

        __result = finalOptions;
        return false;
    }
}

/// <summary>
///     Adds BraveTeaCup to Neow's AllPossibleOptions so it can appear in the relic pool.
/// </summary>
[HarmonyPatch(typeof(Neow), "get_AllPossibleOptions")]
public static class NeowAllPossibleOptionsPatch
{
    [HarmonyPostfix]
    public static void Postfix(Neow __instance, ref IEnumerable<EventOption> __result)
    {
        if (__instance.Owner?.Character?.Id.Entry != "NEUVILLETTE_CHARACTER_NEUVILLETTE")
        {
            return;
        }

        var list = __result.ToList();
        if (list.Any(o => o.Relic is BraveTeaCup))
        {
            __result = list;
            return;
        }

        var relicOptionMethod = AccessTools.Method(
            typeof(MegaCrit.Sts2.Core.Models.AncientEventModel),
            nameof(MegaCrit.Sts2.Core.Models.AncientEventModel.RelicOption),
            System.Type.EmptyTypes);

        if (relicOptionMethod == null)
        {
            relicOptionMethod = AccessTools.Method(
                typeof(MegaCrit.Sts2.Core.Models.AncientEventModel),
                "RelicOption",
                new[] { typeof(string), typeof(string) });
        }

        var genericMethod = relicOptionMethod?.MakeGenericMethod(typeof(BraveTeaCup));
        var option = (EventOption?)genericMethod?.Invoke(__instance, new object?[] { "INITIAL", "NEOW.pages.DONE.POSITIVE.description" });

        if (option != null)
        {
            list.Add(option);
        }

        __result = list;
    }
}