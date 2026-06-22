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

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStartEarly(choiceContext, player);
        _isPlayerTurn = true;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side == CombatSide.Player)
            _isPlayerTurn = false;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        await base.AfterCurrentHpChanged(creature, delta);

        GD.Print($"AsWaterSeeksEquilibrium.AfterCurrentHpChanged: IsPlayer={creature.IsPlayer}, _isPlayerTurn={_isPlayerTurn}, delta={delta}, CurrentHp={creature.CurrentHp}, MaxHp={creature.MaxHp}, PreviousHp={_previousHp}");

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

        await PowerCmd.Apply<SourcewaterDroplet>(new ThrowingPlayerChoiceContext(), creature, 1, creature, null);
    }
}