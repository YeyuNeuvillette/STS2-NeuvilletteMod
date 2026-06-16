using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class ProsecutionPower : NeuvillettePower
{
    private sealed class Data
    {
        public int RemainingActivations;
        public int LastAgilityAmount;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool AllowNegative => true;

    protected override object InitInternalData() => new Data();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        GD.Print($"[ProsecutionPower] AfterApplied called. Owner: {Owner?.Name}, Amount: {Amount}");
        if (Owner == null)
        {
            GD.Print($"[ProsecutionPower] AfterApplied: Owner is null, returning");
            await base.AfterApplied(applier, cardSource);
            return;
        }
        
        var data = GetInternalData<Data>();
        data.RemainingActivations = (int)Amount;
        var agilityPower = Owner.GetPower<AgilityPower>();
        data.LastAgilityAmount = agilityPower?.Amount ?? 0;
        GD.Print($"[ProsecutionPower] AfterApplied: initialized RemainingActivations to {data.RemainingActivations}, LastAgilityAmount to {data.LastAgilityAmount}");
        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        GD.Print($"[ProsecutionPower] AfterSideTurnStart called. Owner: {Owner?.Name}, Side: {side}, Amount: {Amount}");
        if (Owner == null || side != Owner.Side)
        {
            GD.Print($"[ProsecutionPower] AfterSideTurnStart: Owner is null or side mismatch, returning");
            return;
        }

        var data = GetInternalData<Data>();
        data.RemainingActivations = (int)Amount;
        var agilityPower = Owner.GetPower<AgilityPower>();
        data.LastAgilityAmount = agilityPower?.Amount ?? 0;
        GD.Print($"[ProsecutionPower] AfterSideTurnStart: reset RemainingActivations to {data.RemainingActivations}, LastAgilityAmount to {data.LastAgilityAmount}");
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        var data = GetInternalData<Data>();

        if (power is ProsecutionPower prosecutionPower && prosecutionPower.Owner == Owner)
        {
            GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: ProsecutionPower itself changed by {amount}, from {Amount - amount} to {Amount}");
            data.RemainingActivations = (int)Amount;
            GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: updated RemainingActivations to {data.RemainingActivations}");
            return;
        }

        if (power is not AgilityPower agilityPower || agilityPower.Owner != Owner)
        {
            return;
        }

        GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: AgilityPower changed by {amount}, from {data.LastAgilityAmount} to {agilityPower.Amount}");

        if (data.LastAgilityAmount > 0 && agilityPower.Amount == 0)
        {
            GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: AgilityPower was removed (from {data.LastAgilityAmount} to 0)");
            
            if (data.RemainingActivations > 0)
            {
                GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: restoring AgilityPower with {data.LastAgilityAmount} layers");
                await PowerCmd.Apply<AgilityPower>(new ThrowingPlayerChoiceContext(), Owner, data.LastAgilityAmount, null, null);
                data.RemainingActivations--;
                GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: RemainingActivations now {data.RemainingActivations}");
                
                var selfProsecutionPower = Owner.GetPower<ProsecutionPower>();
                if (selfProsecutionPower != null)
                {
                    await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), selfProsecutionPower, -1m, null, null);
                    GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: removed 1 layer from ProsecutionPower");
                }
            }
            else
            {
                GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: no remaining activations, cannot restore");
            }
        }

        data.LastAgilityAmount = agilityPower.Amount;
        GD.Print($"[ProsecutionPower] AfterPowerAmountChanged: updated LastAgilityAmount to {data.LastAgilityAmount}");
    }
}