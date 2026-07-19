using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Characters.Neuvillette.Relics;
using Neuvillette.Infrastructure;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(Neow), "get_AllPossibleOptions")]
internal static class NeowAllPossibleOptionsPatch
{
    [HarmonyPostfix]
    public static void Postfix(Neow __instance, ref IEnumerable<EventOption> __result)
    {
        var list = __result.ToList();
        if (list.Any(o => o.Relic is BraveTeaCup))
        {
            __result = list;
            return;
        }

        list.Add(__instance.RelicOption<BraveTeaCup>("INITIAL", "NEOW.pages.DONE.POSITIVE.description"));
        __result = list;
    }
}

[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
internal static class NeowGenerateInitialOptionsPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        var owner = __instance.Owner;
        if (!GameCompatibility.IsNeuvillette(owner) || owner == null)
            return true;

        if (owner.RunState.Modifiers.Count > 0)
            return true;

        var cursePool = __instance.CurseOptions.ToList();
        if (!cursePool.Any(o => o.Relic is BraveTeaCup))
            cursePool.Add(__instance.RelicOption<BraveTeaCup>("INITIAL", "NEOW.pages.DONE.POSITIVE.description"));

        cursePool.RemoveAll(o => o.Relic is LeafyPoultice);
        cursePool.RemoveAll(r => r.Relic != null && !r.Relic.IsAllowedAtNeow(owner));

        if (cursePool.Count == 0)
        {
            __result = Array.Empty<EventOption>();
            return false;
        }

        var thirdOption = __instance.Rng.NextItem(cursePool);

        if (thirdOption == null)
        {
            __result = Array.Empty<EventOption>();
            return false;
        }

        var positiveOptions = __instance.PositiveOptions.ToList();

        if (thirdOption.Relic is CursedPearl)
            positiveOptions.RemoveAll(o => o.Relic is GoldenPearl);
        if (thirdOption.Relic is HeftyTablet)
            positiveOptions.RemoveAll(o => o.Relic is ArcaneScroll);
        if (thirdOption.Relic is LeafyPoultice)
            positiveOptions.RemoveAll(o => o.Relic is NewLeaf);
        if (thirdOption.Relic is PrecariousShears)
            positiveOptions.RemoveAll(o => o.Relic is PreciseScissors);
        if (thirdOption.Relic is LargeCapsule)
        {
            if (__instance.Rng.NextBool())
                positiveOptions.Add(__instance.LavaRockOption);
            else
                positiveOptions.Add(__instance.SmallCapsuleOption);
        }

        positiveOptions.Add(__instance.StoneHumidifierOption);

        if (__instance.Rng.NextBool())
            positiveOptions.Add(__instance.NeowsTalismanOption);
        else
            positiveOptions.Add(__instance.PomanderOption);

        positiveOptions.RemoveAll(r => r.Relic != null && !r.Relic.IsAllowedAtNeow(owner));

        var finalOptions = new List<EventOption>();
        finalOptions.AddRange(positiveOptions.UnstableShuffle(__instance.Rng).Take(2));
        finalOptions.Add(thirdOption);

        __result = finalOptions;
        return false;
    }
}
