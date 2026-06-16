using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class TidesPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card is not EquitableJudgment)
            return;

        Flash();
        var surgeAmount = Amount + Owner.GetPowerAmount<LivingWaterPower>();
        await CreatureCmd.Heal(Owner, surgeAmount);
        await PowerCmd.Apply<SurgePower>(context, Owner, surgeAmount, Owner, null);
    }
}