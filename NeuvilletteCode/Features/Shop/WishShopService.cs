using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using Neuvillette.Characters.Neuvillette.Relics;

namespace Neuvillette.Features.Shop;

internal static class WishShopService
{
    internal const int WishCost = 100;

    internal static bool ShouldAddWishToShop(Player player)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return false;
        if (player.GetRelic<Persona>() == null) return false;
        if (player.GetRelic<Wish>() != null) return false;
        return true;
    }

    internal static bool IsWishEntry(MerchantEntry entry)
    {
        return entry is MerchantRelicEntry relicEntry && relicEntry.Model is Wish;
    }

    internal static void AddWishEntry(Player player, MerchantInventory inventory)
    {
        if (inventory.RelicEntries.Any(IsWishEntry)) return;

        var wishRelic = ModelDb.Relic<Wish>().ToMutable();
        var wishEntry = new MerchantRelicEntry(wishRelic, player);
        inventory.AddRelicEntry(wishEntry);
    }

    internal static void EnsureWishRelicSlot(NMerchantInventory merchantInventory, MerchantInventory inventory)
    {
        if (!inventory.RelicEntries.Any(IsWishEntry)) return;

        var relicContainer = merchantInventory.GetNodeOrNull<Control>("%Relics");
        if (relicContainer == null) return;

        var existingSlots = relicContainer.GetChildren().OfType<NMerchantRelic>().ToList();
        while (existingSlots.Count < inventory.RelicEntries.Count)
        {
            var template = existingSlots.LastOrDefault();
            if (template == null) break;

            var duplicate = (NMerchantRelic?)template.Duplicate(15);
            if (duplicate == null) break;

            Vector2 offset;
            if (existingSlots.Count >= 2)
            {
                var diff = ((Control)existingSlots[existingSlots.Count - 1]).Position
                         - ((Control)existingSlots[existingSlots.Count - 2]).Position;
                offset = diff.LengthSquared() > 1f ? diff : new Vector2(160f, 0f);
            }
            else
            {
                offset = new Vector2(160f, 0f);
            }

            ((Control)duplicate).Position = ((Control)template).Position + offset;
            relicContainer.AddChild(duplicate, false);
            existingSlots.Add(duplicate);
        }

        MoveCardRemovalDown(merchantInventory);
    }

    private static void MoveCardRemovalDown(NMerchantInventory merchantInventory)
    {
        var cardRemoval = merchantInventory.GetNodeOrNull<Control>("%MerchantCardRemoval");
        if (cardRemoval != null)
        {
            cardRemoval.Position += new Vector2(0f, 60f);
        }
    }
}