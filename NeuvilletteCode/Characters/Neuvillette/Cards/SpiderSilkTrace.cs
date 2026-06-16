using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class SpiderSilkTrace() : SubmitCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = Owner.PlayerCombatState?.Hand;
        if (hand == null)
            return;

        var statusAndCurseCards = hand.Cards
            .Where(c => c.Type == CardType.Status || c.Type == CardType.Curse)
            .ToList();

        if (statusAndCurseCards.Count == 0)
            return;

        foreach (var card in statusAndCurseCards)
            await PerformSubmit(choiceContext, card);

        await CardPileCmd.Draw(choiceContext, statusAndCurseCards.Count, Owner);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}