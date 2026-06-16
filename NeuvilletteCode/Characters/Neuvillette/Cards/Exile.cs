using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Exile() : NeuvilletteCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = Owner.PlayerCombatState?.Hand;
        if (hand == null || hand.Cards.Count == 0)
            return;

        CardModel? cardToMove;

        if (IsUpgraded)
        {
            cardToMove = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "NEUVILLETTE-EXILE.selectionScreenPrompt"), 1),
                null,
                this)).FirstOrDefault();
        }
        else
        {
            cardToMove = Owner.RunState.Rng.CombatCardSelection.NextItem(hand.Cards);
        }

        if (cardToMove == null)
            return;

        await CardPileCmd.Add(cardToMove, PileType.Draw, CardPilePosition.Bottom, this);

        var energyGain = cardToMove.EnergyCost.GetAmountToSpend();
        if (energyGain > 0)
            await PlayerCmd.GainEnergy(energyGain, Owner);
    }
}