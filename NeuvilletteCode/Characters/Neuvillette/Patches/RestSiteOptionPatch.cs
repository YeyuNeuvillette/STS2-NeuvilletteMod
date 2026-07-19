using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Characters.Neuvillette.Relics;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(RestSiteOption), "Generate")]
internal static class RestSiteOptionPatch
{
    [HarmonyPostfix]
    public static void Postfix(Player player, List<RestSiteOption> __result)
    {
        if (player == null || __result == null)
            return;

        var braveTeaCup = player.Relics.FirstOrDefault(r => r is BraveTeaCup);

        if (braveTeaCup != null)
        {
            var healOption = __result.FirstOrDefault(o => o is HealRestSiteOption);
            if (healOption != null)
            {
                __result.Remove(healOption);
            }
        }
    }
}