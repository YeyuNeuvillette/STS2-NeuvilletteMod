using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Admonition() : SurgeCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override int BaseSurgeValue => 3;
    protected override int UpgradeSurgeValue => 2;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromPower<ContemptOfCourtPower>()]);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat([new PowerVar<ContemptOfCourtPower>(3m)]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        var contemptAmount = DynamicVars["ContemptOfCourtPower"].BaseValue;
        await PowerCmd.Apply<ContemptOfCourtPower>(choiceContext, cardPlay.Target, contemptAmount, Owner.Creature, this);

        await ApplySurgeLogic(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ContemptOfCourtPower"].UpgradeValueBy(2m);
        base.OnUpgrade();
    }
}