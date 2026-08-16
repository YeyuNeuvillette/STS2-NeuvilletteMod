using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using Neuvillette.Characters.Neuvillette.Timeline;

namespace Neuvillette.Characters.Neuvillette.Patches;

/// <summary>
/// The base game's UnlockState only contains explicit checks for its four
/// built-in character epochs.  Mod characters therefore need to contribute
/// their own first-epoch gate to the character-select unlock set.
/// </summary>
[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.Characters), MethodType.Getter)]
internal static class NeuvilletteCharacterUnlockPatch
{
    [HarmonyPostfix]
    private static void FilterLockedNeuvillette(UnlockState __instance, ref IEnumerable<CharacterModel> __result)
    {
        if (!__instance.IsEpochRevealed<Neuvillette1Epoch>())
            __result = __result.Where(character => character is not Neuvillette).ToList();
    }
}
