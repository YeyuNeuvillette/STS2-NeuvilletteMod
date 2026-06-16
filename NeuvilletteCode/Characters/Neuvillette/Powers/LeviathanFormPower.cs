using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class LeviathanFormPower : NeuvillettePower
{
    private int storedMaxHp;
    private decimal storedCurrentHp;
    private bool isInfiniteHpActive;
    private decimal hpBeforeActivation;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => (int)Amount;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        Log.Info($"[LeviathanFormPower] BeforeApplied START: target.CurrentHp={target.CurrentHp}, target.MaxHp={target.MaxHp}");
        
        if (target.MaxHp >= 999999999m)
        {
            Log.Info($"[LeviathanFormPower] BeforeApplied: Detected infinite HP state, deactivating all LeviathanFormPower infinite HP");
            
            var leviathanPowers = target.Powers.OfType<LeviathanFormPower>().ToList();
            foreach (var power in leviathanPowers)
            {
                if (power.isInfiniteHpActive && power != this)
                {
                    Log.Info($"[LeviathanFormPower] BeforeApplied: Deactivating other LeviathanFormPower's infinite HP");
                    await power.DeactivateInfiniteHpInternal();
                }
            }
        }
        
        hpBeforeActivation = target.CurrentHp;
        Log.Info($"[LeviathanFormPower] BeforeApplied END: target.CurrentHp={target.CurrentHp}, hpBeforeActivation={hpBeforeActivation}, amount={amount}");
    }

    public override async Task AfterApplied(Creature? applier, CardModel? sourceCard)
    {
        Log.Info($"[LeviathanFormPower] AfterApplied: Owner.CurrentHp={Owner.CurrentHp}, Owner.MaxHp={Owner.MaxHp}, isInfiniteHpActive={isInfiniteHpActive}");
        await ActivateInfiniteHp();
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        Log.Info($"[LeviathanFormPower] AfterSideTurnStart: side={side}, Owner.Side={Owner.Side}, Amount={Amount}, isInfiniteHpActive={isInfiniteHpActive}, Owner.CurrentHp={Owner.CurrentHp}");
        
        if (side != Owner.Side)
            return;

        if (isInfiniteHpActive)
        {
            Log.Info($"[LeviathanFormPower] AfterSideTurnStart: Deactivating infinite HP");
            await DeactivateInfiniteHp();
        }

        if (Amount > 1m)
        {
            Log.Info($"[LeviathanFormPower] AfterSideTurnStart: Decrementing, Amount={Amount}");
            await PowerCmd.Decrement(this);
            return;
        }

        Log.Info($"[LeviathanFormPower] AfterSideTurnStart: Removing and reapplying power");
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<LeviathanFormPower>(new ThrowingPlayerChoiceContext(), Owner, 5m, Owner, null);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (isInfiniteHpActive)
            await DeactivateInfiniteHp();
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        Log.Info($"[LeviathanFormPower] AfterRemoved: isInfiniteHpActive={isInfiniteHpActive}, Owner.CurrentHp={Owner?.CurrentHp}, Owner.MaxHp={Owner?.MaxHp}");
        
        bool wasInfiniteHpActive = isInfiniteHpActive;
        if (wasInfiniteHpActive)
            await DeactivateInfiniteHp();
        
        if (wasInfiniteHpActive && Owner != null && !Owner.IsDead)
        {
            Log.Info($"[LeviathanFormPower] AfterRemoved: Setting Owner.CurrentHp to 0 to ensure death");
            Owner.LoseHpInternal(Owner.CurrentHp, ValueProp.Unblockable | ValueProp.Unpowered);
        }
        
        Log.Info($"[LeviathanFormPower] AfterRemoved END: Owner.CurrentHp={Owner?.CurrentHp}, Owner.MaxHp={Owner?.MaxHp}");
    }

    private async Task ActivateInfiniteHp()
    {
        Log.Info($"[LeviathanFormPower] ActivateInfiniteHp: isInfiniteHpActive={isInfiniteHpActive}, Owner.CurrentHp={Owner.CurrentHp}, Owner.MaxHp={Owner.MaxHp}, hpBeforeActivation={hpBeforeActivation}");
        
        if (isInfiniteHpActive)
            return;

        Flash();
        isInfiniteHpActive = true;
        storedMaxHp = Owner.MaxHp;
        storedCurrentHp = hpBeforeActivation;

        Log.Info($"[LeviathanFormPower] ActivateInfiniteHp: storedMaxHp={storedMaxHp}, storedCurrentHp={storedCurrentHp}");
        
        Owner.HpDisplay = HpDisplay.InfiniteWithoutNumbers;
        await CreatureCmd.SetMaxAndCurrentHp(Owner, 999999999m);
        
        Log.Info($"[LeviathanFormPower] ActivateInfiniteHp: After SetMaxAndCurrentHp, Owner.CurrentHp={Owner.CurrentHp}, Owner.MaxHp={Owner.MaxHp}");
    }

    private async Task DeactivateInfiniteHp()
    {
        await DeactivateInfiniteHpInternal();
    }

    private async Task DeactivateInfiniteHpInternal()
    {
        Log.Info($"[LeviathanFormPower] DeactivateInfiniteHp: isInfiniteHpActive={isInfiniteHpActive}, Owner.CurrentHp={Owner.CurrentHp}, Owner.MaxHp={Owner.MaxHp}, storedMaxHp={storedMaxHp}, storedCurrentHp={storedCurrentHp}");
        
        if (!isInfiniteHpActive)
            return;

        Flash();
        isInfiniteHpActive = false;
        Owner.HpDisplay = HpDisplay.Normal;

        await CreatureCmd.SetMaxHp(Owner, storedMaxHp);
        await CreatureCmd.SetCurrentHp(Owner, storedCurrentHp);
        
        Log.Info($"[LeviathanFormPower] DeactivateInfiniteHp: After restore, Owner.CurrentHp={Owner.CurrentHp}, Owner.MaxHp={Owner.MaxHp}");
    }
}