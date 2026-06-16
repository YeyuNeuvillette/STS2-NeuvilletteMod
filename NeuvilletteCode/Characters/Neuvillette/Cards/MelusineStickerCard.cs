using MegaCrit.Sts2.Core.Entities.Cards;

namespace Neuvillette.Characters.Neuvillette.Cards;

public abstract class MelusineStickerCard(TargetType targetType)
    : NeuvilletteCard(0, CardType.Skill, CardRarity.Token, targetType, shouldShowInCardLibrary: false)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        CardKeyword.Retain
    ];

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.MelusineSticker
    ];
}