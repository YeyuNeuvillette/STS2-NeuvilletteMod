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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using Neuvillette.Characters.Base;
using STS2RitsuLib.Interop.AutoRegistration;

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
        if (card is null || card.Owner is not Player { } owner || owner != Owner)
            return;

        _drawnCards.Add(card);
        InvokeDisplayAmountChanged();

        if (_drawnCards.Count < DrawThreshold)
            return;

        var batch = _drawnCards.Take(DrawThreshold).ToList();
        _drawnCards.RemoveRange(0, DrawThreshold);
        InvokeDisplayAmountChanged();

        await ResolveDrawnCards(choiceContext, batch);
    }

    private async Task ResolveDrawnCards(PlayerChoiceContext choiceContext, IReadOnlyList<CardModel> drawnCards)
    {
        if (drawnCards.Count == 0)
            return;

        Flash();

        var selectableCards = drawnCards.Where(IsPlayableForFree).ToList();
        if (selectableCards.Count == 0)
        {
            await DiscardRemainingDrawnCards(choiceContext, drawnCards, selected: null);
            return;
        }

        var chosen = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            selectableCards,
            Owner,
            new CardSelectorPrefs(
                new LocString("card_selection", "NEUVILLETTE-GUILE_CANDLE.selectionScreenPrompt"),
                1));

        var selected = chosen.FirstOrDefault();
        if (selected == null)
            return;

        var combatState = Owner.Creature?.CombatState;
        if (combatState == null)
            return;

        await DiscardRemainingDrawnCards(choiceContext, drawnCards, selected);

        var target = GetTarget(selected, combatState);
        await CardCmd.AutoPlay(choiceContext, selected, target);

        // A selected card can recursively trigger this relic while it is resolving
        // (Summons to Court draws cards). If that changes the active combat effect,
        // the game's normal result-pile cleanup can be skipped. Never leave the
        // auto-played card stranded in the play pile.
        if (selected.Pile?.Type == PileType.Play)
        {
            if (selected.Keywords.Contains(CardKeyword.Exhaust))
                await CardCmd.Exhaust(choiceContext, selected, causedByEthereal: false);
            else if (selected.Type == CardType.Power || selected.IsDupe)
                await CardPileCmd.RemoveFromCombat(selected);
            else
                await CardPileCmd.Add(selected, PileType.Discard);
        }
    }

    private async Task DiscardRemainingDrawnCards(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> drawnCards,
        CardModel? selected)
    {
        var hand = PileType.Hand.GetPile(Owner);
        var cardsToDiscard = drawnCards
            .Where(card => card != selected && card.Pile == hand)
            .ToList();
        await CardCmd.Discard(choiceContext, cardsToDiscard);
    }

    private static bool IsPlayableForFree(CardModel card)
    {
        if (card.CanPlay(out var reason, out _))
            return true;

        var nonResourceReasons = reason
            & ~(UnplayableReason.EnergyCostTooHigh | UnplayableReason.StarCostTooHigh);
        return nonResourceReasons == UnplayableReason.None;
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
