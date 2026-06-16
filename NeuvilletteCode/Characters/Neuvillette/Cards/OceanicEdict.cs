using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class OceanicEdict() : NeuvilletteCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.SourcewaterDroplet
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Cards", 2m),
        new PowerVar<SourcewaterDroplet>(6m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Count == 0)
            return;

        var cardCount = Math.Min(DynamicVars["Cards"].IntValue, drawPile.Cards.Count);
        var selectedCards = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            drawPile.Cards,
            Owner,
            new CardSelectorPrefs(new LocString("card_selection", "TO_ADD_TO_HAND"), cardCount))).ToList();

        if (selectedCards.Count == 0)
            return;

        var dropletPower = Owner.Creature.GetPower<SourcewaterDroplet>();
        var dropletCost = DynamicVars["SourcewaterDroplet"].BaseValue;

        if (dropletPower != null && dropletPower.Amount >= dropletCost)
        {
            await PowerCmd.ModifyAmount(choiceContext, dropletPower, -dropletCost, null, null);
            
            foreach (var card in selectedCards)
            {
                await CardCmd.AutoPlay(choiceContext, card, null);
            }
        }
        else
        {
            foreach (var card in selectedCards)
            {
                await CardPileCmd.Add(card, PileType.Hand);
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}