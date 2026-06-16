using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(NHealthBar), "RefreshText")]
public static class NHealthBarPatch
{
    public static void Postfix(NHealthBar __instance)
    {
        var creature = Traverse.Create(__instance).Field<Creature>("_creature").Value;
        if (creature == null || creature.HpDisplay != HpDisplay.Normal)
            return;

        var infinityTex = Traverse.Create(__instance).Field<TextureRect>("_infinityTex").Value;
        var hpLabel = Traverse.Create(__instance).Field<MegaLabel>("_hpLabel").Value;

        if (infinityTex != null)
            infinityTex.Visible = false;

        if (hpLabel != null)
        {
            hpLabel.Visible = true;
            hpLabel.SetTextAutoSize($"{creature.CurrentHp}/{creature.MaxHp}");
        }
    }
}