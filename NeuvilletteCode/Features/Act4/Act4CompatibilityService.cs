using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
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

    internal static bool TryTruncateActsForNonSwordHolders(RunState state)
    {
        if (state.CurrentActIndex != 2)
            return false;

        if (state.Players.All(player => player.GetRelic<NarzissenkreuzSword>() != null))
            return false;

        var truncatedActs = state.Acts.Take(3).ToList();
        GameCompatibility.SetActs(state, truncatedActs);
        return true;
    }
}
