using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class StatueFragment : BaseRelic
{
    private const string _roundKey = "Round";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount
    {
        get
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                return 0;
            }
            if (base.IsCanonical)
            {
                return 0;
            }
            int roundNumber = base.DynamicVars["Round"].IntValue;
            if (roundNumber >= 3)
            {
                return 0;
            }
            return roundNumber;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>(new DynamicVar[1]
    {
        new DynamicVar("Round", 0m)
    });

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        base.DynamicVars["Round"].BaseValue++;
        InvokeDisplayAmountChanged();

        if (combatState.RoundNumber == 3)
        {
            Flash();
            var enemies = combatState.Enemies;
            foreach (var enemy in enemies)
            {
                var slowPower = enemy.GetPower<SlowPower>();
                if (slowPower == null)
                {
                    await PowerCmd.Apply<SlowPower>(new ThrowingPlayerChoiceContext(), enemy, 1m, Owner.Creature, null);
                }
                else
                {
                    await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), slowPower, 1m, Owner.Creature, null, false);
                }
            }
        }

        return;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        if (combatState.RoundNumber == 3)
        {
            var enemies = combatState.Enemies;
            foreach (var enemy in enemies)
            {
                if (enemy.Monster is MegaCrit.Sts2.Core.Models.Monsters.BygoneEffigy)
                {
                    continue;
                }

                var slowPower = enemy.GetPower<SlowPower>();
                if (slowPower != null)
                {
                    await PowerCmd.Remove(slowPower);
                }
            }
        }

        return;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        base.DynamicVars["Round"].BaseValue = 0m;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (!(room is CombatRoom))
        {
            return Task.CompletedTask;
        }
        base.DynamicVars["Round"].BaseValue = 0m;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}