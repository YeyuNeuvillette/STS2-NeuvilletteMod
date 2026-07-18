using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(NMultiplayerPlayerState), "UpdateHealthBarWidth")]
public static class MultiplayerInfiniteHpBarPatch
{
    private const float StandardHealthBarWidth = 175f;

    public static bool Prefix(NMultiplayerPlayerState __instance)
    {
        Creature? creature = __instance.Player?.Creature;
        if (creature?.HpDisplay == HpDisplay.Normal)
            return true;

        NHealthBar? healthBar = Traverse.Create(__instance)
            .Field<NHealthBar>("_healthBar")
            .Value;

        if (creature == null || healthBar == null)
            return true;

        float referenceHp = Math.Max(1f, (float)creature.MaxHp);
        healthBar.UpdateWidthRelativeToReferenceValue(referenceHp, StandardHealthBarWidth);
        return false;
    }
}
