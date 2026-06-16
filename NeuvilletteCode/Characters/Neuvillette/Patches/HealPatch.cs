using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
public static class HealPatch
{
    public static readonly Dictionary<Creature, decimal> HpBeforeHeal = [];

    [HarmonyPrefix]
    public static void Prefix(Creature creature)
    {
        if (creature.GetPower<AssistArrestPower>() != null)
            HpBeforeHeal[creature] = creature.CurrentHp;
    }
}