using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace Neuvillette.Characters.Neuvillette;

[RegisterSharedCardPool]
public sealed class MelusineCardPool : TypeListCardPoolModel
{
    private static readonly Dictionary<CombatState, HashSet<Type>> RemovedFromPoolInCombat = [];

    public override string Title => "Melusine";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override Color DeckEntryCardColor => Colors.White;
    public override bool IsColorless => true;

    public static void RemoveFromPoolInCombat(CombatState combatState, Type cardType)
    {
        if (!RemovedFromPoolInCombat.TryGetValue(combatState, out var removed))
        {
            removed = [];
            RemovedFromPoolInCombat[combatState] = removed;
        }

        removed.Add(cardType);
    }

    public static bool IsRemovedFromPoolInCombat(CombatState combatState, Type cardType)
    {
        return RemovedFromPoolInCombat.TryGetValue(combatState, out var removed) && removed.Contains(cardType);
    }

    public static IEnumerable<CardModel> GetAvailableCardsForCombat(CombatState combatState)
    {
        var allCards = ModelDb.CardPool<MelusineCardPool>().AllCards;
        var filteredCards = allCards.Where(static card => card.GetType().Name != "SigewinneSticker");

        if (!RemovedFromPoolInCombat.TryGetValue(combatState, out var removed))
            return filteredCards;

        return filteredCards.Where(card => !removed.Contains(card.GetType()));
    }

    public static void CleanupCombat(CombatState combatState)
    {
        RemovedFromPoolInCombat.Remove(combatState);
    }
}
