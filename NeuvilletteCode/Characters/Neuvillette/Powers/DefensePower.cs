using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class DefensePower : NeuvillettePower
{
    private sealed class Data
    {
        public int RemainingActivations;
        public int LastVigorAmount;
        public bool IsRestoring;
        public VigorPower? RestoredVigorPower;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool AllowNegative => true;

    protected override object InitInternalData() => new Data();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        GD.Print($"[DefensePower] AfterApplied called. Owner: {Owner?.Name}, Amount: {Amount}");
        if (Owner == null)
        {
            GD.Print($"[DefensePower] AfterApplied: Owner is null, returning");
            await base.AfterApplied(applier, cardSource);
            return;
        }
        
        var data = GetInternalData<Data>();
        data.RemainingActivations = (int)Amount;
        var vigorPower = Owner.GetPower<VigorPower>();
        data.LastVigorAmount = vigorPower?.Amount ?? 0;
        GD.Print($"[DefensePower] AfterApplied: initialized RemainingActivations to {data.RemainingActivations}, LastVigorAmount to {data.LastVigorAmount}");
        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        GD.Print($"[DefensePower] AfterSideTurnStart called. Owner: {Owner?.Name}, Side: {side}, Amount: {Amount}");
        if (Owner == null || side != Owner.Side)
        {
            GD.Print($"[DefensePower] AfterSideTurnStart: Owner is null or side mismatch, returning");
            return;
        }

        var data = GetInternalData<Data>();
        data.RemainingActivations = (int)Amount;
        var vigorPower = Owner.GetPower<VigorPower>();
        data.LastVigorAmount = vigorPower?.Amount ?? 0;
        GD.Print($"[DefensePower] AfterSideTurnStart: reset RemainingActivations to {data.RemainingActivations}, LastVigorAmount to {data.LastVigorAmount}");
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        var data = GetInternalData<Data>();

        if (power is DefensePower defensePower && defensePower.Owner == Owner)
        {
            GD.Print($"[DefensePower] AfterPowerAmountChanged: DefensePower itself changed by {amount}, from {Amount - amount} to {Amount}");
            data.RemainingActivations = (int)Amount;
            GD.Print($"[DefensePower] AfterPowerAmountChanged: updated RemainingActivations to {data.RemainingActivations}");
            return;
        }

        if (power is not VigorPower vigorPower || vigorPower.Owner != Owner)
        {
            return;
        }

        GD.Print($"[DefensePower] AfterPowerAmountChanged: VigorPower changed by {amount}, from {data.LastVigorAmount} to {vigorPower.Amount}, IsRestoring: {data.IsRestoring}, RemainingActivations: {data.RemainingActivations}");
        GD.Print($"[DefensePower] AfterPowerAmountChanged: VigorPower instance: {vigorPower.GetHashCode()}, Owner: {vigorPower.Owner?.Name}");

        if (data.IsRestoring)
        {
            GD.Print($"[DefensePower] AfterPowerAmountChanged: currently restoring, skipping detection");
            data.IsRestoring = false;
            data.LastVigorAmount = vigorPower.Amount;
            GD.Print($"[DefensePower] AfterPowerAmountChanged: updated LastVigorAmount to {data.LastVigorAmount}");
            return;
        }

        if (data.LastVigorAmount > 0 && vigorPower.Amount == 0)
        {
            GD.Print($"[DefensePower] AfterPowerAmountChanged: VigorPower was removed (from {data.LastVigorAmount} to 0)");
            
            if (data.RemainingActivations > 0)
            {
                GD.Print($"[DefensePower] AfterPowerAmountChanged: restoring VigorPower with {data.LastVigorAmount} layers");
                data.IsRestoring = true;
                
                var currentVigorPowers = Owner.GetPowerInstances<VigorPower>();
                GD.Print($"[DefensePower] AfterPowerAmountChanged: current VigorPowers count before restore: {currentVigorPowers.Count()}");
                
                var oldVigorPower = vigorPower;
                data.RestoredVigorPower = oldVigorPower;
                
                await PowerCmd.Remove(oldVigorPower);
                GD.Print($"[DefensePower] AfterPowerAmountChanged: removed old VigorPower instance {oldVigorPower.GetHashCode()}");
                
                await PowerCmd.Apply<VigorPower>(choiceContext, Owner, data.LastVigorAmount, null, null);
                
                var newVigorPowers = Owner.GetPowerInstances<VigorPower>();
                GD.Print($"[DefensePower] AfterPowerAmountChanged: VigorPowers count after restore: {newVigorPowers.Count()}");
                VigorPower? newVigorPower = null;
                foreach (var vp in newVigorPowers)
                {
                    GD.Print($"[DefensePower] AfterPowerAmountChanged: VigorPower instance: {vp.GetHashCode()}, Amount: {vp.Amount}, Owner: {vp.Owner?.Name}");
                    newVigorPower = vp;
                }
                
                data.RemainingActivations--;
                GD.Print($"[DefensePower] AfterPowerAmountChanged: RemainingActivations now {data.RemainingActivations}");
                
                var selfDefensePower = Owner.GetPower<DefensePower>();
                if (selfDefensePower != null)
                {
                    await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), selfDefensePower, -1m, null, null);
                    GD.Print($"[DefensePower] AfterPowerAmountChanged: removed 1 layer from DefensePower");
                }
                
                if (newVigorPower != null)
                {
                    data.LastVigorAmount = newVigorPower.Amount;
                    GD.Print($"[DefensePower] AfterPowerAmountChanged: updated LastVigorAmount to {data.LastVigorAmount} (from new VigorPower)");
                }
                return;
            }
            else
            {
                GD.Print($"[DefensePower] AfterPowerAmountChanged: no remaining activations, cannot restore");
            }
        }

        data.LastVigorAmount = vigorPower.Amount;
        GD.Print($"[DefensePower] AfterPowerAmountChanged: updated LastVigorAmount to {data.LastVigorAmount}");
    }
}