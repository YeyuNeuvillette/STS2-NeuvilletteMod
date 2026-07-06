using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class CeremonialSilverBell : BaseRelic
{
    private const int ReplayAmount = 4;
    private const int RingingInterval = 3;

    private int _turnsUntilRinging;
    private readonly Dictionary<CardModel, Action> _afflictionHandlers = [];
    private readonly HashSet<CardModel> _cardsWithReplay = [];

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
        _turnsUntilRinging = RingingInterval;
        _afflictionHandlers.Clear();
        _cardsWithReplay.Clear();
        InvokeDisplayAmountChanged();

        var combatState = Owner?.PlayerCombatState;
        if (combatState != null)
        {
            foreach (var card in combatState.AllCards)
                SubscribeCard(card);
        }

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
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner != Owner)
            return Task.CompletedTask;

        SubscribeCard(card);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        UnsubscribeAllCards();
        return Task.CompletedTask;
    }

    private void SubscribeCard(CardModel card)
    {
        if (_afflictionHandlers.ContainsKey(card))
            return;

        Action handler = () => OnCardAfflictionChanged(card);
        _afflictionHandlers[card] = handler;
        card.AfflictionChanged += handler;

        if (card.Affliction is Ringing)
        {
            card.BaseReplayCount += ReplayAmount;
            _cardsWithReplay.Add(card);
        }
    }

    private void UnsubscribeAllCards()
    {
        foreach (var (card, handler) in _afflictionHandlers)
        {
            card.AfflictionChanged -= handler;
            if (_cardsWithReplay.Contains(card))
                card.BaseReplayCount -= ReplayAmount;
        }
        _afflictionHandlers.Clear();
        _cardsWithReplay.Clear();
    }

    private void OnCardAfflictionChanged(CardModel card)
    {
        if (card.Affliction is Ringing && _cardsWithReplay.Add(card))
            card.BaseReplayCount += ReplayAmount;
        else if (card.Affliction is not Ringing && _cardsWithReplay.Remove(card))
            card.BaseReplayCount -= ReplayAmount;
    }
}