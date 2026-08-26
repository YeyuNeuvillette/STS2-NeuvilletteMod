using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Neuvillette.Cards;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class TidesPower : NeuvillettePower
{
    private const string TriggerCountKey = "TriggerCount";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => DynamicVars[TriggerCountKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(TriggerCountKey, 0m)
    ];

    public void AddTriggerCount(int amount)
    {
        if (amount <= 0)
            return;

        DynamicVars[TriggerCountKey].BaseValue += amount;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card is not EquitableJudgment)
            return;

        Flash();
        var surgeAmount = Amount + Owner.GetPowerAmount<LivingWaterPower>();
        for (var i = 0; i < DynamicVars[TriggerCountKey].IntValue; i++)
        {
            await CreatureCmd.Heal(Owner, surgeAmount);
            await PowerCmd.Apply<SurgePower>(context, Owner, surgeAmount, Owner, null);
        }
    }
}
