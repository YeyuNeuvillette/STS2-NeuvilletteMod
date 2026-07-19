using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class AssistArrestPower : NeuvillettePower
{
    private decimal? _hpBeforeHeal;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    internal void RecordHpBeforeHeal(decimal hp) => _hpBeforeHeal = hp;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (delta <= 0m || creature != Owner || Owner.Player == null)
            return;

        if (!_hpBeforeHeal.HasValue)
            return;

        var hpBefore = _hpBeforeHeal.Value;
        _hpBeforeHeal = null;
        var actualHpGained = creature.CurrentHp - hpBefore;
        if (actualHpGained <= 0m)
            return;

        Flash();
        await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner.Player, actualHpGained * Amount, this);
    }
}
