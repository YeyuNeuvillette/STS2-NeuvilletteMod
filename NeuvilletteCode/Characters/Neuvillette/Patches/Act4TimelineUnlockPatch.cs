using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;

namespace Neuvillette.Characters.Neuvillette.Patches;

/// <summary>
/// Unlock the Act 4 setting at the same moment chapter 7 becomes canonical
/// timeline progress.  QueueUnlocks runs later in the inspect screen and is not
/// a reliable persistence boundary (for example, the screen may be interrupted).
/// </summary>
[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.RevealEpoch))]
internal static class Act4TimelineUnlockPatch
{
    [HarmonyPostfix]
    private static void AfterRevealEpoch(string epochId)
    {
        if (string.Equals(epochId, "NEUVILLETTE7_EPOCH", StringComparison.Ordinal))
            NeuvilletteSettingsStore.UnlockAct4();
    }
}
