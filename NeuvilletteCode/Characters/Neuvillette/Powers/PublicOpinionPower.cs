using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class PublicOpinionPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterApplied(Creature? applier, CardModel? sourceCard)
    {
        await base.AfterApplied(applier, sourceCard);
        GD.Print($"PublicOpinionPower applied to {Owner.Player?.NetId}");
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        GD.Print($"PublicOpinionPower AfterCardPlayed: card owner = {cardPlay.Card.Owner.NetId}, power owner = {Owner.Player?.NetId}, card type = {cardPlay.Card.Type}");

        if (Owner.Player == null || cardPlay.Card.Owner.NetId == Owner.Player.NetId)
            return;

        if (cardPlay.Card.Type != CardType.Attack && cardPlay.Card.Type != CardType.Skill)
            return;

        var amount = 5m;
        var oratricePower = Owner.GetPower<OratricePower>();
        GD.Print($"PublicOpinionPower: OratricePower exists = {oratricePower != null}");
        
        if (cardPlay.Card.Type == CardType.Attack)
        {
            if (oratricePower != null)
            {
                await PowerCmd.ModifyAmount(context, oratricePower, amount, Owner, null);
            }
            else
            {
                await PowerCmd.Apply<OratricePower>(context, Owner, amount, Owner, null);
            }
        }
        else if (cardPlay.Card.Type == CardType.Skill)
        {
            if (oratricePower != null)
            {
                await PowerCmd.ModifyAmount(context, oratricePower, -amount, Owner, null);
            }
            else
            {
                await PowerCmd.Apply<OratricePower>(context, Owner, -amount, Owner, null);
            }
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }
}