using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
internal static class HealPatch
{
    [HarmonyPrefix]
    public static void Prefix(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature)
    {
        creature.GetPower<AssistArrestPower>()?.RecordHpBeforeHeal(creature.CurrentHp);
    }
}
