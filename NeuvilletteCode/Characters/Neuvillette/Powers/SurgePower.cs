using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class SurgePower : NeuvillettePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var currentHp = Owner.CurrentHp;
        var maxHp = Owner.MaxHp;
        var debt = Amount;
        var minHp = maxHp * 0.5m;
        var targetHp = Math.Max(currentHp - debt, minHp);

        if (targetHp < currentHp)
            await CreatureCmd.SetCurrentHp(Owner, targetHp);
    }
}
