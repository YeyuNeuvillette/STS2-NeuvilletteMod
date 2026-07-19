using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using Neuvillette.Characters.Neuvillette.Features;
using Neuvillette.Infrastructure;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(NMultiplayerPlayerState), "UpdateHealthBarWidth")]
internal static class MultiplayerInfiniteHpBarPatch
{
    private const float StandardHealthBarWidth = 175f;

    public static bool Prefix(NMultiplayerPlayerState __instance)
    {
        Creature? creature = __instance.Player?.Creature;
        if (creature == null || !LeviathanHealthService.IsInfinite(creature))
            return true;

        NHealthBar? healthBar = GameCompatibility.GetMultiplayerHealthBar(__instance);

        if (healthBar == null)
            return true;

        float referenceHp = Math.Max(1f, (float)creature.MaxHp);
        healthBar.UpdateWidthRelativeToReferenceValue(referenceHp, StandardHealthBarWidth);
        return false;
    }
}
