using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class Plumule : BaseRelic
{
    private bool _triggeredThisTurn;

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

        if (_triggeredThisTurn)
            return;

        var hpLost = -delta;
        _triggeredThisTurn = true;
        Flash();
        await PowerCmd.Apply<PlumulePower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            hpLost,
            Owner.Creature,
            null);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            _triggeredThisTurn = false;

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _triggeredThisTurn = false;
        return Task.CompletedTask;
    }
}
