using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class BreathPower : NeuvillettePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not SurgePower surgePower || surgePower.Owner != Owner)
            return;

        if (amount <= 0)
            return;

        var enemies = Owner.CombatState?.HittableEnemies;
        if (enemies != null && enemies.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, enemies, amount,  ValueProp.Unpowered, Owner, null);
        }
    }
}