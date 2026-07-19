using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
internal static class TemporaryMaxHpPowerPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState? combatState, ref Task __result)
    {
        if (combatState is not CombatState state)
            return;

        __result = CleanupAfterCombat(__result, state);
    }

    private static async Task CleanupAfterCombat(Task originalTask, CombatState combatState)
    {
        try
        {
            await originalTask;
        }
        finally
        {
            MelusineCardPool.CleanupCombat(combatState);
        }
    }
}
