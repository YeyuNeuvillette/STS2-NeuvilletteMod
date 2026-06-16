using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class TidesIHaveReturned() : NeuvilletteCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<EquitableJudgment>(IsUpgraded)
    ];
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.Surge];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SfxCmd.Play("event:/Neuvillette/sfx/TidesIHaveReturned");

        for (var i = 0; i < 2; i++)
        {
            var card = CombatState!.CreateCard<EquitableJudgment>(Owner);
            if (IsUpgraded)
                CardCmd.Upgrade(card);

            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }

        var powerStacks = 2m;
        var powerTimes = IsUpgraded ? 2 : 1;
        for (var i = 0; i < powerTimes; i++)
        {
            await PowerCmd.Apply<TidesPower>(choiceContext, Owner.Creature, powerStacks, Owner.Creature, this);
        }
    }
}