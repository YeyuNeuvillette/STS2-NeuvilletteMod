using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Features.Difficulty;

internal static class OnboardingCharacterSelectUi
{
    private const string ToggleScenePath = MainFile.ResPath + "/scenes/ui/onboarding_difficulty_toggle.tscn";

    private sealed class ScreenState
    {
        public Control? Panel { get; set; }
        public bool TutorialPending { get; set; }
        public bool IsEligibleSelection { get; set; }
    }

    private static readonly ConditionalWeakTable<NCharacterSelectScreen, ScreenState> States = new();

    internal static void Attach(NCharacterSelectScreen screen)
    {
        ScreenState state = States.GetOrCreateValue(screen);
        if (state.Panel is not null && GodotObject.IsInstanceValid(state.Panel))
            return;

        PackedScene? scene = ResourceLoader.Load<PackedScene>(ToggleScenePath, null, ResourceLoader.CacheMode.Reuse);
        if (scene is null)
        {
            MainFile.Logger.Error($"Could not load Judicial Guidance character-select UI: {ToggleScenePath}");
            return;
        }

        state.Panel = scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        state.Panel.Visible = false;
        screen.AddChildSafely(state.Panel);
        state.Panel.GetNode<MegaLabel>("%DifficultyLabel").SetTextAutoSize(
            NeuvilletteSettingsStore.Localization.Get(
                "neuvillette.character_select.difficulty.label",
                "Judicial Guidance"));
    }

    internal static void OnOpened(NCharacterSelectScreen screen)
    {
        Attach(screen);
        ScreenState state = States.GetOrCreateValue(screen);
        state.Panel?.GetNodeOrNull<NOnboardingDifficultyTickbox>("%DifficultyTickbox")?.RefreshFromSettings();
    }

    internal static void OnCharacterSelected(
        NCharacterSelectScreen screen,
        NCharacterSelectButton button,
        CharacterModel character)
    {
        Attach(screen);
        ScreenState state = States.GetOrCreateValue(screen);
        bool isNeuvillette = character is Characters.Neuvillette.Neuvillette
                             || character.Id.Entry == "NEUVILLETTE_CHARACTER_NEUVILLETTE";
        state.IsEligibleSelection = isNeuvillette && !button.IsLocked;

        if (state.Panel is not null)
            state.Panel.Visible = state.IsEligibleSelection;

        if (!state.IsEligibleSelection)
            return;

        state.Panel?.GetNodeOrNull<NOnboardingDifficultyTickbox>("%DifficultyTickbox")?.RefreshFromSettings();

        if (state.TutorialPending || SaveManager.Instance.SeenFtue(NOnboardingDifficultyFtue.Id))
            return;

        state.TutorialPending = true;
        _ = ShowTutorialWhenModalIsFree(screen, state);
    }

    private static async Task ShowTutorialWhenModalIsFree(NCharacterSelectScreen screen, ScreenState state)
    {
        try
        {
            await screen.AwaitProcessFrame();
            while (GodotObject.IsInstanceValid(screen)
                   && screen.IsInsideTree()
                   && screen.Visible
                   && NModalContainer.Instance?.OpenModal is not null)
            {
                await screen.AwaitProcessFrame();
            }

            if (!GodotObject.IsInstanceValid(screen)
                || !screen.IsInsideTree()
                || !screen.Visible
                || !state.IsEligibleSelection
                || SaveManager.Instance.SeenFtue(NOnboardingDifficultyFtue.Id))
            {
                return;
            }

            NOnboardingDifficultyFtue? ftue = NOnboardingDifficultyFtue.Create();
            if (ftue is not null)
                NModalContainer.Instance?.Add(ftue);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Could not show Judicial Guidance FTUE: {ex}");
        }
        finally
        {
            state.TutorialPending = false;
        }
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Ready))]
internal static class OnboardingCharacterSelectReadyPatch
{
    [HarmonyPostfix]
    private static void AttachDifficultyToggle(NCharacterSelectScreen __instance)
    {
        OnboardingCharacterSelectUi.Attach(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened))]
internal static class OnboardingCharacterSelectOpenedPatch
{
    [HarmonyPostfix]
    private static void RefreshDifficultyToggle(NCharacterSelectScreen __instance)
    {
        OnboardingCharacterSelectUi.OnOpened(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class OnboardingCharacterSelectedPatch
{
    [HarmonyPostfix]
    private static void UpdateDifficultyToggle(
        NCharacterSelectScreen __instance,
        NCharacterSelectButton charSelectButton,
        CharacterModel characterModel)
    {
        OnboardingCharacterSelectUi.OnCharacterSelected(__instance, charSelectButton, characterModel);
    }
}
