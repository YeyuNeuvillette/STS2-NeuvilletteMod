using MegaCrit.Sts2.Core.Combat;
using System.Runtime.CompilerServices;

namespace Neuvillette.Characters.Neuvillette.Features;

internal static class MelusineCombatStateService
{
    private sealed class State
    {
        internal HashSet<Type> RemovedCardTypes { get; } = [];
    }

    private static readonly ConditionalWeakTable<CombatState, State> States = new();

    internal static void Remove(CombatState combatState, Type cardType) =>
        States.GetOrCreateValue(combatState).RemovedCardTypes.Add(cardType);

    internal static bool IsRemoved(CombatState combatState, Type cardType) =>
        States.TryGetValue(combatState, out var state) && state.RemovedCardTypes.Contains(cardType);

    internal static void Cleanup(CombatState combatState) => States.Remove(combatState);
}
