using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
public static class DustyTomePatch
{
    public static void Postfix(DustyTome __instance, Player player)
    {
        if (player.Character.Id.ToString() == "NEUVILLETTE-NEUVILLETTE")
            __instance.AncientCard = ModelDb.Card<AnnounceNoCrime>().Id;
    }
}
