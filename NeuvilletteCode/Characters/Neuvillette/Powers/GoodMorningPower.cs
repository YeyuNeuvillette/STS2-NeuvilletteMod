using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class GoodMorningPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner.Player)
            return;

        var availableStickerCards = MelusineCardPool.GetAvailableCardsForCombat((CombatState)combatState).ToList();
        if (availableStickerCards.Count == 0)
            return;

        Flash();
        foreach (var sticker in CardFactory.GetForCombat(player, availableStickerCards, (int)Amount, player.RunState.Rng.CombatCardGeneration).ToList())
            await CardPileCmd.AddGeneratedCardToCombat(sticker, PileType.Hand, player);
    }
}