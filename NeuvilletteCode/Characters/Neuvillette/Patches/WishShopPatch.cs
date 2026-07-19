using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using Neuvillette.Features.Shop;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch]
internal static class WishShopPatch
{
    [HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant))]
    [HarmonyPostfix]
    public static void Postfix_CreateForNormalMerchant(Player player, MerchantInventory __result)
    {
        if (!WishShopService.ShouldAddWishToShop(player)) return;
        WishShopService.AddWishEntry(player, __result);
    }

    [HarmonyPatch(typeof(NMerchantInventory), "Initialize")]
    [HarmonyPrefix]
    public static void Prefix_Initialize(NMerchantInventory __instance, MerchantInventory inventory)
    {
        if (!inventory.RelicEntries.Any(WishShopService.IsWishEntry)) return;
        WishShopService.EnsureWishRelicSlot(__instance, inventory);
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ShouldRefillMerchantEntry))]
    [HarmonyPostfix]
    public static void Postfix_ShouldRefillMerchantEntry(MerchantEntry entry, ref bool __result)
    {
        if (WishShopService.IsWishEntry(entry))
            __result = false;
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyMerchantPrice))]
    [HarmonyPostfix]
    public static void Postfix_ModifyMerchantPrice(MerchantEntry entry, ref decimal __result)
    {
        if (WishShopService.IsWishEntry(entry))
            __result = WishShopService.WishCost;
    }
}
