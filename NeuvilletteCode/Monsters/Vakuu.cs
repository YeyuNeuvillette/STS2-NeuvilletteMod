using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Neuvillette.Characters.Neuvillette.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Test.Scripts;

[RegisterMonster]
public class TestMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 444, 422);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 466, 444);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://Test/scenes/test_monster.tscn"
    );

    public override async Task AfterAddedToRoom()
    {
        await PowerCmd.Apply<TrueDemonPower>(new ThrowingPlayerChoiceContext(), Creature, 1m, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var wonderfulCard = new MoveState(
            "WONDERFUL_CARD",
            WonderfulCardMove,
            new DebuffIntent()
        );

        wonderfulCard.FollowUpState = wonderfulCard;

        return new MonsterMoveStateMachine([wonderfulCard], wonderfulCard);
    }

    private async Task WonderfulCardMove(IReadOnlyList<Creature> targets)
    {
        foreach (Creature target in targets)
        {
            Player player = target.Player ?? target.PetOwner;
            CardPile deck = PileType.Deck.GetPile(player);
            List<CardModel> powerCards = deck.Cards.Where((CardModel c) => c.Type == CardType.Power).ToList();
            
            if (powerCards.Count > 0)
            {
                CardModel card = base.RunRng.CombatCardGeneration.NextItem(powerCards);
                await CardPileCmd.RemoveFromDeck(card, showPreview: false);
                
                var cardPlay = new MegaCrit.Sts2.Core.Entities.Cards.CardPlay
                {
                    Card = card,
                    Target = Creature,
                    ResultPile = PileType.Exhaust,
                    Resources = new MegaCrit.Sts2.Core.Entities.Cards.ResourceInfo
                    {
                        EnergySpent = 0,
                        EnergyValue = 0,
                        StarsSpent = 0,
                        StarValue = 0
                    },
                    IsAutoPlay = true,
                    PlayIndex = 0,
                    PlayCount = 1
                };
                
                await card.OnPlayWrapper(new BlockingPlayerChoiceContext(), Creature, isAutoPlay: true, cardPlay.Resources);
            }
        }
    }
}