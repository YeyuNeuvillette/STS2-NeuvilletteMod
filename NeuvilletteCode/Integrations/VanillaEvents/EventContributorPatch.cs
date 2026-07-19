using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using Neuvillette.Api;
using Neuvillette.Infrastructure;

namespace Neuvillette.Integrations.VanillaEvents;

[HarmonyPatch(typeof(EventModel), "GenerateInitialOptionsWrapper")]
internal static class EventContributorPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void ApplyContributors(EventModel __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__result != null && GameCompatibility.IsNeuvillette(__instance.Owner))
            __result = ApiRegistry.ApplyEventContributors(__instance, __result.ToArray());
    }
}
