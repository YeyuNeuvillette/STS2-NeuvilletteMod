using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(NeuvilletteCardPool))]
public sealed class Cherish() : NeuvilletteCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    [Obsolete("Use CardModel.CanonicalKeywords with CardKeyword values instead.")]
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.MelusineSticker];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.Static(StaticHoverTip.Transform)]);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(24m, ValueProp.Move),
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var selectedCards = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(
                    new LocString("card_selection", "NEUVILLETTE-CHERISH.selectionScreenPrompt"),
                    DynamicVars.Cards.IntValue),
                card => card != this && card.IsTransformable,
                this))
            .ToList();
        if (selectedCards.Count == 0 || Owner.Creature.CombatState is not CombatState combatState)
            return;

        var availableStickerTypes = MelusineCardPool.GetAvailableCardsForCombat(combatState)
            .Select(static card => card.GetType())
            .ToHashSet();
        var availableStickerCards = ModelDb.CardPool<MelusineCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => availableStickerTypes.Contains(card.GetType()))
            .ToArray();
        if (availableStickerCards.Length == 0)
            return;

        var replacements = CardFactory.GetForCombat(
                Owner,
                availableStickerCards,
                selectedCards.Count,
                Owner.RunState.Rng.CombatCardGeneration)
            .ToArray();
        var transformations = selectedCards
            .Zip(replacements, static (original, replacement) => new CardTransformation(original, replacement));
        await CardCmd.Transform(transformations, null);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
    }
}
