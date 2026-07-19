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
using Neuvillette.Characters.Neuvillette.Features;

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
            .Where(static card => card.GetType().Name != "SigewinneSticker")
            .Where(card => !MelusineCombatStateService.IsRemoved(combatState, card.GetType()))
            .ToArray();
        return ApiRegistry.ApplyStickerContributors(combatState, filteredCards);
    }

    public static void CleanupCombat(CombatState combatState)
    {
        MelusineCombatStateService.Cleanup(combatState);
    }
}
