using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class CeremonialSilverBell : BaseRelic
{
    private const int ReplayAmount = 4;
    private const int RingingInterval = 3;

    private bool _hasReplay;
    private int _turnsUntilRinging;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => _turnsUntilRinging;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar>
        {
            new DynamicVar("Replay", ReplayAmount),
            new DynamicVar("Interval", RingingInterval)
        };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips
            .Concat(HoverTipFactory.FromPowerWithPowerHoverTips<RingingPower>())
            .Append(HoverTipFactory.Static(StaticHoverTip.ReplayStatic));

    public override Task BeforeCombatStart()
    {
        _hasReplay = false;
        _turnsUntilRinging = RingingInterval;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Creature.Side)
            return;

        var creature = Owner.Creature;
        var turnNumber = combatState.RoundNumber;

        _turnsUntilRinging = RingingInterval - (turnNumber % RingingInterval);
        if (_turnsUntilRinging == 0)
            _turnsUntilRinging = RingingInterval;
        InvokeDisplayAmountChanged();

        if (turnNumber > 0 && turnNumber % RingingInterval == 0)
        {
            Flash();
            await PowerCmd.Apply<RingingPower>(new ThrowingPlayerChoiceContext(), creature, 1m, creature, null);
        }

        if (_turnsUntilRinging == 1)
            Flash();

        UpdateReplay(creature);
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner != Owner)
            return Task.CompletedTask;

        if (!_hasReplay)
            return Task.CompletedTask;

        card.BaseReplayCount += ReplayAmount;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side)
            return Task.CompletedTask;

        if (_hasReplay)
        {
            RemoveReplayFromAllCards();
            _hasReplay = false;
        }

        return Task.CompletedTask;
    }

    private void UpdateReplay(Creature creature)
    {
        bool hasRinging = creature.HasPower<RingingPower>();

        if (hasRinging && !_hasReplay)
        {
            AddReplayToAllCards();
            _hasReplay = true;
        }
        else if (!hasRinging && _hasReplay)
        {
            RemoveReplayFromAllCards();
            _hasReplay = false;
        }
    }

    private void AddReplayToAllCards()
    {
        var combatState = Owner?.PlayerCombatState;
        if (combatState == null) return;
        foreach (var card in combatState.AllCards)
        {
            card.BaseReplayCount += ReplayAmount;
        }
    }

    private void RemoveReplayFromAllCards()
    {
        var combatState = Owner?.PlayerCombatState;
        if (combatState == null) return;
        foreach (var card in combatState.AllCards)
        {
            card.BaseReplayCount -= ReplayAmount;
        }
    }
}