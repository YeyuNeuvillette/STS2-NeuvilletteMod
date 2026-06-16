using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class CourtArgument() : NeuvilletteCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([
            HoverTipFactory.FromPower<VigorPower>(),
            HoverTipFactory.FromPower<AgilityPower>(),
        ]);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<DefensePower>(1m),
        new PowerVar<ProsecutionPower>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DefensePower>(choiceContext, Owner.Creature, DynamicVars["DefensePower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<ProsecutionPower>(choiceContext, Owner.Creature, DynamicVars["ProsecutionPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DefensePower"].UpgradeValueBy(1m);
        DynamicVars["ProsecutionPower"].UpgradeValueBy(1m);
    }
}