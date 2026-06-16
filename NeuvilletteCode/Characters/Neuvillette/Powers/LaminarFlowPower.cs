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
public sealed class LaminarFlowPower : NeuvillettePower
{
    private sealed class Data
    {
        public bool HasPlayedAttackThisTurn;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData() => new Data();

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        var data = GetInternalData<Data>();
        data.HasPlayedAttackThisTurn = false;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != Owner)
            return playCount;

        if (card.Type != CardType.Attack)
            return playCount;

        var data = GetInternalData<Data>();
        if (data.HasPlayedAttackThisTurn)
            return playCount;

        var droplets = Owner.GetPower<SourcewaterDroplet>();
        if (droplets == null || droplets.Amount < Amount)
            return playCount;

        return playCount + 1;
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card.Owner.Creature != Owner)
            return;

        if (card.Type != CardType.Attack)
            return;

        var data = GetInternalData<Data>();
        if (data.HasPlayedAttackThisTurn)
            return;

        var droplets = Owner.GetPower<SourcewaterDroplet>();
        if (droplets == null || droplets.Amount < Amount)
            return;

        data.HasPlayedAttackThisTurn = true;

        Flash();
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), droplets, -Amount, null, null);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);
    }
}