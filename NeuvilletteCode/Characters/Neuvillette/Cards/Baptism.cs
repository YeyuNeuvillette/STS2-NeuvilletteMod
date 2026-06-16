using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Baptism() : SurgeCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.Surge];
    protected override bool HasEnergyCostX => true;
    protected override int BaseSurgeValue => 5;
    protected override int UpgradeSurgeValue => 2; // 升级后从6增加到8

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat([new CardsVar(0)]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = ResolveEnergyXValue();
        if (xValue <= 0)
            return;

        // 执行X次潮涌
        for (int i = 0; i < xValue; i++)
        {
            await ApplySurgeLogic(choiceContext);
        }
    }
}