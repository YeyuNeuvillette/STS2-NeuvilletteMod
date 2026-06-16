using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Touching() : NeuvilletteCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Surge", 3m)
    ];

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
        IsUpgraded
            ? [NeuvilletteKeywords.SourcewaterDroplet, NeuvilletteKeywords.Surge]
            : [NeuvilletteKeywords.SourcewaterDroplet];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            var surgeValue = DynamicVars["Surge"].BaseValue;
            await CreatureCmd.Heal(Owner.Creature, surgeValue);
            await PowerCmd.Apply<SurgePower>(choiceContext, Owner.Creature, surgeValue, Owner.Creature, this);
        }

        var droplets = Owner.Creature.GetPower<SourcewaterDroplet>();
        if (droplets == null || droplets.Amount <= 0)
            return;

        var drawCount = droplets.Amount * 1m;
        await PowerCmd.Remove(droplets);
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }
}