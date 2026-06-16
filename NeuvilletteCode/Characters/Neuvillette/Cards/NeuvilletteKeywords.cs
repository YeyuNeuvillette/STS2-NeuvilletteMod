using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace Neuvillette.Characters.Neuvillette.Cards;

internal static class NeuvilletteKeywordRegistration
{
    [RegisterOwnedCardKeyword(NeuvilletteKeywords.SourcewaterDropletKey,
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
    private sealed class SourcewaterDroplet;

    [RegisterOwnedCardKeyword(NeuvilletteKeywords.SurgeKey,
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
    private sealed class Surge;

    [RegisterOwnedCardKeyword(NeuvilletteKeywords.SubmitKey,
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
    private sealed class Submit;

    [RegisterOwnedCardKeyword(NeuvilletteKeywords.AgilityKey,
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
    private sealed class Agility;

    [RegisterOwnedCardKeyword(NeuvilletteKeywords.MelusineStickerKey,
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
    private sealed class MelusineSticker;
}

public static class NeuvilletteKeywords
{
    public const string SourcewaterDropletKey = "sourcewaterdroplet";
    public const string SurgeKey = "surge";
    public const string SubmitKey = "submit";
    public const string AgilityKey = "agility";
    public const string MelusineStickerKey = "melusinesticker";

    public static readonly string SourcewaterDroplet = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, SourcewaterDropletKey);
    public static readonly string Surge = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, SurgeKey);
    public static readonly string Submit = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, SubmitKey);
    public static readonly string Agility = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, AgilityKey);
    public static readonly string MelusineSticker = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, MelusineStickerKey);
}