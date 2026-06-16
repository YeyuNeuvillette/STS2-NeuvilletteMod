using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

public abstract class SurgeCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : NeuvilletteCard(baseCost, type, rarity, target, showInCardLibrary)
{
    protected abstract int BaseSurgeValue { get; }
    protected virtual int UpgradeSurgeValue => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Surge", BaseSurgeValue)
    ];

    protected async Task ApplySurgeLogic(PlayerChoiceContext context)
    {
        var value = DynamicVars["Surge"].BaseValue;
        var livingWaterAmount = Owner.Creature.GetPowerAmount<LivingWaterPower>();
        var totalSurge = value + livingWaterAmount;

        await CreatureCmd.Heal(Owner.Creature, totalSurge);
        await PowerCmd.Apply<SurgePower>(context, Owner.Creature, totalSurge, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        if (UpgradeSurgeValue > 0)
            DynamicVars["Surge"].UpgradeValueBy(UpgradeSurgeValue);
    }
}