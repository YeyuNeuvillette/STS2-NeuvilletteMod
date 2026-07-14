using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class SleepingShell : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new PowerVar<PlatingPower>(99m) };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(HoverTipFactory.FromPowerWithPowerHoverTips<PlatingPower>());

    public override async Task BeforeCombatStart()
    {
        await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 99m, Owner.Creature, null);
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner.Creature && dealer?.PetOwner != Owner) return;
        if (result.UnblockedDamage <= 0) return;
        var plating = Owner.Creature.GetPower<PlatingPower>();
        if (plating != null)
        {
            Flash();
            await PowerCmd.Remove<PlatingPower>(Owner.Creature);
        }
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature) return;
        if (result.UnblockedDamage <= 0) return;
        var plating = Owner.Creature.GetPower<PlatingPower>();
        if (plating != null)
        {
            Flash();
            await PowerCmd.Remove<PlatingPower>(Owner.Creature);
        }
    }
}