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
    private const decimal InfiniteHpValue = 999999999m;

    private int storedMaxHp;
    private decimal storedCurrentHp;
    private bool isInfiniteHpActive;
    private decimal hpBeforeActivation;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => (int)Amount;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private static bool IsInfiniteHp(Creature creature)
    {
        return creature.MaxHp >= InfiniteHpValue;
    }

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        Log.Info($"[LeviathanFormPower] BeforeApplied START: target.CurrentHp={target.CurrentHp}, target.MaxHp={target.MaxHp}");
        
        if (IsInfiniteHp(target))
        {
            Log.Info($"[LeviathanFormPower] BeforeApplied: Detected infinite HP state, deactivating all LeviathanFormPower infinite HP");
            
            var leviathanPowers = target.Powers.OfType<LeviathanFormPower>().ToList();
            foreach (var power in leviathanPowers)
            {
                if (power != this)
                {
                    Log.Info($"[LeviathanFormPower] BeforeApplied: Deactivating other LeviathanFormPower's infinite HP (isInfiniteHpActive={power.isInfiniteHpActive})");
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

        if (isInfiniteHpActive || IsInfiniteHp(Owner))
        {
            Log.Info($"[LeviathanFormPower] AfterSideTurnStart: Deactivating infinite HP (isInfiniteHpActive={isInfiniteHpActive}, MaxHp={Owner.MaxHp})");
            await DeactivateInfiniteHpInternal();
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
        if (isInfiniteHpActive || (Owner != null && IsInfiniteHp(Owner)))
        {
            Log.Info($"[LeviathanFormPower] AfterCombatEnd: Deactivating infinite HP (isInfiniteHpActive={isInfiniteHpActive}, MaxHp={Owner?.MaxHp})");
            await DeactivateInfiniteHpInternal();
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        bool wasInfiniteHpActive = isInfiniteHpActive || (oldOwner != null && IsInfiniteHp(oldOwner));
        
        Log.Info($"[LeviathanFormPower] AfterRemoved: wasInfiniteHpActive={wasInfiniteHpActive}, isInfiniteHpActive={isInfiniteHpActive}, oldOwner.MaxHp={oldOwner?.MaxHp}");
        
        if (wasInfiniteHpActive)
            await DeactivateInfiniteHpInternal();
        
        if (wasInfiniteHpActive && oldOwner != null && !oldOwner.IsDead)
        {
            Log.Info($"[LeviathanFormPower] AfterRemoved: Setting oldOwner.CurrentHp to 0 to ensure death");
            oldOwner.LoseHpInternal(oldOwner.CurrentHp, ValueProp.Unblockable | ValueProp.Unpowered);
        }
        
        Log.Info($"[LeviathanFormPower] AfterRemoved END: oldOwner.CurrentHp={oldOwner?.CurrentHp}, oldOwner.MaxHp={oldOwner?.MaxHp}");
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
        await CreatureCmd.SetMaxAndCurrentHp(Owner, InfiniteHpValue);
        
        Log.Info($"[LeviathanFormPower] ActivateInfiniteHp: After SetMaxAndCurrentHp, Owner.CurrentHp={Owner.CurrentHp}, Owner.MaxHp={Owner.MaxHp}");
    }

    private async Task DeactivateInfiniteHpInternal()
    {
        Log.Info($"[LeviathanFormPower] DeactivateInfiniteHp: isInfiniteHpActive={isInfiniteHpActive}, Owner.CurrentHp={Owner?.CurrentHp}, Owner.MaxHp={Owner?.MaxHp}, storedMaxHp={storedMaxHp}, storedCurrentHp={storedCurrentHp}");
        
        if (!isInfiniteHpActive && (Owner == null || !IsInfiniteHp(Owner)))
            return;

        if (Owner == null)
            return;

        Flash();
        Owner.HpDisplay = HpDisplay.Normal;

        if (storedMaxHp > 0 && storedMaxHp < InfiniteHpValue)
        {
            await CreatureCmd.SetMaxHp(Owner, storedMaxHp);
            await CreatureCmd.SetCurrentHp(Owner, Math.Min(storedCurrentHp, storedMaxHp));
        }
        else
        {
            Log.Info($"[LeviathanFormPower] DeactivateInfiniteHp: storedMaxHp={storedMaxHp} is invalid, using fallback");
            var fallbackMaxHp = Owner.Player?.Character?.StartingHp ?? 50;
            await CreatureCmd.SetMaxHp(Owner, fallbackMaxHp);
            await CreatureCmd.SetCurrentHp(Owner, Math.Min(1, fallbackMaxHp));
        }

        isInfiniteHpActive = false;
        
        Log.Info($"[LeviathanFormPower] DeactivateInfiniteHp: After restore, Owner.CurrentHp={Owner.CurrentHp}, Owner.MaxHp={Owner.MaxHp}");
    }
}