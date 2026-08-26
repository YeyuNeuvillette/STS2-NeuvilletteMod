using MegaCrit.Sts2.Core.Timeline;
using Neuvillette.Characters.Neuvillette.Cards;
using Neuvillette.Characters.Neuvillette.Relics;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Timeline.Scaffolding;

namespace Neuvillette.Characters.Neuvillette.Timeline;

/// <summary>
/// The seven chapters intentionally occupy a single story column and each
/// chapter uses its own supplied timeline illustration.
/// </summary>
[RegisterStory]
public sealed class NeuvilletteStory : ModStoryTemplate
{
    protected override string StoryKey => "Neuvillette";

    // The base game discovers Act 1-3 epochs by appending 2/3/4_EPOCH to
    // CharacterModel.Id.Entry. Keep this derived from the registered character
    // entry so changes to the mod/type name cannot silently break progression.
    internal static string ActEpochKey(int actNumber) =>
        ModContentRegistry.GetFixedPublicEntry(MainFile.ModId, typeof(Neuvillette))
        + $"{actNumber + 1}_EPOCH";
}

public abstract class NeuvilletteEpoch : ModEpochTemplate
{
    public override string StoryId => "Neuvillette";
    public override string? CustomBigPortraitPath =>
        "res://Neuvillette/images/characters/Neuvillette/neuvillette_char_select.png";
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(NeuvilletteStory))]
[AutoTimelineSlot(EpochEra.Invitation0)]
public sealed class Neuvillette1Epoch : CharacterUnlockEpochTemplate<Neuvillette>
{
    public override string Id => "NEUVILLETTE1_EPOCH";
    public override string StoryId => "Neuvillette";
    public override string? CustomPackedPortraitPath => NeuvilletteTimelineArt.Chapter1;
    public override string? CustomBigPortraitPath => NeuvilletteTimelineArt.Chapter1;
    protected override IReadOnlyList<Type> ExpansionEpochTypes =>
    [
        typeof(Neuvillette2Epoch), typeof(Neuvillette3Epoch), typeof(Neuvillette4Epoch),
        typeof(Neuvillette5Epoch), typeof(Neuvillette6Epoch), typeof(Neuvillette7Epoch)
    ];
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(NeuvilletteStory))]
[AutoTimelineSlot(EpochEra.Invitation2)]
[RegisterEpochCards(typeof(AweInspiring), typeof(LaminarFlow), typeof(ObjectionOverruled))]
public sealed class Neuvillette2Epoch : CardUnlockEpochTemplate
{
    public override string Id => NeuvilletteStory.ActEpochKey(1);
    public override string StoryId => "Neuvillette";
    public override string? CustomPackedPortraitPath => NeuvilletteTimelineArt.Chapter2;
    public override string? CustomBigPortraitPath => NeuvilletteTimelineArt.Chapter2;
    protected override IReadOnlyList<Type> CardTypes => [typeof(AweInspiring), typeof(LaminarFlow), typeof(ObjectionOverruled)];
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(NeuvilletteStory))]
[AutoTimelineSlot(EpochEra.Invitation3)]
public sealed class Neuvillette3Epoch : RelicUnlockEpochTemplate
{
    public override string Id => NeuvilletteStory.ActEpochKey(2);
    public override string StoryId => "Neuvillette";
    public override string? CustomPackedPortraitPath => NeuvilletteTimelineArt.Chapter3;
    public override string? CustomBigPortraitPath => NeuvilletteTimelineArt.Chapter3;
    protected override IReadOnlyList<Type> RelicTypes => [typeof(Gavel), typeof(ExcuseNote), typeof(SeaFoamMailbox)];
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(NeuvilletteStory))]
[AutoTimelineSlot(EpochEra.Invitation4)]
[RegisterEpochCards(typeof(ProceduralJustice), typeof(ThousandFingersPointing), typeof(HydroDragon))]
public sealed class Neuvillette4Epoch : CardUnlockEpochTemplate
{
    public override string Id => NeuvilletteStory.ActEpochKey(3);
    public override string StoryId => "Neuvillette";
    public override string? CustomPackedPortraitPath => NeuvilletteTimelineArt.Chapter4;
    public override string? CustomBigPortraitPath => NeuvilletteTimelineArt.Chapter4;
    protected override IReadOnlyList<Type> CardTypes => [typeof(ProceduralJustice), typeof(ThousandFingersPointing), typeof(HydroDragon)];
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(NeuvilletteStory))]
[AutoTimelineSlot(EpochEra.Invitation5)]
public sealed class Neuvillette5Epoch : RelicUnlockEpochTemplate
{
    public override string Id => "NEUVILLETTE5_EPOCH";
    public override string StoryId => "Neuvillette";
    public override string? CustomPackedPortraitPath => NeuvilletteTimelineArt.Chapter5;
    public override string? CustomBigPortraitPath => NeuvilletteTimelineArt.Chapter5;
    protected override IReadOnlyList<Type> RelicTypes => [typeof(StoppedPocketWatch), typeof(Monocle), typeof(Plumule)];
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(NeuvilletteStory))]
[AutoTimelineSlot(EpochEra.Invitation6)]
[RegisterEpochCards(typeof(Retrial), typeof(LegalAid), typeof(Rebirth), typeof(TrialGroup), typeof(AssistArrest))]
public sealed class Neuvillette6Epoch : CardUnlockEpochTemplate
{
    public override string Id => "NEUVILLETTE6_EPOCH";
    public override string StoryId => "Neuvillette";
    public override string? CustomPackedPortraitPath => NeuvilletteTimelineArt.Chapter6;
    public override string? CustomBigPortraitPath => NeuvilletteTimelineArt.Chapter6;
    protected override IReadOnlyList<Type> CardTypes =>
        [typeof(Retrial), typeof(LegalAid), typeof(Rebirth), typeof(TrialGroup), typeof(AssistArrest)];
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(NeuvilletteStory))]
[AutoTimelineSlot(EpochEra.Invitation7)]
public sealed class Neuvillette7Epoch : NeuvilletteEpoch
{
    public override string Id => "NEUVILLETTE7_EPOCH";
    public override string? CustomPackedPortraitPath => NeuvilletteTimelineArt.Chapter7;
    public override string? CustomBigPortraitPath => NeuvilletteTimelineArt.Chapter7;
    public override string UnlockText => "解锁第四幕。";

    public override void QueueUnlocks()
    {
        base.QueueUnlocks();
        NeuvilletteSettingsStore.UnlockAct4();
    }
}

internal static class NeuvilletteTimelineArt
{
    private const string TimelineRoot = "res://Neuvillette/images/timeline/epoch_portraits/";

    public const string Chapter1 = TimelineRoot + "neuvillette1_epoch.png";
    public const string Chapter2 = TimelineRoot + "neuvillette2_epoch.png";
    public const string Chapter3 = TimelineRoot + "neuvillette3_epoch.png";
    public const string Chapter4 = TimelineRoot + "neuvillette4_epoch.png";
    public const string Chapter5 = TimelineRoot + "neuvillette5_epoch.png";
    public const string Chapter6 = TimelineRoot + "neuvillette6_epoch.png";
    public const string Chapter7 = TimelineRoot + "neuvillette7_epoch.png";
    public const string Placeholder = "res://Neuvillette/images/characters/Neuvillette/neuvillette_char_select.png";
}
