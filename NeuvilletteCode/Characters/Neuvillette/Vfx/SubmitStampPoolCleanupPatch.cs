using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Neuvillette.Characters.Neuvillette.Vfx;

/// <summary>
/// NCard instances are pooled. Custom children survive that pooling unless they
/// are explicitly detached, which can transfer a submitted seal to the next card
/// model displayed by the same NCard. Clean the complete effect up synchronously
/// at the pool boundary rather than relying on deferred ExitTree cleanup.
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
internal static class SubmitStampPoolCleanupPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance)
    {
        foreach (var guard in __instance.GetChildren().OfType<NSubmittedPreviewLayerGuard>().ToArray())
            guard.RestoreAndRemoveForPool();

        if (!__instance.IsNodeReady())
            return;

        foreach (var child in __instance.Body.GetChildren().ToArray())
        {
            if (child is not NSubmitStampVfx &&
                child is not NSubmittedBalanceSeal &&
                child is not NSubmittedSealBurst)
            {
                continue;
            }

            __instance.Body.RemoveChild(child);
            child.QueueFreeSafelyNoPool();
        }
    }
}
