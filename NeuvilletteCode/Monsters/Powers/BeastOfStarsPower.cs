using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Monsters.Powers;

[RegisterPower]
public sealed class BeastOfStarsPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterSideTurnStart(MegaCrit.Sts2.Core.Combat.CombatSide side, System.Collections.Generic.IReadOnlyList<Creature> participants, MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (side != Owner.Side) return;

        if (Amount != 10m)
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, 10m - Amount, null, null);
        }
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0) return;
        if (Target != null && dealer != Target) return;

        decimal healPercent = Amount / 100m;
        int healAmount = Math.Max(1, (int)Math.Round(result.UnblockedDamage * healPercent));
        Flash();
        await CreatureCmd.Heal(Owner, healAmount);
        await PowerCmd.ModifyAmount(choiceContext, this, 10m, null, null);
    }
}