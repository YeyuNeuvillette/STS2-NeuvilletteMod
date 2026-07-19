using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class AllRiversRunIntoSeaPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Creature ModifyUnblockedDamageTarget(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer)
    {
        if (amount <= 0m
            || Owner.IsDead
            || target == Owner
            || target.Side != Owner.Side
            || !target.IsPlayer
            || target.GetPower<AllRiversRunIntoSeaPower>() is not null)
            return target;

        Flash();
        return Owner;
    }

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m)
            return amount;

        return Math.Min(amount, Math.Max(Owner.CurrentHp - 1m, 0m));
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        return Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature) => creature != Owner;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner || creature.CurrentHp >= 1)
            return;

        Flash();
        await CreatureCmd.Heal(Owner, 1m);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }
}
