using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Characters.Neuvillette.Relics;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(UnknownMapPointOdds), "Roll")]
public static class ExcuseNotePatch
{
    [HarmonyPostfix]
    public static void Postfix(UnknownMapPointOdds __instance, IEnumerable<RoomType> blacklist, IRunState runState, ref RoomType __result)
    {
        if (runState == null)
            return;

        var excuseNote = runState.Players
            .SelectMany(p => p.Relics)
            .FirstOrDefault(r => r is ExcuseNote);

        if (excuseNote != null)
        {
            if (runState.Rng.UnknownMapPoint.NextFloat() < 0.35f)
            {
                __result = RoomType.RestSite;
            }
        }
    }
}