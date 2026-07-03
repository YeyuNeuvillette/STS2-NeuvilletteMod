using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class TemporaryMaxHpPowerPatch
{
    [HarmonyPriority(Priority.First)]
    public static async void Prefix(IRunState runState, CombatState? combatState, CombatRoom room)
    {
        if (combatState == null)
            return;

        foreach (var player in combatState.Players)
        {
            Creature owner = player.Creature;

            var leviathanFormPower = owner.Powers.OfType<LeviathanFormPower>().FirstOrDefault();
            if (leviathanFormPower != null)
                await leviathanFormPower.AfterCombatEnd(room);

            var temporaryMaxHpPower = owner.Powers.OfType<TemporaryMaxHpPower>().FirstOrDefault();
            if (temporaryMaxHpPower != null)
                await temporaryMaxHpPower.AfterCombatEnd(room);

            MelusineCardPool.CleanupCombat(combatState);
        }
    }
}