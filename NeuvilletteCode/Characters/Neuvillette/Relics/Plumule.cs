using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class Plumule : BaseRelic
{
    private decimal _strengthGainedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromPower<StrengthPower>()]);

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner?.Creature)
            return;

        if (!CombatManager.Instance.IsInProgress)
            return;

        if (delta >= 0)
            return;

        if (Owner?.Creature?.CombatState?.CurrentSide != CombatSide.Player)
            return;

        if (_strengthGainedThisCombat > 0)
            return;

        var hpLost = -delta;
        _strengthGainedThisCombat = hpLost;
        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, hpLost, Owner.Creature, null);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        _strengthGainedThisCombat = 0m;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner?.Creature?.Side)
            return;

        if (_strengthGainedThisCombat > 0)
        {
            var strengthPower = Owner.Creature.GetPower<StrengthPower>();
            if (strengthPower != null)
            {
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), strengthPower, -_strengthGainedThisCombat, null, null);
            }
            _strengthGainedThisCombat = 0m;
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _strengthGainedThisCombat = 0m;
        return Task.CompletedTask;
    }
}