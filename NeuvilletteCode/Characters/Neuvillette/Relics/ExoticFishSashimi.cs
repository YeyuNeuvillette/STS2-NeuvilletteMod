using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class ExoticFishSashimi : BaseRelic, IModRightClickableRelic
{
    private const int MaxTotalUses = 3;

    private bool _isUsedUp;

    [SavedProperty]
    public int ExoticFishSashimi_TotalUses { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsUsedUp => _isUsedUp;

    public override bool ShowCounter => !_isUsedUp;

    public override int DisplayAmount => MaxTotalUses - ExoticFishSashimi_TotalUses;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DynamicVar("Uses", MaxTotalUses) };

    [SavedProperty]
    public bool IsUsedUpProp
    {
        get => _isUsedUp;
        set
        {
            AssertMutable();
            _isUsedUp = value;
            if (_isUsedUp)
                Status = RelicStatus.Disabled;
        }
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        if (!CombatManager.Instance.IsInProgress)
            return false;
        if (context.Player != base.Owner)
            return false;
        return true;
    }

    public override Task BeforeCombatStart()
    {
        base.DynamicVars["Uses"].BaseValue = MaxTotalUses - ExoticFishSashimi_TotalUses;
        return Task.CompletedTask;
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (ExoticFishSashimi_TotalUses >= MaxTotalUses)
            return;

        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null) return;

        Flash();
        ExoticFishSashimi_TotalUses++;
        base.DynamicVars["Uses"].BaseValue = MaxTotalUses - ExoticFishSashimi_TotalUses;
        InvokeDisplayAmountChanged();

        await CreatureCmd.LoseMaxHp(context.PlayerChoiceContext!, ownerCreature, 6m, false);
        await PowerCmd.Apply<IntangiblePower>(context.PlayerChoiceContext!, ownerCreature, 1, ownerCreature, null);

        if (ExoticFishSashimi_TotalUses >= MaxTotalUses)
        {
            IsUsedUpProp = true;
        }
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context) => !_isUsedUp && ExoticFishSashimi_TotalUses < MaxTotalUses;
}