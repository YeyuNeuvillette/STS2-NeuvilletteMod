using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class NarzissenkreuzSword : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        var owner = Owner;
        if (owner != null && participants.Contains(owner.Creature) && owner.PlayerCombatState is { TurnNumber: <= 1 })
        {
            Flash();
            await PlayerCmd.GainEnergy(1m, owner);
            await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, owner);
        }
    }

    public override async Task BeforeCombatStart()
    {
        var creature = Owner!.Creature;
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), creature, 1m, creature, null);
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), creature, 1m, creature, null);
    }
}