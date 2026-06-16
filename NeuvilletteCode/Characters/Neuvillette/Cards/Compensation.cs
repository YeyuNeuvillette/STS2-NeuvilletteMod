using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Compensation() : NeuvilletteCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([
            HoverTipFactory.FromPower<StrengthPower>()
        ]);

    private int HpThreshold => IsUpgraded ? 4 : 6;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            ModCardVars.Computed("StrengthAmount", 0m, card => 
                card?.Owner?.Creature != null 
                    ? Math.Floor((card.Owner.Creature.MaxHp - card.Owner.Creature.CurrentHp) / (card.IsUpgraded ? 4m : 6m)) 
                    : 0m)
        ];

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hpDifference = Owner.Creature.MaxHp - Owner.Creature.CurrentHp;
        var strengthAmount = hpDifference / HpThreshold;

        if (strengthAmount > 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, strengthAmount, Owner.Creature, this);
        }
    }
}