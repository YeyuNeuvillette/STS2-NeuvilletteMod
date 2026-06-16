using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Neuvillette.Characters.Base;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class SoulGraftRelic : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner || combatState.RoundNumber != 1)
        {
            return;
        }

        Flash();
        
        IEnumerable<CardModel> attackCards = base.Owner.Character.CardPool.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Type == CardType.Attack && c.CanBeGeneratedInCombat);
        
        List<CardModel> cards = CardFactory.GetDistinctForCombat(base.Owner, attackCards, base.DynamicVars.Cards.IntValue, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
        
        foreach (CardModel card in cards)
        {
            card.SetToFreeThisCombat();
        }
        
        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, base.Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(results, 2f);
        await Cmd.Wait(0.75f);
    }
}