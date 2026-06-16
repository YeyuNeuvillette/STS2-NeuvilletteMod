using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class SigewinnesWillPower : NeuvillettePower
{
    private const int TriggerCount = 9;
    private int cardsPlayed;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => TriggerCount - cardsPlayed;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterApplied(Creature? applier, CardModel? sourceCard)
    {
        await base.AfterApplied(applier, sourceCard);
        cardsPlayed = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card is not MelusineStickerCard)
            return;

        if (cardPlay.IsAutoPlay)
            return;

        cardsPlayed++;
        if (cardsPlayed < TriggerCount)
        {
            InvokeDisplayAmountChanged();
            return;
        }

        Flash();
        var generated = CardFactory.GetForCombat(Owner.Player, [ModelDb.Card<SigewinneSticker>()], 1, Owner.Player.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (generated != null)
            await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, Owner.Player);

        cardsPlayed = 0;
        InvokeDisplayAmountChanged();
    }
}