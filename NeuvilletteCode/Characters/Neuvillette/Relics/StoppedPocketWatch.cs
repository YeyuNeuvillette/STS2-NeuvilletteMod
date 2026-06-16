using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using Neuvillette.Characters.Neuvillette.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class StoppedPocketWatch : BaseRelic
{
    private bool _hasTriggered;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override Task BeforeCombatStart()
    {
        _hasTriggered = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStartEarly(choiceContext, player);

        if (player != Owner)
            return;

        if (_hasTriggered)
            return;

        var creature = Owner?.Creature;
        if (creature == null)
            return;

        if (creature.CombatState?.RoundNumber != 4)
            return;

        _hasTriggered = true;
        Flash();

        await CardPileCmd.Draw(choiceContext, 4m, player);

        var livingWaterAmount = creature.GetPowerAmount<LivingWaterPower>();
        var totalSurge = 4m + livingWaterAmount;

        await CreatureCmd.Heal(creature, totalSurge);
        await PowerCmd.Apply<SurgePower>(new ThrowingPlayerChoiceContext(), creature, totalSurge, creature, null);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _hasTriggered = false;
        return Task.CompletedTask;
    }
}