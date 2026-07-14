using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using Neuvillette.Characters.Neuvillette.Relics;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch]
public static class WishShopPatch
{
    private const int WishCost = 100;

    [HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant))]
    [HarmonyPostfix]
    public static void Postfix_CreateForNormalMerchant(Player player, MerchantInventory __result)
    {
        if (!ShouldAddWishToShop(player)) return;

        if (__result.RelicEntries.Any(IsWishEntry)) return;

        var wishRelic = ModelDb.Relic<Wish>().ToMutable();
        var wishEntry = new MerchantRelicEntry(wishRelic, player);
        __result.AddRelicEntry(wishEntry);
    }

    [HarmonyPatch(typeof(NMerchantInventory), "Initialize")]
    [HarmonyPrefix]
    public static void Prefix_Initialize(NMerchantInventory __instance, MerchantInventory inventory)
    {
        if (!inventory.RelicEntries.Any(IsWishEntry)) return;

        EnsureWishRelicSlot(__instance, inventory);
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ShouldRefillMerchantEntry))]
    [HarmonyPrefix]
    public static bool Prefix_ShouldRefillMerchantEntry(MerchantEntry entry, ref bool __result)
    {
        if (!IsWishEntry(entry)) return true;

        __result = false;
        return false;
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyMerchantPrice))]
    [HarmonyPrefix]
    public static bool Prefix_ModifyMerchantPrice(MerchantEntry entry, ref decimal __result)
    {
        if (!IsWishEntry(entry)) return true;

        __result = WishCost;
        return false;
    }

    private static void EnsureWishRelicSlot(NMerchantInventory merchantInventory, MerchantInventory inventory)
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

    private static bool ShouldAddWishToShop(Player player)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled()) return false;

        if (player.GetRelic<Persona>() == null) return false;

        if (player.GetRelic<Wish>() != null) return false;

        return true;
    }

    private static bool IsWishEntry(MerchantEntry entry)
    {
        if (entry is MerchantRelicEntry relicEntry)
        {
            return relicEntry.Model is Wish;
        }
        return false;
    }
}