using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Patches;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class AssistArrestPower : NeuvillettePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (delta <= 0m || creature != Owner || Owner.Player == null)
            return;

        if (!HealPatch.HpBeforeHeal.TryGetValue(creature, out var hpBefore))
            return;

        HealPatch.HpBeforeHeal.Remove(creature);
        var actualHpGained = creature.CurrentHp - hpBefore;
        if (actualHpGained <= 0m)
            return;

        Flash();
        await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner.Player, actualHpGained, this);
    }
}