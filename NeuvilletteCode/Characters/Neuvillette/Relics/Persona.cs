using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using Neuvillette.Characters.Base;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(NeuvilletteRelicPool))]
public sealed class Persona : BaseRelic
{
    private int _personaActIndex = -1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    [SavedProperty]
    public int PersonaActIndex
    {
        get => _personaActIndex;
        set
        {
            AssertMutable();
            _personaActIndex = value;
        }
    }

    [SavedProperty]
    private int MarkedEliteCol { get; set; } = -1;

    [SavedProperty]
    private int MarkedEliteRow { get; set; } = -1;

    [SavedProperty]
    private bool MarkedEliteCoordSet { get; set; }

    public override Task AfterObtained()
    {
        PersonaActIndex = Owner!.RunState.CurrentActIndex;
        return Task.CompletedTask;
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (PersonaActIndex < 0) return map;

        int targetActIndex = PersonaActIndex + 1;
        if (actIndex != targetActIndex) return map;

        if (MarkedEliteCoordSet)
        {
            var coord = new MapCoord(MarkedEliteCol, MarkedEliteRow);
            if (map.HasPoint(coord))
            {
                var point = map.GetPoint(coord);
                if (point != null && (point.PointType == MapPointType.Elite || point.PointType == MapPointType.Monster))
                {
                    point.AddQuest(this);
                    return map;
                }
            }

            MarkedEliteCoordSet = false;
        }

        var rng = new Rng(Owner!, Id);
        var candidates = map.GetAllMapPoints()
            .Where(p => p.PointType == MapPointType.Elite && !p.Quests.Any(q => q is Persona))
            .ToList();
        candidates.UnstableShuffle(rng);

        var chosen = candidates.FirstOrDefault();
        if (chosen == null) return map;

        MarkedEliteCol = chosen.coord.col;
        MarkedEliteRow = chosen.coord.row;
        MarkedEliteCoordSet = true;
        chosen.AddQuest(this);

        return map;
    }

    private MapCoord? GetMarkedEliteCoord()
    {
        if (!MarkedEliteCoordSet) return null;
        return new MapCoord(MarkedEliteCol, MarkedEliteRow);
    }

    private bool IsAtMarkedElite()
    {
        var coord = GetMarkedEliteCoord();
        if (coord == null) return false;
        return Owner?.RunState.CurrentMapPoint?.coord == coord;
    }

    private static readonly HashSet<uint> _buffedCreatures = new();

    public override async Task BeforeCombatStart()
    {
        if (!IsAtMarkedElite()) return;

        var combatState = Owner!.Creature.CombatState;
        if (combatState == null) return;

        var unbuffedEnemies = combatState.HittableEnemies
            .Where(e => e.CombatId.HasValue && _buffedCreatures.Add(e.CombatId.Value))
            .ToList();
        if (unbuffedEnemies.Count == 0) return;

        Flash();
        foreach (var enemy in unbuffedEnemies)
        {
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), enemy, 2m, null, null);
        }
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side != CombatSide.Enemy) return;
        if (!IsAtMarkedElite()) return;
        if (!creature.CombatId.HasValue || !_buffedCreatures.Add(creature.CombatId.Value)) return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), creature, 2m, null, null);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _buffedCreatures.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (!IsAtMarkedElite()) return Task.CompletedTask;
        if (room.RoomType != RoomType.Elite) return Task.CompletedTask;

        var soulRelic = ModelDb.Relic<Soul>().ToMutable();
        room.AddExtraReward(Owner!, new RelicReward(soulRelic, Owner!));

        return Task.CompletedTask;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;

        foreach (var opt in options)
        {
            if (opt.OptionId == "NEUVILLETTE_FORGE_SWORD" ||
                opt.OptionId == "NEUVILLETTE_MEDITATE")
                return false;
        }

        if (Owner.GetRelic<Memory>() == null)
        {
            options.Add(new MeditateRestSiteOption(player));
        }

        options.Add(new ForgeSwordRestSiteOption(player));
        return true;
    }
}