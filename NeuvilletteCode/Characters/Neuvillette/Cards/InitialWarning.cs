using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class InitialWarning() : NeuvilletteCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new PowerVar<ContemptOfCourtPower>(4m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromPower<ContemptOfCourtPower>()]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        await PowerCmd.Apply<ContemptOfCourtPower>(choiceContext, cardPlay.Target, DynamicVars["ContemptOfCourtPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["ContemptOfCourtPower"].UpgradeValueBy(2m);
    }
}