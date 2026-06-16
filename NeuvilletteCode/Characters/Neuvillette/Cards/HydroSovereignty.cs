using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class HydroSovereignty() : NeuvilletteCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.Surge
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Surge", 2m),
        new DynamicVar("Times", 4m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var surgeValue = DynamicVars["Surge"].BaseValue;
        var livingWaterAmount = Owner.Creature.GetPowerAmount<LivingWaterPower>();
        var totalSurge = surgeValue + livingWaterAmount;
        var repeatTimes = IsUpgraded ? 6 : (int)DynamicVars["Times"].BaseValue;

        for (int i = 0; i < repeatTimes; i++)
        {
            await CreatureCmd.Heal(Owner.Creature, totalSurge);
            await PowerCmd.Apply<SurgePower>(choiceContext, Owner.Creature, totalSurge, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
    }
}