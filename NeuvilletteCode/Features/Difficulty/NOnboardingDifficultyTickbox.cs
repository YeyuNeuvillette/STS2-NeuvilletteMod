using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Neuvillette.Features.Difficulty;

/// <summary>
/// Character-select mirror of the Judicial Guidance setting. Both this control
/// and RitsuLib's settings page write the same global settings cache.
/// </summary>
public partial class NOnboardingDifficultyTickbox : NTickbox
{
    public override void _Ready()
    {
        ConnectSignals();
        RefreshFromSettings();
    }

    public void RefreshFromSettings()
    {
        IsTicked = NeuvilletteSettingsStore.IsOnboardingDifficultyEnabled();
    }

    protected override void OnTick()
    {
        NeuvilletteSettingsStore.SetOnboardingDifficultyEnabled(true);
    }

    protected override void OnUntick()
    {
        NeuvilletteSettingsStore.SetOnboardingDifficultyEnabled(false);
    }
}
