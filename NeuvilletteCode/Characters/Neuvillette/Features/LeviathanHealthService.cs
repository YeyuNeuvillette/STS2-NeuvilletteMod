using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Neuvillette.Characters.Neuvillette.Features;

public static class LeviathanHealthService
{
    public const decimal InfiniteHpValue = 999999999m;

    public static bool IsInfinite(Creature creature) =>
        creature.HpDisplay != HpDisplay.Normal || creature.MaxHp >= InfiniteHpValue;
}
