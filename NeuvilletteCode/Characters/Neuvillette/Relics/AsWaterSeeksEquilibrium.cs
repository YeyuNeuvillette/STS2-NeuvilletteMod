using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Cards;
using Neuvillette.Characters.Neuvillette.Powers;
using Godot;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
[RegisterTouchOfOrobasRefinement(typeof(OratriceTime))]
[RegisterCharacterStarterRelic(typeof(Neuvillette))]
public sealed class AsWaterSeeksEquilibrium : BaseRelic
{
    private bool _isPlayerTurn;
    private decimal _previousHp;
    // HP-change hooks do not receive the choice context that caused them.  Keep the
    // owner's turn-start context so a droplet earned during setup is part of that
    // same synchronized action chain, rather than starting a competing local chain.
    private PlayerChoiceContext? _ownerTurnChoiceContext;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(HoverTipFactory.FromPowerWithPowerHoverTips<SourcewaterDroplet>());

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStartEarly(choiceContext, player);
        _isPlayerTurn = true;
        if (player == Owner)
            _ownerTurnChoiceContext = choiceContext;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side == CombatSide.Player)
        {
            _isPlayerTurn = false;
            _ownerTurnChoiceContext = null;
        }
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        await base.AfterCurrentHpChanged(creature, delta);

        if (!creature.IsPlayer || Owner == null || creature != Owner.Creature)
            return;

        if (!_isPlayerTurn)
        {
            _previousHp = creature.CurrentHp;
            return;
        }

        bool shouldSkip = delta > 0m && _previousHp >= creature.MaxHp;
        _previousHp = creature.CurrentHp;

        if (shouldSkip)
            return;

        // A new ThrowingPlayerChoiceContext here races the parallel multiplayer
        // turn-start setup tasks.  It can leave one peer with a droplet (and thus a
        // cheaper Equitable Judgment) before the checksum while another peer has not
        // applied it yet.  The owner's setup context is awaited by the turn setup.
        if (_ownerTurnChoiceContext != null)
            await PowerCmd.Apply<SourcewaterDroplet>(_ownerTurnChoiceContext, creature, 1, creature, null);
    }
}
