using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Monsters.Powers;

[RegisterPower]
public sealed class HostilityPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Owner != null ? (int)(Owner.MaxHp * Amount / 100m) : 0;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            int threshold = Owner != null ? (int)(Owner.MaxHp * Amount / 100m) : 0;
            yield return new DynamicVar("Threshold", threshold);
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner != null)
        {
            Owner.MaxHpChanged += OnMaxHpChanged;
        }
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        oldOwner.MaxHpChanged -= OnMaxHpChanged;
        return Task.CompletedTask;
    }

    private void OnMaxHpChanged(int oldMaxHp, int newMaxHp)
    {
        if (DynamicVars.TryGetValue("Threshold", out var thresholdVar))
        {
            thresholdVar.BaseValue = Owner != null ? (int)(Owner.MaxHp * Amount / 100m) : 0;
        }
        InvokeDisplayAmountChanged();
    }
}