using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class PastDraconicGlories() : NeuvilletteCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<EquitableJudgment>(IsUpgraded)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PastDraconicGloriesPower>(1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        var cardModel = CombatState!.CreateCard<EquitableJudgment>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(cardModel);
        }
        
        await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, Owner);
        
        await PowerCmd.Apply<PastDraconicGloriesPower>(choiceContext, Owner.Creature, DynamicVars["PastDraconicGloriesPower"].BaseValue, Owner.Creature, this);
    }
}