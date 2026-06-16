using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Relics;
using Neuvillette.Characters.Neuvillette.Relics;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette.Characters.Neuvillette.Patches;

/// <summary>
///     Patches RestSiteOption.Generate to remove HealRestSiteOption for players with BraveTeaCup relic
/// </summary>
[HarmonyPatch(typeof(RestSiteOption), "Generate")]
public static class RestSiteOptionPatch
{
    private static readonly Logger Logger = new("Neuvillette", LogType.Generic);

    [HarmonyPostfix]
    public static void Postfix(Player player, List<RestSiteOption> __result)
    {
        if (player == null || __result == null)
            return;

        var braveTeaCup = player.Relics.FirstOrDefault(r => r is BraveTeaCup);

        if (braveTeaCup != null)
        {
            Logger.Info("Player has BraveTeaCup, removing HealRestSiteOption");

            var healOption = __result.FirstOrDefault(o => o is HealRestSiteOption);
            if (healOption != null)
            {
                __result.Remove(healOption);
                Logger.Info("HealRestSiteOption removed");
            }
        }
    }
}