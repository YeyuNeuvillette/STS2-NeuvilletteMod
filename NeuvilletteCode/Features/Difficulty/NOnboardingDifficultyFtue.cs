using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace Neuvillette.Features.Difficulty;

/// <summary>
/// One-time, profile-scoped introduction rendered with the base game's FTUE
/// popup, pointer and confirmation button.
/// </summary>
public partial class NOnboardingDifficultyFtue : NFtue
{
    public const string Id = "neuvillette_judicial_guidance_ftue";
    private const string ScenePath = MainFile.ResPath + "/scenes/ui/onboarding_difficulty_ftue.tscn";

    public override void _Ready()
    {
        var i18n = NeuvilletteSettingsStore.Localization;
        GetNode<MegaLabel>("%Header").SetTextAutoSize(i18n.Get(
            "neuvillette.character_select.difficulty.ftue.title",
            "Judicial Guidance"));
        GetNode<MegaRichTextLabel>("%Description").SetTextAutoSize(i18n.Get(
            "neuvillette.character_select.difficulty.ftue.description",
            "Judicial Guidance eases your first ten Neuvillette runs without changing Ascension. Its limited revives are shared across the entire run. You can toggle it here or in Mod Settings."));
        GetNode<NButton>("%FtueConfirmButton").Connect(
            NClickableControl.SignalName.Released,
            Callable.From((Action<NButton>)Close));

        Color modulate = Modulate;
        modulate.A = 0f;
        Modulate = modulate;
        Control popup = GetNode<Control>("%FtuePopup");
        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "modulate:a", 1f, 0.25f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(popup, "position:y", popup.Position.Y, 0.3f)
            .From(popup.Position.Y + 60f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    public static NOnboardingDifficultyFtue? Create()
    {
        if (TestMode.IsOn)
            return null;

        PackedScene? scene = ResourceLoader.Load<PackedScene>(ScenePath, null, ResourceLoader.CacheMode.Reuse);
        return scene?.Instantiate<NOnboardingDifficultyFtue>(PackedScene.GenEditState.Disabled);
    }

    private void Close(NButton _)
    {
        SaveManager.Instance.MarkFtueAsComplete(Id);
        CloseFtue();
    }
}
