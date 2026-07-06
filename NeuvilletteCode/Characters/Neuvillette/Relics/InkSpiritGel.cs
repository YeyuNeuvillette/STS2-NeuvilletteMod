using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
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
public sealed class InkSpiritGel : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DynamicVar("DamageThreshold", 80m), new PowerVar<SlipperyPower>(1m) };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(HoverTipFactory.FromPowerWithPowerHoverTips<SlipperyPower>());

    public override bool ShowCounter => true;

    public override int DisplayAmount => _damageThisTurn;

    private int _damageThisTurn;
    private bool _triggeredThisTurn;

    public override Task BeforeCombatStart()
    {
        _damageThisTurn = 0;
        _triggeredThisTurn = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner.Creature && dealer?.PetOwner != Owner)
            return;

        if (target.IsPlayer)
            return;

        if (result.UnblockedDamage <= 0)
            return;

        _damageThisTurn += (int)result.UnblockedDamage;
        InvokeDisplayAmountChanged();

        if (_damageThisTurn >= 80 && !_triggeredThisTurn)
        {
            _triggeredThisTurn = true;
            Flash();
            await PowerCmd.Apply<SlipperyPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side)
            return;

        _damageThisTurn = 0;
        _triggeredThisTurn = false;
        InvokeDisplayAmountChanged();
    }
}