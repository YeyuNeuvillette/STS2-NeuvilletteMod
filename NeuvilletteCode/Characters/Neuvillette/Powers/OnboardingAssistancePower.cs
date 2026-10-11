using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using Neuvillette.Features.Difficulty;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

/// <summary>
/// Last-resort safety net for the first ten runs. Each stack is one revive.
/// It runs in the late death-prevention pass so normal resources such as Fairy
/// in a Bottle are consumed first.
/// </summary>
[RegisterPower]
public sealed class OnboardingAssistancePower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldDieLate(Creature creature)
    {
        return creature != Owner || Amount <= 0m;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner || Amount <= 0m)
            return;

        // Consume the charge before healing so a nested death check cannot use
        // the same charge twice. CreatureCmd clamps the result to MaxHp.
        await PowerCmd.Decrement(this);
        NeuvilletteDifficultyService.RecordReviveConsumed(creature);
        await CreatureCmd.Heal(creature, Math.Max(creature.MaxHp * 0.3m, 1m));
    }
}
