using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;
using Neuvillette.Api;
using Neuvillette.Characters.Neuvillette.Cards;
using Neuvillette.Characters.Neuvillette.Features;
using Neuvillette.Monsters;

namespace Neuvillette.Characters.Neuvillette;

[RegisterSharedCardPool]
public sealed class MelusineCardPool : TypeListCardPoolModel
{
    public override string Title => "Melusine";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override Color DeckEntryCardColor => Colors.White;
    public override bool IsColorless => true;

    public static void RemoveFromPoolInCombat(CombatState combatState, Type cardType)
    {
        MelusineCombatStateService.Remove(combatState, cardType);
    }

    public static bool IsRemovedFromPoolInCombat(CombatState combatState, Type cardType)
    {
        return MelusineCombatStateService.IsRemoved(combatState, cardType);
    }

    public static IEnumerable<CardModel> GetAvailableCardsForCombat(CombatState combatState)
    {
        var allCards = ModelDb.CardPool<MelusineCardPool>().AllCards;
        var filteredCards = allCards
            .Where(card => IsAvailableForCombat(combatState, card))
            .ToArray();

        return ApiRegistry.ApplyStickerContributors(combatState, filteredCards)
            .Where(card => IsAvailableForCombat(combatState, card))
            .ToArray();
    }

    private static bool IsAvailableForCombat(CombatState combatState, CardModel card)
    {
        var cardType = card.GetType();
        if (cardType == typeof(SigewinneSticker) ||
            MelusineCombatStateService.IsRemoved(combatState, cardType))
        {
            return false;
        }

        var isNarwhalCombat = combatState.Encounter is NarwhalBossEncounter ||
                              combatState.Enemies.Any(enemy => enemy.Monster is AllDevouringNarwhal);
        return !isNarwhalCombat ||
               (cardType != typeof(TalochardSticker) && cardType != typeof(XanaSticker));
    }

    public static void CleanupCombat(CombatState combatState)
    {
        MelusineCombatStateService.Cleanup(combatState);
    }
}
