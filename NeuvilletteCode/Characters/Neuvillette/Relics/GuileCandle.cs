using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class GuileCandle : BaseRelic
{
    private const int DrawThreshold = 3;

    private List<CardModel> _drawnCards = [];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => _drawnCards.Count;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DynamicVar("Cards", DrawThreshold) };

    public override Task BeforeCombatStart()
    {
        _drawnCards = [];
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        Log.Info($"[GuileCandle] AfterCardDrawn called: card={card?.Id.ToString() ?? "null"}, owner={card?.Owner?.NetId.ToString() ?? "null"}, relicOwner={Owner?.NetId.ToString() ?? "null"}, fromHandDraw={fromHandDraw}, currentCount={_drawnCards.Count}");

        if (card.Owner != Owner)
        {
            Log.Info($"[GuileCandle] Skipping: card owner != relic owner");
            return;
        }

        _drawnCards.Add(card);
        InvokeDisplayAmountChanged();
        Log.Info($"[GuileCandle] Card added. _drawnCards count={_drawnCards.Count}, cards=[{string.Join(", ", _drawnCards.Select(c => c.Id))}]");

        if (_drawnCards.Count >= DrawThreshold)
        {
            Log.Info($"[GuileCandle] Threshold reached ({_drawnCards.Count} >= {DrawThreshold}), calling TryAutoPlayFromDrawn");
            await TryAutoPlayFromDrawn(choiceContext);
            Log.Info($"[GuileCandle] TryAutoPlayFromDrawn completed. _drawnCards count after={_drawnCards.Count}");
        }
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side)
            return Task.CompletedTask;

        Log.Info($"[GuileCandle] AfterSideTurnEnd: resetting _drawnCards (was count={_drawnCards.Count})");
        _drawnCards = [];
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private async Task TryAutoPlayFromDrawn(PlayerChoiceContext choiceContext)
    {
        var handCards = PileType.Hand.GetPile(Owner).Cards;
        var selectableCards = _drawnCards.Where(c => c.CanPlay() && handCards.Contains(c)).ToList();

        Log.Info($"[GuileCandle] TryAutoPlayFromDrawn: _drawnCards=[{string.Join(", ", _drawnCards.Select(c => c.Id))}], selectableCards=[{string.Join(", ", selectableCards.Select(c => c.Id))}]");

        if (selectableCards.Count == 0)
        {
            Log.Info($"[GuileCandle] No selectable cards in hand, returning early");
            return;
        }

        Flash();

        var selectableSet = selectableCards.ToHashSet();
        Log.Info($"[GuileCandle] Calling CardSelectCmd.FromHand with ref-based filter, selectable=[{string.Join(", ", selectableSet.Select(c => c.Id))}]...");
        var chosen = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(new LocString("card_selection", "NEUVILLETTE-GUILE_CANDLE.selectionScreenPrompt"), 1),
            c => selectableSet.Contains(c),
            this);
        Log.Info($"[GuileCandle] CardSelectCmd.FromHand returned: chosen count={chosen?.Count() ?? 0}");

        var selected = chosen.FirstOrDefault()!;
        if (selected == null)
        {
            Log.Info($"[GuileCandle] No card selected, returning early");
            return;
        }

        Log.Info($"[GuileCandle] Selected card: {selected.Id}");

        var combatState = Owner.Creature?.CombatState;
        if (combatState == null)
        {
            Log.Info($"[GuileCandle] combatState is null, returning early");
            return;
        }

        var target = GetTarget(selected, combatState);
        Log.Info($"[GuileCandle] Target: {(target != null ? target.GetType().Name : "null")}, Card TargetType: {selected.TargetType}");

        await selected.SpendResources();
        await CardCmd.AutoPlay(choiceContext, selected, target, AutoPlayType.Default, skipXCapture: true);

        Log.Info($"[GuileCandle] Card auto-played. Resetting _drawnCards.");
        _drawnCards = [];
        InvokeDisplayAmountChanged();
    }

    private Creature? GetTarget(CardModel card, ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => combatState.Allies.FirstOrDefault(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature),
            TargetType.AnyPlayer => Owner.Creature,
            _ => null,
        };
    }
}