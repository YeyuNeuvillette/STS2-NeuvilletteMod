using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Liquefaction() : NeuvilletteCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override bool GainsBlock => true;

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.Surge
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var blockAmount = Owner.Creature.Block;
        var livingWaterAmount = Owner.Creature.GetPowerAmount<LivingWaterPower>();
        var totalSurge = blockAmount + livingWaterAmount;

        if (blockAmount > 0)
        {
            await CreatureCmd.LoseBlock(choiceContext, Owner.Creature, blockAmount, null);
        }

        if (totalSurge > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, totalSurge);
            await PowerCmd.Apply<SurgePower>(choiceContext, Owner.Creature, totalSurge, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
