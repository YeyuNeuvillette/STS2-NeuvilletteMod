using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class CourtSessionPower : NeuvillettePower
{
    private sealed class Data
    {
        public CardModel? SourceCard;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData() => new Data();

    public override async Task AfterApplied(Creature? applier, CardModel? sourceCard)
    {
        await base.AfterApplied(applier, sourceCard);
        var data = GetInternalData<Data>();
        data.SourceCard = sourceCard;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        if (cardPlay.Card.Owner != Owner.Player)
            return;

        var data = GetInternalData<Data>();
        if (data.SourceCard != null && cardPlay.Card == data.SourceCard)
            return;

        var cost = cardPlay.Card.EnergyCost == null ? 0 : Math.Max(0, (int)cardPlay.Card.EnergyCost.GetResolved());
        var points = 10 + cost * 10;

        await CardCmd.Exhaust(context, cardPlay.Card);
        await PowerCmd.Apply<OratricePower>(context, Owner, points, Owner, null);

        var proceduralJustice = Owner.GetPower<ProceduralJusticePower>();
        if (proceduralJustice != null)
            await PowerCmd.Apply<OratricePower>(context, Owner, proceduralJustice.Amount, Owner, null);

        await PowerCmd.Decrement(this);
    }
}