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
using MegaCrit.Sts2.Core.Models.RelicPools;
using Neuvillette.Api;
using Neuvillette.Features.Map;

namespace Neuvillette.Characters.Neuvillette.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class Persona : BaseRelic
{
    private enum EliteEnhancement
    {
        None,
        Strength,
        Artifact,
        Plating,
    }

    private int _personaActIndex = -1;
    private int _markedEliteActIndex = -1;
    private int _markedEliteColumn = -1;
    private int _markedEliteRow = -1;

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
    public bool MarkedEliteCompleted { get; private set; }

    [SavedProperty]
    public int MarkedEliteActIndex
    {
        get => _markedEliteActIndex;
        private set
        {
            AssertMutable();
            _markedEliteActIndex = value;
        }
    }

    [SavedProperty]
    public int MarkedEliteColumn
    {
        get => _markedEliteColumn;
        private set
        {
            AssertMutable();
            _markedEliteColumn = value;
        }
    }

    [SavedProperty]
    public int MarkedEliteRow
    {
        get => _markedEliteRow;
        private set
        {
            AssertMutable();
            _markedEliteRow = value;
        }
    }

    internal bool TryGetMarkedEliteCoord(int actIndex, out MapCoord coord)
    {
        if (MarkedEliteActIndex == actIndex
            && MarkedEliteColumn >= 0
            && MarkedEliteRow >= 0)
        {
            coord = new MapCoord(MarkedEliteColumn, MarkedEliteRow);
            return true;
        }

        coord = default;
        return false;
    }

    internal void SetMarkedEliteCoord(int actIndex, MapCoord coord)
    {
        MarkedEliteActIndex = actIndex;
        MarkedEliteColumn = coord.col;
        MarkedEliteRow = coord.row;
    }

    internal void ClearMarkedEliteCoord()
    {
        MarkedEliteActIndex = -1;
        MarkedEliteColumn = -1;
        MarkedEliteRow = -1;
    }

    public override Task AfterObtained()
    {
        PersonaActIndex = Owner!.RunState.CurrentActIndex;
        MarkedEliteCompleted = false;
        if (PersonaEliteMarkerService.IsCompleted(Owner.RunState))
        {
            MarkedEliteCompleted = true;
            return Task.CompletedTask;
        }
        TryCreateMarker(
            Owner.RunState,
            Owner.RunState.Map,
            PersonaActIndex,
            Owner.RunState.CurrentMapCoord?.row ?? -1,
            Owner.RunState.CurrentMapPoint);
        return Task.CompletedTask;
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (!NeuvilletteSettingsStore.IsAct4Enabled())
        {
            PersonaEliteMarkerService.RemoveAll(runState, map);
            return map;
        }
        if (PersonaEliteMarkerService.IsCompleted(runState))
        {
            MarkedEliteCompleted = true;
            return map;
        }
        if (PersonaActIndex < 0
            || actIndex < PersonaActIndex
            || actIndex >= PersonaEliteMarkerService.StandardActCount)
            return map;

        if (PersonaEliteMarkerService.Normalize(runState, map) != null)
            return map;

        int minimumRow = actIndex == PersonaActIndex
            ? runState.CurrentMapCoord?.row ?? -1
            : -1;
        MapPoint? routeOrigin = actIndex == PersonaActIndex
            && runState.CurrentActIndex == actIndex
            && runState.CurrentMapCoord is { } currentCoord
                ? map.GetPoint(currentCoord)
                : null;
        TryCreateMarker(runState, map, actIndex, minimumRow, routeOrigin);
        return map;
    }

    private void TryCreateMarker(
        IRunState runState,
        ActMap map,
        int actIndex,
        int minimumRow,
        MapPoint? routeOrigin)
    {
        if (!PersonaEliteMarkerService.EnsureMarked(
                this, runState, map, actIndex, minimumRow, routeOrigin))
            return;
        if (runState is RunState state)
        {
            NeuvilletteApi.PublishMarkerCreated(new(
                NeuvilletteMapMarkerKind.PersonaElite,
                state,
                actIndex));
        }
    }

    private bool IsAtMarkedElite()
    {
        return PersonaEliteMarkerService.GetCurrentMarker(Owner?.RunState) != null;
    }

    private bool OwnsCurrentMarker()
    {
        return ReferenceEquals(
            PersonaEliteMarkerService.GetCurrentMarker(Owner?.RunState),
            this);
    }

    // AbstractModel.MutableClone() starts with a shallow MemberwiseClone(). Keep this
    // collection instance-local, otherwise a Persona from a previous SL session can
    // leave repeated CombatIds in the canonical/shared HashSet and make the rebuilt
    // combat incorrectly look as if its enemies were already enhanced.
    private HashSet<uint> _buffedCreatures = [];
    private ICombatState? _trackedCombatState;
    private EliteEnhancement _activeEliteEnhancement;
    private bool _markerEnteredPublished;

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _buffedCreatures = [];
        _trackedCombatState = null;
        _activeEliteEnhancement = EliteEnhancement.None;
        _markerEnteredPublished = false;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        var owner = Owner;
        if (owner != null && participants.Contains(owner.Creature) && owner.PlayerCombatState is { TurnNumber: <= 1 })
        {
            Flash();
            await PlayerCmd.GainEnergy(1m, owner);
        }
    }

    public override async Task BeforeCombatStart()
    {
        var combatState = Owner?.Creature.CombatState;
        if (combatState == null) return;

        TrackCombat(combatState);
        if (!OwnsCurrentMarker()) return;

        if (!_markerEnteredPublished)
        {
            _markerEnteredPublished = true;
            NeuvilletteApi.PublishMarkerEntered(new(
                NeuvilletteMapMarkerKind.PersonaElite,
                Owner!.RunState,
                Owner.RunState.CurrentActIndex));
        }

        var enemies = combatState.HittableEnemies.ToList();
        int enemyCount = enemies.Count;
        var unbuffedEnemies = enemies
            .Where(e => e.CombatId.HasValue && _buffedCreatures.Add(e.CombatId.Value))
            .ToList();
        if (unbuffedEnemies.Count == 0) return;

        EnsureEliteEnhancementSelected();
        Flash();
        foreach (var enemy in unbuffedEnemies)
            await ApplyEliteEnhancement(enemy, enemyCount);
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side != CombatSide.Enemy) return;
        if (creature.CombatState is not { } combatState) return;

        TrackCombat(combatState);
        if (!OwnsCurrentMarker()) return;
        if (!creature.CombatId.HasValue || !_buffedCreatures.Add(creature.CombatId.Value)) return;

        EnsureEliteEnhancementSelected();
        Flash();
        await ApplyEliteEnhancement(creature, combatState.HittableEnemies.Count);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _buffedCreatures.Clear();
        _trackedCombatState = null;
        _activeEliteEnhancement = EliteEnhancement.None;
        _markerEnteredPublished = false;
        return Task.CompletedTask;
    }

    private void TrackCombat(ICombatState combatState)
    {
        if (ReferenceEquals(_trackedCombatState, combatState))
            return;

        _trackedCombatState = combatState;
        _buffedCreatures.Clear();
        _activeEliteEnhancement = EliteEnhancement.None;
        _markerEnteredPublished = false;
    }

    private void EnsureEliteEnhancementSelected()
    {
        if (_activeEliteEnhancement != EliteEnhancement.None)
            return;

        var coord = Owner?.RunState.CurrentMapCoord;
        string rngId = coord.HasValue
            ? $"PersonaEliteEnhancement:{Owner!.RunState.CurrentActIndex}:{coord.Value.col}:{coord.Value.row}"
            : $"PersonaEliteEnhancement:{Owner!.RunState.CurrentActIndex}";
        var rng = new Rng(Owner!.RunState.Rng.Seed, rngId);
        _activeEliteEnhancement = rng.NextInt(3) switch
        {
            0 => EliteEnhancement.Strength,
            1 => EliteEnhancement.Artifact,
            _ => EliteEnhancement.Plating,
        };
    }

    internal static decimal ScaleEliteEnhancementAmount(decimal amount, int enemyCount)
    {
        return enemyCount > 1 ? decimal.Ceiling(amount / 2m) : amount;
    }

    private Task ApplyEliteEnhancement(Creature creature, int enemyCount)
    {
        var context = new ThrowingPlayerChoiceContext();
        return _activeEliteEnhancement switch
        {
            EliteEnhancement.Strength => PowerCmd.Apply<StrengthPower>(
                context, creature, ScaleEliteEnhancementAmount(2m, enemyCount), null, null),
            EliteEnhancement.Artifact => PowerCmd.Apply<ArtifactPower>(
                context, creature, ScaleEliteEnhancementAmount(2m, enemyCount), null, null),
            EliteEnhancement.Plating => PowerCmd.Apply<PlatingPower>(
                context, creature, ScaleEliteEnhancementAmount(7m, enemyCount), null, null),
            _ => Task.CompletedTask,
        };
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (!IsAtMarkedElite()) return Task.CompletedTask;
        if (room.RoomType != RoomType.Elite) return Task.CompletedTask;

        bool ownsMarker = OwnsCurrentMarker();
        foreach (Player player in Owner!.RunState.Players)
        {
            Persona? persona = player.GetRelic<Persona>();
            if (persona != null)
                persona.MarkedEliteCompleted = true;
        }
        PersonaEliteMarkerService.MarkCompleted(Owner.RunState);
        if (ownsMarker)
        {
            NeuvilletteApi.PublishMarkerCompleted(new(
                NeuvilletteMapMarkerKind.PersonaElite,
                Owner.RunState,
                Owner.RunState.CurrentActIndex));
        }
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
