using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Neuvillette.Characters.Neuvillette.Powers;
using Neuvillette.Monsters.Afflictions;
using Neuvillette.Monsters.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Monsters.Powers;

[RegisterPower]
public sealed class AppetitePower : NeuvillettePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;

        var player = Owner.Player;
        if (player == null) return;

        var hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Count == 0) return;

        int cardsToAffect = Math.Min((int)Amount, hand.Cards.Count);
        var candidates = hand.Cards.Where(c => c.IsTransformable && c.Type != CardType.Status).ToList();
        if (candidates.Count == 0) return;

        var rng = combatState.RunState.Rng.Niche;
        var selected = new List<CardModel>();
        var pool = new List<CardModel>(candidates);

        for (int i = 0; i < cardsToAffect && pool.Count > 0; i++)
        {
            var card = rng.NextItem(pool);
            if (card != null)
            {
                selected.Add(card);
                pool.Remove(card);
            }
        }

        await CardCmd.AfflictAndPreview<CravingAffliction>(selected, 1, CardPreviewStyle.HorizontalLayout);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;

        var player = Owner.Player;
        if (player == null) return;

        var allCards = player.PlayerCombatState?.AllCards.ToList() ?? new List<CardModel>();
        var cravingCardsInHand = allCards
            .Where(c => c.Affliction is CravingAffliction && c.Pile?.Type == PileType.Hand)
            .ToList();

        foreach (var card in cravingCardsInHand)
        {
            CardCmd.ClearAffliction(card);
            await CardPileCmd.RemoveFromDeck(card, showPreview: true);
            var riftCard = card.CardScope?.CreateCard<RiftCard>(player);
            if (riftCard != null)
            {
                await CardPileCmd.AddGeneratedCardToCombat(riftCard, PileType.Hand, player);
            }
        }

        var remainingCraving = allCards.Where(c => c.Affliction is CravingAffliction).ToList();
        foreach (var card in remainingCraving)
        {
            CardCmd.ClearAffliction(card);
        }
    }
}