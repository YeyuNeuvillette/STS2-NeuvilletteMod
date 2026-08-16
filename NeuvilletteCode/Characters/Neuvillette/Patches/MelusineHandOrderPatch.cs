using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(NPlayerHand), nameof(NPlayerHand.Add), typeof(NCard), typeof(int))]
internal static class MelusineHandOrderPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPlayerHand __instance, NHandCardHolder __result)
    {
        CardModel? addedCard = __result.CardModel;
        if (addedCard?.Pool is not MelusineCardPool || addedCard.Pile?.Type != PileType.Hand)
            return;

        // Selected and queued cards remain in the backend hand while their holders temporarily leave this container.
        IReadOnlyList<CardModel> handOrder = addedCard.Pile.Cards;
        var orderedHolders = __instance.CardHolderContainer
            .GetChildren()
            .OfType<NHandCardHolder>()
            .Where(static holder => holder.CardModel != null)
            .OrderBy(holder => IndexOf(handOrder, holder.CardModel!))
            .ToList();

        for (var index = 0; index < orderedHolders.Count; index++)
        {
            var holder = orderedHolders[index];
            if (holder.GetIndex() != index)
                __instance.CardHolderContainer.MoveChildSafely(holder, index);
        }

        __instance.ForceRefreshCardIndices();
    }

    private static int IndexOf(IReadOnlyList<CardModel> cards, CardModel target)
    {
        for (var index = 0; index < cards.Count; index++)
        {
            if (ReferenceEquals(cards[index], target))
                return index;
        }

        return int.MaxValue;
    }
}
