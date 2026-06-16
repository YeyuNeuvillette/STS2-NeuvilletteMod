using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Neuvillette.Characters.Neuvillette.Powers;

[RegisterPower]
public sealed class SiphonPrimordialSeaPower : NeuvillettePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        var droplets = Owner.GetPower<SourcewaterDroplet>();
        if (droplets == null || droplets.Amount < Amount)
            return;

        Flash();
        await PowerCmd.ModifyAmount(choiceContext, droplets, -Amount, null, null);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, 2m, Owner, null);
    }
}