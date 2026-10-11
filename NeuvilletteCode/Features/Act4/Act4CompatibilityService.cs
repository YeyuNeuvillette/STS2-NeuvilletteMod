using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Neuvillette.Characters.Neuvillette.Act;
using Neuvillette.Characters.Neuvillette.Relics;
using Neuvillette.Infrastructure;

namespace Neuvillette.Features.Act4;

internal static class Act4CompatibilityService
{
    internal static int ClampActIndex(int actIndex)
    {
        return actIndex > 2 ? 2 : actIndex;
    }

    internal static bool ShouldSuppressMusic()
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled())
            return false;

        var state = GameCompatibility.GetRunState();
        return GameCompatibility.IsNeuvilletteAct(state);
    }

    /// <summary>
    /// Makes the Neuvillette act an opt-in route instead of a permanently
    /// registered default act.  The route is normalized by stable model ID so
    /// loading an old save cannot accumulate duplicate copies.
    /// </summary>
    internal static int? NormalizeNeuvilletteActRoute(RunState state)
    {
        var targetAct = ModelDb.Act<NeuvilletteAct>();
        var normalizedActs = state.Acts
            .Where(act => act.Id != targetAct.Id)
            .ToList();
        bool shouldEnterAct = NeuvilletteSettingsStore.IsAct4Enabled()
            && state.Players.Count > 0
            && state.Players.All(player => player.GetRelic<NarzissenkreuzSword>() != null);

        int? targetIndex = null;
        if (shouldEnterAct)
        {
            // During Act 3 this is exactly the next slot.  When the sword is
            // forged earlier, keep the route behind the three standard acts.
            int insertionIndex = Math.Min(
                normalizedActs.Count,
                Math.Max(3, state.CurrentActIndex + 1));
            normalizedActs.Insert(insertionIndex, targetAct);
            targetIndex = insertionIndex;
        }

        bool changed = normalizedActs.Count != state.Acts.Count
            || !normalizedActs.Select(act => act.Id).SequenceEqual(state.Acts.Select(act => act.Id));
        if (changed && !GameCompatibility.SetActs(state, normalizedActs))
        {
            MainFile.Logger.Warn(
                "[Act4Route] Could not normalize the run's act list; leaving the original transition untouched.");
            return null;
        }

        // Do not trust the requested insertion index after a compatibility
        // write. Another implementation may normalize or copy this list.
        targetIndex = shouldEnterAct
            ? state.Acts.ToList().FindIndex(act => act.Id == targetAct.Id)
            : null;
        if (shouldEnterAct && targetIndex < 0)
        {
            MainFile.Logger.Warn(
                "[Act4Route] The Neuvillette act was not present after normalization; leaving the original transition untouched.");
            return null;
        }

        MainFile.Logger.Info(
            $"[Act4Route] enabled={NeuvilletteSettingsStore.IsAct4Enabled()}, sword={shouldEnterAct}, " +
            $"current={state.CurrentActIndex}, target={(targetIndex?.ToString() ?? "none")}, " +
            $"acts=[{string.Join(", ", state.Acts.Select(act => act.Id))}]");
        return targetIndex;
    }
}
