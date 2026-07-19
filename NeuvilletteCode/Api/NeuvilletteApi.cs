using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using Neuvillette.Characters.Neuvillette.Events;
using Neuvillette.Characters.Neuvillette.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;

namespace Neuvillette.Api;

public interface IEventOptionContributor
{
    bool AppliesTo(EventModel eventModel);
    IReadOnlyList<EventOption> ModifyOptions(EventModel eventModel, IReadOnlyList<EventOption> options);
}

public interface IStickerPoolContributor
{
    IEnumerable<CardModel> AddCandidates(CombatState combatState, IReadOnlyList<CardModel> currentCandidates);
    bool IsCandidateAvailable(CombatState combatState, CardModel candidate);
}

public interface IAct4Contributor
{
    void ConfigureMap(StandardActMap map) { }
    void ConfigureRooms(ActModel act, RoomSet rooms) { }
    bool TryHandleTerminalBossRewards(RewardsSet rewards, IRunState runState) => false;
    NCombatBackground? CreateCombatBackground(BackgroundAssets assets) => null;
}

public enum NeuvilletteMapMarkerKind
{
    FourQuadrantsLand,
    PersonaElite,
}

public readonly record struct NeuvilletteMapMarkerEvent(
    NeuvilletteMapMarkerKind Kind,
    IRunState RunState,
    int ActIndex);

public static class NeuvilletteApi
{
    public static event Action<NeuvilletteMapMarkerEvent>? MapMarkerCreated;
    public static event Action<NeuvilletteMapMarkerEvent>? MapMarkerEntered;
    public static event Action<NeuvilletteMapMarkerEvent>? MapMarkerCompleted;

    public static IDisposable RegisterEventOptionContributor(string ownerModId, IEventOptionContributor contributor) =>
        ApiRegistry.RegisterEventContributor(ownerModId, contributor);

    public static IDisposable RegisterStickerPoolContributor(string ownerModId, IStickerPoolContributor contributor) =>
        ApiRegistry.RegisterStickerContributor(ownerModId, contributor);

    public static IDisposable RegisterAct4Contributor(string ownerModId, IAct4Contributor contributor) =>
        ApiRegistry.RegisterAct4Contributor(ownerModId, contributor);

    public static bool IsAct4Enabled => NeuvilletteSettingsStore.IsAct4Enabled();

    public static bool IsInNeuvilletteAct(IRunState? runState) =>
        runState?.Act is Characters.Neuvillette.Act.NeuvilletteAct;

    public static bool HasInfiniteHealth(Creature? creature) =>
        creature != null && Characters.Neuvillette.Features.LeviathanHealthService.IsInfinite(creature);

    public static bool HasMapMarker(IRunState? runState, NeuvilletteMapMarkerKind kind)
    {
        if (runState == null)
            return false;

        return kind switch
        {
            NeuvilletteMapMarkerKind.FourQuadrantsLand => runState.Map.GetAllMapPoints()
                .Any(point => point.Quests.Any(quest => quest.Id == ModelDb.GetId<FourQuadrantsLand>())),
            NeuvilletteMapMarkerKind.PersonaElite => runState.Map.GetAllMapPoints()
                .Any(point => point.Quests.Any(quest => quest is Persona)),
            _ => false,
        };
    }

    internal static void PublishMarkerCreated(NeuvilletteMapMarkerEvent value) =>
        InvokeSafely(MapMarkerCreated, value, nameof(MapMarkerCreated));

    internal static void PublishMarkerCompleted(NeuvilletteMapMarkerEvent value) =>
        InvokeSafely(MapMarkerCompleted, value, nameof(MapMarkerCompleted));

    internal static void PublishMarkerEntered(NeuvilletteMapMarkerEvent value) =>
        InvokeSafely(MapMarkerEntered, value, nameof(MapMarkerEntered));

    private static void InvokeSafely(
        Action<NeuvilletteMapMarkerEvent>? handlers,
        NeuvilletteMapMarkerEvent value,
        string eventName)
    {
        if (handlers == null)
            return;

        foreach (Action<NeuvilletteMapMarkerEvent> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(value);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"API subscriber failed in {eventName}: {ex}");
            }
        }
    }
}
