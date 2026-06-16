using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Liquefaction() : NeuvilletteCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.Surge
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var blockAmount = Owner.Creature.Block;
        var livingWaterAmount = Owner.Creature.GetPowerAmount<LivingWaterPower>();
        var totalSurge = blockAmount + livingWaterAmount;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (blockAmount > 0)
        {
            await CreatureCmd.LoseBlock(Owner.Creature, blockAmount);
        }

        if (totalSurge > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, totalSurge);
            await PowerCmd.Apply<SurgePower>(choiceContext, Owner.Creature, totalSurge, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}