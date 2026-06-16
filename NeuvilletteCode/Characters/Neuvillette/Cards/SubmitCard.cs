using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

public abstract class SubmitCard(
    int energyCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool shouldShowInCardLibrary = true)
    : NeuvilletteCard(energyCost, type, rarity, target, shouldShowInCardLibrary)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([
            HoverTipFactory.FromCard<FinalJudgment>()
        ]);

    protected static CardSelectorPrefs GetSubmitSelectionPrefs()
    {
        return new(new MegaCrit.Sts2.Core.Localization.LocString("card_selection", "TO_SUBMIT"), 1);
    }

    protected async Task PerformSubmit(PlayerChoiceContext choiceContext, CardModel cardToSubmit)
    {
        if (Owner?.Creature == null)
            return;

        var cost = cardToSubmit.EnergyCost == null ? 0 : Math.Max(0, (int)cardToSubmit.EnergyCost.GetResolved());
        var points = 10 + cost * 10;

        await CardCmd.Exhaust(choiceContext, cardToSubmit);
        await PowerCmd.Apply<OratricePower>(choiceContext, Owner.Creature, points, Owner.Creature, this);

        var proceduralJustice = Owner.Creature.GetPower<ProceduralJusticePower>();
        if (proceduralJustice != null)
            await PowerCmd.Apply<OratricePower>(choiceContext, Owner.Creature, proceduralJustice.Amount, Owner.Creature, this);
    }
}