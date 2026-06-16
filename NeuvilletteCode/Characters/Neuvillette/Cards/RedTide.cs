using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class RedTide() : SurgeCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.Surge];
    protected override int BaseSurgeValue => 9;
    protected override int UpgradeSurgeValue => 3;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 2m,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        await ApplySurgeLogic(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}