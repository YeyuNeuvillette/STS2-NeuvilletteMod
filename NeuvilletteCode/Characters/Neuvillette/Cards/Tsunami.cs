using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Tsunami() : NeuvilletteCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.Surge];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromPower<SurgePower>()]);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Surge", 0m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var surgePercentage = IsUpgraded ? 0.75m : 0.5m;
        var surgeValue = Owner.Creature.MaxHp * surgePercentage;
        var livingWaterAmount = Owner.Creature.GetPowerAmount<LivingWaterPower>();
        var totalSurge = surgeValue + livingWaterAmount;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.Heal(Owner.Creature, totalSurge);
        await PowerCmd.Apply<SurgePower>(choiceContext, Owner.Creature, totalSurge, Owner.Creature, this);
    }
}