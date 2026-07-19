using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using Neuvillette.Features.Act4;
using Neuvillette.Infrastructure;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch]
internal static class NeuvilletteActPatch
{
    [HarmonyPatch(typeof(StandardActMap), "AssignPointTypes")]
    [HarmonyPostfix]
    public static void Postfix_Map(StandardActMap __instance)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return;
        var state = GameCompatibility.GetRunState();
        if (state != null)
            Act4MapService.TryApplyLinearLayout(__instance, state);
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
    [HarmonyPostfix]
    public static void Postfix_GenerateRooms(RunManager __instance)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return;
        var state = GameCompatibility.GetRunState(__instance);
        if (state != null)
            Act4RoomService.ConfigureDoubleBoss(__instance, state);
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
    [HarmonyPostfix]
    public static void Postfix_Rooms(ActModel __instance)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return;
        Act4RoomService.TryConfigureNeuvilletteRooms(__instance);
    }

    [HarmonyPatch(typeof(NRewardsScreen), "ShowScreen")]
    [HarmonyPrefix]
    public static bool Prefix_ShowScreen(RewardsSet set, bool isTerminal, IRunState runState)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return true;
        return !Act4RewardService.TryHandleBossRewardScreen(set, isTerminal, runState);
    }

    [HarmonyPatch(typeof(NCombatBackground), nameof(NCombatBackground.Create))]
    [HarmonyPrefix]
    public static bool Prefix_CreateCombatBg(BackgroundAssets bg, ref NCombatBackground __result)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return true;
        return !Act4SceneService.TryCreateCombatBackground(bg, ref __result);
    }

    [HarmonyPatch(typeof(NRunMusicController), "UpdateMusic")]
    [HarmonyPrefix]
    public static bool Prefix_Music()
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return true;
        return !Act4CompatibilityService.ShouldSuppressMusic();
    }

    [HarmonyPatch(typeof(TreasureRoom), MethodType.Constructor, typeof(int))]
    [HarmonyPrefix]
    public static void Prefix_TreasureRoom(ref int actIndex)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return;
        actIndex = Act4CompatibilityService.ClampActIndex(actIndex);
    }

    [HarmonyPatch(typeof(MultiplayerScalingModel), nameof(MultiplayerScalingModel.GetMultiplayerScaling))]
    [HarmonyPrefix]
    public static bool Prefix_GetMultiplayerScaling(EncounterModel? encounter, ref int actIndex, ref decimal __result)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return true;
        actIndex = Act4CompatibilityService.ClampActIndex(actIndex);
        return true;
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
    [HarmonyPrefix]
    public static void Prefix_EnterNextAct(RunManager __instance)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return;
        var state = GameCompatibility.GetRunState(__instance);
        if (state != null)
            Act4CompatibilityService.TryTruncateActsForNonSwordHolders(state);
    }
}
