using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Combat;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class WaterfallBonsai : BaseRelic
{
    private bool _wasUsed;
    private decimal _currentDamage;
    private int _hpBeforeFatalDamage = 1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsUsedUp => _wasUsed;

    public override bool ShowCounter => !_wasUsed;

    public override int DisplayAmount => (int)_currentDamage;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar("Damage", 30m, ValueProp.Unpowered) };

    [SavedProperty]
    public bool WasUsed
    {
        get => _wasUsed;
        set
        {
            AssertMutable();
            _wasUsed = value;
            if (_wasUsed)
                Status = RelicStatus.Disabled;
        }
    }

    public decimal CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task BeforeCombatStart()
    {
        CurrentDamage = 30m;
        _hpBeforeFatalDamage = 1;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner.Creature)
            return Task.CompletedTask;
        if (_wasUsed)
            return Task.CompletedTask;
        if (delta < 0m)
            _hpBeforeFatalDamage = creature.CurrentHp - (int)delta;
        return Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner.Creature)
            return true;
        if (_wasUsed)
            return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        WasUsed = true;

        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null) return;

        await CreatureCmd.Heal(ownerCreature, _hpBeforeFatalDamage);

        var combatState = ownerCreature.CombatState;
        if (combatState == null) return;

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            combatState.HittableEnemies,
            new DamageVar(_currentDamage, ValueProp.Unpowered),
            ownerCreature);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player) return;
        if (!participants.Contains(Owner.Creature)) return;
        if (_wasUsed) return;

        CurrentDamage += 30m;
    }
}