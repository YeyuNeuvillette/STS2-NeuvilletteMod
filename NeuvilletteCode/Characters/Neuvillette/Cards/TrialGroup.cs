using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class TrialGroup() : NeuvilletteCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.MelusineSticker];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([
            HoverTipFactory.FromCard<MinionStrike>(IsUpgraded),
            HoverTipFactory.FromCard<MinionDiveBomb>(IsUpgraded),
            HoverTipFactory.FromCard<MinionSacrifice>(IsUpgraded)
        ]);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var combatState = Owner.Creature.CombatState;
        if (combatState != null)
        {
            var availableStickerCards = ModelDb.CardPool<MelusineCardPool>()
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(card => MelusineCardPool.GetAvailableCardsForCombat((CombatState)combatState).Any(available => available.GetType() == card.GetType()))
                .ToList();

            foreach (var sticker in CardFactory.GetForCombat(Owner, availableStickerCards, 2, Owner.RunState.Rng.CombatCardGeneration).ToList())
                await CardPileCmd.AddGeneratedCardToCombat(sticker, PileType.Hand, Owner);
        }

        var minions = ModelDb.CardPool<TokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.GetType() == typeof(MinionStrike) || card.GetType() == typeof(MinionDiveBomb) || card.GetType() == typeof(MinionSacrifice))
            .ToList();

        var minion = CardFactory.GetForCombat(Owner, minions, 1, Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (minion != null)
            await CardPileCmd.AddGeneratedCardToCombat(minion, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}