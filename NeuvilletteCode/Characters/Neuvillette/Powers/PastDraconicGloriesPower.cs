using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class PastDraconicGloriesPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageMultiplierVar(),
        new AdditionalHpLossVar()
    };

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DisplayAmountChanged += OnDisplayAmountChanged;
        UpdateDynamicVars();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        DisplayAmountChanged -= OnDisplayAmountChanged;
        return Task.CompletedTask;
    }

    private void OnDisplayAmountChanged()
    {
        UpdateDynamicVars();
    }

    private void UpdateDynamicVars()
    {
        if (DynamicVars.TryGetValue("DamageMultiplier", out var damageMultiplier))
        {
            damageMultiplier.BaseValue = 1m + Amount;
        }
        if (DynamicVars.TryGetValue("AdditionalHpLoss", out var additionalHpLoss))
        {
            additionalHpLoss.BaseValue = Amount * 2m;
        }
    }

    private class DamageMultiplierVar : DynamicVar
    {
        public DamageMultiplierVar() : base("DamageMultiplier", 1m)
        {
        }
    }

    private class AdditionalHpLossVar : DynamicVar
    {
        public AdditionalHpLossVar() : base("AdditionalHpLoss", 2m)
        {
        }
    }
}