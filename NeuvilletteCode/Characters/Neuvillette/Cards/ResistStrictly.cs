using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class ResistStrictly() : NeuvilletteCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<ContemptOfCourtPower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromPower<ContemptOfCourtPower>()]);

    protected override bool ShouldGlowGoldInternal =>
        CombatState != null && CombatState.HittableEnemies.Any(static e => e.Monster?.IntendsToAttack == true);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        var powerAmount = DynamicVars["ContemptOfCourtPower"].BaseValue;
        if (cardPlay.Target.Monster?.IntendsToAttack == true)
            powerAmount *= 2m;

        await PowerCmd.Apply<ContemptOfCourtPower>(choiceContext, cardPlay.Target, powerAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars["ContemptOfCourtPower"].UpgradeValueBy(1m);
    }
}