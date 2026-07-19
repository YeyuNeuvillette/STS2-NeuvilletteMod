using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using Neuvillette.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Neuvillette.Characters.Neuvillette.Patches;

[HarmonyPatch(typeof(Trial), "GenerateInitialOptions")]
internal static class TrialPatch
{
    [HarmonyPostfix]
    public static void Postfix(Trial __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner == null)
        {
            return;
        }

        if (!GameCompatibility.IsNeuvillette(__instance.Owner))
        {
            return;
        }

        var mutable = __result as List<EventOption> ?? __result.ToList();
        
        var rejectOption = mutable.FirstOrDefault(o => o.TextKey == "TRIAL.pages.INITIAL.options.REJECT");
        if (rejectOption != null)
        {
            mutable.Remove(rejectOption);
            mutable.Add(new EventOption(__instance, () => NeuvilletteReject(__instance), "TRIAL.pages.INITIAL.options.NEUVILLETTE_REJECT"));
        }
        
        __result = mutable;
    }

    private static Task NeuvilletteReject(Trial @event)
    {
        EventOption[] eventOptions = new EventOption[2]
        {
            new EventOption(@event, () => FollowHeart(@event), "TRIAL.pages.NEUVILLETTE_REJECT.options.FOLLOW_HEART"),
            new EventOption(@event, () => InsistReject(@event), "TRIAL.pages.NEUVILLETTE_REJECT.options.INSIST_REJECT", false, true).ThatWillKillPlayerIf((Player _) => true)
        };
        var description = @event.L10NLookup("TRIAL.pages.NEUVILLETTE_REJECT.description");
        
        var setEventStateMethod = typeof(EventModel).GetMethod("SetEventState", BindingFlags.NonPublic | BindingFlags.Instance);
        if (setEventStateMethod != null)
        {
            setEventStateMethod.Invoke(@event, new object[] { description, eventOptions });
        }
        
        return Task.CompletedTask;
    }

    private static Task InsistReject(Trial @event)
    {
        var doubleDownMethod = typeof(Trial).GetMethod("DoubleDown", BindingFlags.NonPublic | BindingFlags.Instance);
        if (doubleDownMethod != null)
        {
            var result = doubleDownMethod.Invoke(@event, null);
            if (result is Task task)
            {
                return task;
            }
        }
        return Task.CompletedTask;
    }

    private static Task FollowHeart(Trial @event)
    {
        var acceptMethod = typeof(Trial).GetMethod("Accept", BindingFlags.NonPublic | BindingFlags.Instance);
        if (acceptMethod != null)
        {
            acceptMethod.Invoke(@event, null);
        }
        return Task.CompletedTask;
    }
}