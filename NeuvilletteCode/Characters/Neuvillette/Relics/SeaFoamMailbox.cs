using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class SeaFoamMailbox : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<string> RegisteredKeywordIds => [NeuvilletteKeywords.MelusineSticker];


    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner)
        {
            return count;
        }
        return count + 2m;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        if (Owner.PlayerCombatState is not { TurnNumber: <= 1 })
        {
            return;
        }

        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count == 0)
        {
            return;
        }

        var prefs = new CardSelectorPrefs(
            new LocString("card_selection", "NEUVILLETTE-SEAFOAM_MAILBOX.selectionScreenPrompt"), 
            0, 
            999999999
        );

        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext, 
            Owner, 
            prefs, 
            null, 
            this
        );

        foreach (var card in selectedCards)
        {
            var combatState = Owner.Creature.CombatState;
            if (combatState == null)
            {
                continue;
            }

            var availableStickerCards = ModelDb.CardPool<MelusineCardPool>()
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(card => MelusineCardPool.GetAvailableCardsForCombat((CombatState)combatState).Any(available => available.GetType() == card.GetType()))
                .ToList();
            
            if (availableStickerCards.Count == 0)
            {
                continue;
            }

            var newCard = CardFactory.GetDistinctForCombat(Owner, availableStickerCards, 1, Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
            if (newCard == null)
            {
                continue;
            }

            await CardCmd.Transform(card, newCard);
        }
    }
}