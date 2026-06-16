using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class InDepthInquiry() : NeuvilletteCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.SourcewaterDroplet];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.Static(StaticHoverTip.ReplayStatic)]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var dropletCount = 0;
        var dropletPower = Owner.Creature.GetPower<SourcewaterDroplet>();
        if (dropletPower != null)
        {
            dropletCount = (int)dropletPower.Amount;
            await PowerCmd.Remove(dropletPower);
        }

        if (dropletCount <= 0)
            return;

        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "NEUVILLETTE-IN_DEPTH_INQUIRY.selectionScreenPrompt"), 1),
            null,
            this);

        foreach (var card in selectedCards)
            card.BaseReplayCount += dropletCount / 2;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}