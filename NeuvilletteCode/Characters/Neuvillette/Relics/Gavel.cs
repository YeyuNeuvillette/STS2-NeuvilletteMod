using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Powers;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class Gavel : BaseRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromPower<ContemptOfCourtPower>()]);

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStartEarly(choiceContext, player);

        if (Owner?.Creature?.CombatState == null)
            return;

        if (player != Owner)
            return;

        var enemies = Owner.Creature.CombatState.Creatures
            .Where(c => c.Side == CombatSide.Enemy)
            .ToList();

        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<ContemptOfCourtPower>(new ThrowingPlayerChoiceContext(), enemy, 1m, Owner.Creature, null);
        }
    }
}