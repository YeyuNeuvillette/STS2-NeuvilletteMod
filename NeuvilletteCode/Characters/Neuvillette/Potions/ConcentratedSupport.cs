using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Characters.Neuvillette.Potions;

[RegisterPotion(typeof(NeuvillettePotionPool))]
public sealed class ConcentratedSupport : BasePotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StickerAmount", 0m)
    ];

    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        NeuvilletteKeywords.MelusineSticker
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("4fc3f7"));
        
        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
            return;

        var availableStickerCards = ModelDb.CardPool<MelusineCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => MelusineCardPool.GetAvailableCardsForCombat((CombatState)combatState).Any(available => available.GetType() == card.GetType()))
            .ToList();

        if (availableStickerCards.Count == 0)
            return;

        var currentHandSize = PileType.Hand.GetPile(Owner).Cards.Count;
        var maxHandSize = 10;
        var neededStickers = maxHandSize - currentHandSize;

        if (neededStickers <= 0)
            return;

        var stickerCount = Math.Min(neededStickers, availableStickerCards.Count);
        var stickers = CardFactory.GetForCombat(Owner, availableStickerCards, stickerCount, Owner.RunState.Rng.CombatCardGeneration).ToList();

        foreach (var sticker in stickers)
        {
            await CardPileCmd.AddGeneratedCardToCombat(sticker, PileType.Hand, Owner);
        }
    }
}