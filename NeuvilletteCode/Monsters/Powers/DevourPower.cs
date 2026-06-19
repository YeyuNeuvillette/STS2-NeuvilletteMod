using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Monsters.Powers;

[RegisterPower]
public sealed class DevourPower : NeuvillettePower
{
    private const decimal DevourPercent = 25m;
    private const string BonusDamageKey = "BonusDamage";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int DisplayAmount => DynamicVars[BonusDamageKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar(BonusDamageKey, 0m) };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (Target != null && dealer != Target) return;
        if (cardSource == null || cardSource.Type != CardType.Attack) return;
        if (!props.IsPoweredAttack()) return;
        if (result.UnblockedDamage <= 0) return;

        int bonusAmount = (int)(result.UnblockedDamage * DevourPercent / 100m);
        if (bonusAmount > 0)
        {
            Flash();
            DynamicVars[BonusDamageKey].BaseValue += bonusAmount;
            InvokeDisplayAmountChanged();
        }
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 0m;
        if (target != Target) return 0m;
        if (!props.IsPoweredAttack()) return 0m;

        return DynamicVars[BonusDamageKey].BaseValue;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;

        DynamicVars[BonusDamageKey].BaseValue = 0m;
        InvokeDisplayAmountChanged();
    }
}