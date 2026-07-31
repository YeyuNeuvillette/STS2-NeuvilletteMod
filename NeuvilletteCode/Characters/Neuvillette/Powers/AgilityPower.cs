using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class AgilityPower : NeuvillettePower
{
    private sealed class Data
    {
        public bool HasAppliedThisTurn;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner)
            return 0m;

        if (cardSource == null)
            return 0m;

        if (cardSource.Owner.Creature != Owner)
            return 0m;

        if (cardSource.Type != CardType.Skill)
            return 0m;

        if (!props.IsPoweredCardOrMonsterMoveBlock())
            return 0m;

        if (cardPlay != null && cardPlay.Card == cardSource)
        {
            var data = GetInternalData<Data>();
            data.HasAppliedThisTurn = true;
        }

        return Amount;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var data = GetInternalData<Data>();
        if (!data.HasAppliedThisTurn)
            return;

        await PowerCmd.ModifyAmount(context, this, -Amount, null, null);
        data.HasAppliedThisTurn = false;
    }
}
