using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using Neuvillette.Characters.Base;

namespace Neuvillette.Characters.Neuvillette.Cards;

public abstract class NeuvilletteCard(
    int energyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : BaseCard(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected virtual IEnumerable<string> RegisteredKeywordIds => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
#pragma warning disable CS0618
            return RegisteredKeywordIds.Select(keywordId => keywordId.GetModCardKeyword());
#pragma warning restore CS0618
        }
    }
}
