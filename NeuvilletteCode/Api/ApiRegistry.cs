using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace Neuvillette.Api;

internal static class ApiRegistry
{
    private sealed record Registration<T>(string OwnerModId, T Contributor);

    private static readonly object Sync = new();
    private static readonly List<Registration<IEventOptionContributor>> EventContributors = [];
    private static readonly List<Registration<IStickerPoolContributor>> StickerContributors = [];
    private static readonly List<Registration<IAct4Contributor>> Act4Contributors = [];

    internal static IDisposable RegisterEventContributor(string ownerModId, IEventOptionContributor contributor) =>
        Register(EventContributors, ownerModId, contributor);

    internal static IDisposable RegisterStickerContributor(string ownerModId, IStickerPoolContributor contributor) =>
        Register(StickerContributors, ownerModId, contributor);

    internal static IDisposable RegisterAct4Contributor(string ownerModId, IAct4Contributor contributor) =>
        Register(Act4Contributors, ownerModId, contributor);

    internal static void ConfigureAct4Map(StandardActMap map)
    {
        foreach (var registration in Snapshot(Act4Contributors))
        {
            try
            {
                registration.Contributor.ConfigureMap(map);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Act 4 contributor '{registration.OwnerModId}' failed while configuring the map: {ex}");
            }
        }
    }

    internal static void ConfigureAct4Rooms(ActModel act, RoomSet rooms)
    {
        foreach (var registration in Snapshot(Act4Contributors))
        {
            try
            {
                registration.Contributor.ConfigureRooms(act, rooms);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Act 4 contributor '{registration.OwnerModId}' failed while configuring rooms: {ex}");
            }
        }
    }

    internal static bool TryHandleAct4Rewards(RewardsSet rewards, IRunState runState)
    {
        foreach (var registration in Snapshot(Act4Contributors))
        {
            try
            {
                if (registration.Contributor.TryHandleTerminalBossRewards(rewards, runState))
                    return true;
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Act 4 contributor '{registration.OwnerModId}' failed while handling rewards: {ex}");
            }
        }

        return false;
    }

    internal static NCombatBackground? CreateAct4Background(BackgroundAssets assets)
    {
        foreach (var registration in Snapshot(Act4Contributors))
        {
            try
            {
                var result = registration.Contributor.CreateCombatBackground(assets);
                if (result != null)
                    return result;
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Act 4 contributor '{registration.OwnerModId}' failed while creating a background: {ex}");
            }
        }

        return null;
    }

    internal static IReadOnlyList<EventOption> ApplyEventContributors(
        EventModel eventModel,
        IReadOnlyList<EventOption> options)
    {
        IReadOnlyList<EventOption> current = options;
        foreach (var registration in Snapshot(EventContributors))
        {
            try
            {
                if (registration.Contributor.AppliesTo(eventModel))
                    current = registration.Contributor.ModifyOptions(eventModel, current)?.ToArray() ?? current;
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Event contributor '{registration.OwnerModId}' failed: {ex}");
            }
        }

        return current;
    }

    internal static IReadOnlyList<CardModel> ApplyStickerContributors(
        CombatState combatState,
        IReadOnlyList<CardModel> candidates)
    {
        IReadOnlyList<CardModel> current = candidates;
        foreach (var registration in Snapshot(StickerContributors))
        {
            try
            {
                current = registration.Contributor.AddCandidates(combatState, current)?.ToArray() ?? current;
                current = current.Where(card => registration.Contributor.IsCandidateAvailable(combatState, card)).ToArray();
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Sticker contributor '{registration.OwnerModId}' failed: {ex}");
            }
        }

        return current;
    }

    private static IDisposable Register<T>(List<Registration<T>> registrations, string ownerModId, T contributor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerModId);
        ArgumentNullException.ThrowIfNull(contributor);

        var registration = new Registration<T>(ownerModId, contributor);
        lock (Sync)
            registrations.Add(registration);
        return new RegistrationHandle(() =>
        {
            lock (Sync)
                registrations.Remove(registration);
        });
    }

    private static Registration<T>[] Snapshot<T>(List<Registration<T>> registrations)
    {
        lock (Sync)
            return registrations.ToArray();
    }

    private sealed class RegistrationHandle(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
