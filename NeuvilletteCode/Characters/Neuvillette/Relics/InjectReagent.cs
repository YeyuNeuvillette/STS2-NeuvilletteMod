using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class InjectReagent : BaseRelic
{
    private int _remainingUses;

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool HasUponPickupEffect => true;
    public override bool ShowCounter => true;
    public override int DisplayAmount => _remainingUses;

    [SavedProperty]
    public int RemainingUses
    {
        get => _remainingUses;
        set
        {
            AssertMutable();
            _remainingUses = value;
            InvokeDisplayAmountChanged();
            if (_remainingUses <= 0)
                Status = RelicStatus.Disabled;
        }
    }

    public override async Task AfterObtained()
    {
        var creature = Owner.Creature;
        var newMaxHp = Math.Max(1, creature.MaxHp / 2);
        await CreatureCmd.SetMaxHp(creature, newMaxHp);
        _remainingUses = 2;
        InvokeDisplayAmountChanged();
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner.Creature)
            return true;
        if (_remainingUses <= 0)
            return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        RemainingUses--;
        await CreatureCmd.Heal(creature, creature.MaxHp);
    }
}