using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class SurgeNextTurnPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || AmountOnTurnStart == 0m)
            return;

        Flash();

        var surgeAmount = AmountOnTurnStart + Owner.GetPowerAmount<LivingWaterPower>();
        await CreatureCmd.Heal(Owner, surgeAmount);
        await PowerCmd.Apply<SurgePower>(choiceContext, Owner, surgeAmount, Owner, null);
        await PowerCmd.Remove(this);
    }
}