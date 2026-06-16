using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Cards;

[RegisterCard(typeof(MelusineCardPool))]
public sealed class CanotilaSticker() : MelusineStickerCard(TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawPile = PileType.Draw.GetPile(Owner).Cards.ToList();
        if (drawPile.Count > 0)
        {
            var cardsToSelect = Math.Min(2, drawPile.Count);
            var prefs = new CardSelectorPrefs(new LocString("card_selection", "TO_ADD_TO_HAND"), cardsToSelect);
            var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, drawPile, Owner, prefs);
            foreach (var card in selectedCards)
                await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, null, false);
        }

        var combatState = Owner.Creature.CombatState;
        if (combatState != null)
            MelusineCardPool.RemoveFromPoolInCombat((CombatState)combatState, typeof(CanotilaSticker));
    }
}