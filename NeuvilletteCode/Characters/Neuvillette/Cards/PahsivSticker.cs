using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(MelusineCardPool))]
public sealed class PahsivSticker() : MelusineStickerCard(TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
            return;

        var availableStickerCards = ModelDb.CardPool<MelusineCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => MelusineCardPool.GetAvailableCardsForCombat((CombatState)combatState).Any(available => available.GetType() == card.GetType()) && card.GetType() != typeof(PahsivSticker))
            .ToList();

        if (availableStickerCards.Count == 0)
            return;

        var choices = CardFactory.GetDistinctForCombat(Owner, availableStickerCards, Math.Min(3, availableStickerCards.Count), Owner.RunState.Rng.CombatCardGeneration).ToList();
        var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, Owner, canSkip: false);
        if (selected != null)
            await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }
}