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
public sealed class ContemptOfCourtPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power is not SourcewaterDroplet || amount <= 0m)
            return;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, Owner, null);
        await PowerCmd.Decrement(this);
    }
}