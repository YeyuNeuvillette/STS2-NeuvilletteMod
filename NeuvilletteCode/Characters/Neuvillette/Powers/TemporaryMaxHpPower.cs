using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class TemporaryMaxHpPower : NeuvillettePower
{
    private bool isSettled;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (isSettled)
            return;

        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner, Amount, false);
        isSettled = true;
        Amount = 0;
    }
}
