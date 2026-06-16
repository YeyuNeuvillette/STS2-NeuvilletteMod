using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class OratricePower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power != this)
            return;

        while (Amount >= 100m)
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -100m, null, null);

            var player = Owner?.Player;
            if (player == null || CombatState == null)
                continue;

            var finalJudgment = CombatState.CreateCard<FinalJudgment>(player);
            await CardPileCmd.AddGeneratedCardsToCombat([finalJudgment], PileType.Hand, player);
        }
    }
}