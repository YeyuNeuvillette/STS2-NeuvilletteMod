using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class StickerBook() : NeuvilletteCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.MelusineSticker];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar("DrawAmount", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState != null)
        {
            var availableStickerCards = ModelDb.CardPool<MelusineCardPool>()
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(card => MelusineCardPool.GetAvailableCardsForCombat((CombatState)combatState).Any(available => available.GetType() == card.GetType()))
                .ToList();

            var stickerCount = IsUpgraded ? 2 : 1;
            var stickers = CardFactory.GetForCombat(Owner, availableStickerCards, stickerCount, Owner.RunState.Rng.CombatCardGeneration);
            foreach (var sticker in stickers)
            {
                if (sticker != null)
                    await CardPileCmd.AddGeneratedCardToCombat(sticker, PileType.Hand, Owner);
            }
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars["DrawAmount"].BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars["DrawAmount"].UpgradeValueBy(1m);
    }
}