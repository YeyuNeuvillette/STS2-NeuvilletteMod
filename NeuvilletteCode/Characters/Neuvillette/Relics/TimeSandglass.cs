using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class TimeSandglass : BaseRelic
{
    private bool _tookExtraTurnThisCycle;
    private int _cardsPlayed;
    private const int CardThreshold = 6;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => _cardsPlayed;

    public override Task BeforeCombatStart()
    {
        _tookExtraTurnThisCycle = false;
        _cardsPlayed = 0;
        Log.Info($"[TimeSandglass] BeforeCombatStart: reset all state");
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);
        if (side != CombatSide.Player || !participants.Any(c => c.Player == base.Owner))
            return;

        if (_tookExtraTurnThisCycle && !CombatManager.Instance.PlayersTakingExtraTurn.Contains(base.Owner))
        {
            _tookExtraTurnThisCycle = false;
            Log.Info($"[TimeSandglass] AfterSideTurnStart: reset tookExtraTurnThisCycle (new normal turn)");
        }

        Log.Info($"[TimeSandglass] AfterSideTurnStart: cardsPlayed={_cardsPlayed}, tookExtraTurnThisCycle={_tookExtraTurnThisCycle}");
        RefreshStatus();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        if (cardPlay.Card.Owner != base.Owner)
        {
            Log.Info($"[TimeSandglass] AfterCardPlayed: skipped, card owner != relic owner");
            return;
        }

        if (!CombatManager.Instance.IsInProgress)
        {
            Log.Info($"[TimeSandglass] AfterCardPlayed: skipped, combat not in progress");
            return;
        }

        _cardsPlayed++;
        Log.Info($"[TimeSandglass] AfterCardPlayed: card={cardPlay.Card.Id.Entry}, cardsPlayed={_cardsPlayed}, threshold={CardThreshold}");
        RefreshStatus();

        if (_cardsPlayed >= CardThreshold)
        {
            _cardsPlayed = 0;
            Log.Info($"[TimeSandglass] AfterCardPlayed: threshold reached! Attempting PlayerCmd.EndTurn. Owner={base.Owner?.NetId}");
            Flash();
            var p = base.Owner;
            if (p != null)
            {
                PlayerCmd.EndTurn(p, canBackOut: false);
                Log.Info($"[TimeSandglass] AfterCardPlayed: PlayerCmd.EndTurn called. IsPlayerReadyToEndTurn={CombatManager.Instance.IsPlayerReadyToEndTurn(p)}");
            }
            else
            {
                Log.Warn($"[TimeSandglass] AfterCardPlayed: Owner is null, cannot end turn!");
            }
            RefreshStatus();
        }
    }

    public override bool ShouldTakeExtraTurn(Player player)
    {
        if (player != base.Owner)
        {
            Log.Info($"[TimeSandglass] ShouldTakeExtraTurn: false (player != owner)");
            return false;
        }

        if (_tookExtraTurnThisCycle)
        {
            Log.Info($"[TimeSandglass] ShouldTakeExtraTurn: false (tookExtraTurnThisCycle=true)");
            return false;
        }

        Log.Info($"[TimeSandglass] ShouldTakeExtraTurn: TRUE! cardsPlayed={_cardsPlayed}, tookExtraTurnThisCycle={_tookExtraTurnThisCycle}");
        return true;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player != base.Owner)
            return Task.CompletedTask;

        Flash();
        _tookExtraTurnThisCycle = true;
        Log.Info($"[TimeSandglass] AfterTakingExtraTurn: set tookExtraTurnThisCycle=true");
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(choiceContext, side, participants);
        if (!participants.Contains(base.Owner.Creature))
            return;

        Log.Info($"[TimeSandglass] BeforeSideTurnEnd: side={side}, cardsPlayed={_cardsPlayed}");
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _tookExtraTurnThisCycle = false;
        _cardsPlayed = 0;
        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        Log.Info($"[TimeSandglass] AfterCombatEnd: reset all state");
        return Task.CompletedTask;
    }

    private void RefreshStatus()
    {
        base.Status = (_cardsPlayed >= CardThreshold) ? RelicStatus.Active : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }
}