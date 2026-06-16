using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class SourceOfLife() : SurgeCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override int BaseSurgeValue => 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
        IsUpgraded
            ? [NeuvilletteKeywords.SourcewaterDroplet, NeuvilletteKeywords.Surge]
            : [NeuvilletteKeywords.SourcewaterDroplet];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
            await ApplySurgeLogic(choiceContext);

        var droplets = Owner.Creature.GetPower<SourcewaterDroplet>();
        if (droplets == null || droplets.Amount <= 0)
            return;

        var energy = (int)droplets.Amount;
        await PowerCmd.Remove(droplets);
        await PlayerCmd.GainEnergy(energy, Owner);
    }
}