using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class IroncladEvidence() : SubmitCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.SourcewaterDroplet,NeuvilletteKeywords.Submit];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SourcewaterDroplet>(0m),
        new CardsVar(0)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = EnergyCost.CapturedXValue;
        if (xValue <= 0)
            return;

        var droplets = Owner.Creature.GetPower<SourcewaterDroplet>();
        if (droplets == null)
            return;

        var consume = Math.Min(xValue, (int)droplets.Amount);
        if (consume <= 0)
            return;

        await PowerCmd.ModifyAmount(choiceContext, droplets, -consume, null, null);

        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "TO_SUBMIT"), consume),
            null,
            this);

        foreach (var selectedCard in selectedCards)
            await PerformSubmit(choiceContext, selectedCard);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}