using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class Breed() : NeuvilletteCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, shouldShowInCardLibrary: false)
{
    private const int BaseCards = 1;
    private const int BaseEnergy = 1;

    private int _currentCards = BaseCards;
    private int _currentEnergy = BaseEnergy;
    private int _increasedCards;
    private int _increasedEnergy;

    [SavedProperty]
    public int CurrentCards
    {
        get => _currentCards;
        set
        {
            AssertMutable();
            _currentCards = value;
            DynamicVars["Cards"].BaseValue = _currentCards;
        }
    }

    [SavedProperty]
    public int CurrentEnergy
    {
        get => _currentEnergy;
        set
        {
            AssertMutable();
            _currentEnergy = value;
            DynamicVars["Energy"].BaseValue = _currentEnergy;
        }
    }

    [SavedProperty]
    public int IncreasedCards
    {
        get => _increasedCards;
        set
        {
            AssertMutable();
            _increasedCards = value;
        }
    }

    [SavedProperty]
    public int IncreasedEnergy
    {
        get => _increasedEnergy;
        set
        {
            AssertMutable();
            _increasedEnergy = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Cards", CurrentCards),
        new DynamicVar("Energy", CurrentEnergy)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawPile = PileType.Draw.GetPile(Owner);
        var cardsToExhaust = Math.Min(DynamicVars["Cards"].IntValue, drawPile.Cards.Count);

        for (int i = 0; i < cardsToExhaust; i++)
        {
            var topCard = drawPile.Cards.FirstOrDefault();
            if (topCard == null)
                break;

            await CardCmd.Exhaust(choiceContext, topCard);
        }

        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);

        BuffFromPlay();
        (DeckVersion as Breed)?.BuffFromPlay();
    }

    protected override void AfterDowngraded()
    {
        UpdateValues();
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    private void BuffFromPlay()
    {
        IncreasedCards += 1;
        IncreasedEnergy += 1;
        UpdateValues();
    }

    private void UpdateValues()
    {
        CurrentCards = BaseCards + IncreasedCards;
        CurrentEnergy = BaseEnergy + IncreasedEnergy;
    }
}