using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Neuvillette.Characters.Neuvillette.Timeline;
using Neuvillette.Characters.Neuvillette.Patches;
using Neuvillette.Infrastructure;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace Neuvillette;

public sealed class NeuvilletteSettings
{
    public bool Act4Enabled { get; set; }
    public bool Act4Unlocked { get; set; }
    public bool MultiplayerCourtEnabled { get; set; }
    public bool SponsorRelicEnabled { get; set; } = true;
}

internal static class NeuvilletteSettingsStore
{
    public const string SettingsKey = "settings";
    public const string SettingsFileName = "settings.json";
    private const string RunSavedSettingsKey = "settings_sync";

    private const string LocalizationPckFolder = $"{MainFile.ResPath}/localization";

    private static ModDataStoreCache<NeuvilletteSettings>? _cache;
    private static bool _act4UnlockMigrationInProgress;
    private static bool _act4UnlockReadWarningLogged;

    private static readonly RunSavedData<NeuvilletteSettings> RunSavedSettings =
        RunSavedDataStore.For(MainFile.ModId).Register<NeuvilletteSettings>(
            key: RunSavedSettingsKey,
            defaultFactory: () => new NeuvilletteSettings(),
            options: new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.AlwaysWhenRegistered });

    public static I18N Localization { get; private set; } = null!;

    public static void Register()
    {
        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            var store = RitsuLibFramework.GetDataStore(MainFile.ModId);
            store.Register(
                key: SettingsKey,
                fileName: SettingsFileName,
                scope: SaveScope.Global,
                defaultFactory: () => new NeuvilletteSettings(),
                autoCreateIfMissing: true);
        }

        _cache = RitsuLibFramework.GetDataStore(MainFile.ModId)
            .CreateCache<NeuvilletteSettings>(SettingsKey);

        Localization = RitsuLibFramework.CreateLocalization(
            $"{MainFile.ModId}-Settings",
            pckFolders: [LocalizationPckFolder]);
    }

    public static bool IsAct4Enabled()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState != null && RunSavedSettings.TryGet(runState, out var synced))
            return synced.Act4Unlocked && synced.Act4Enabled;

        return IsAct4Unlocked() && (_cache?.Value.Act4Enabled ?? false);
    }

    public static bool IsAct4Unlocked()
    {
        if (_cache == null)
            return false;

        bool timelineUnlocked;
        try
        {
            // The revealed epoch is the canonical progression state.  The settings flag is
            // only a migration marker so an already affected profile can be repaired once.
            timelineUnlocked = SaveManager.Instance.IsEpochRevealed<Neuvillette7Epoch>();
        }
        catch (Exception ex)
        {
            // Settings predicates may be queried briefly while profile data is changing.
            // Preserve the last persisted result until the canonical state is readable again.
            if (!_act4UnlockReadWarningLogged)
            {
                MainFile.Logger.Warn($"Could not read Neuvillette Act 4 timeline unlock state: {ex.Message}");
                _act4UnlockReadWarningLogged = true;
            }

            return _cache.Value.Act4Unlocked;
        }

        _act4UnlockReadWarningLogged = false;
        if (!timelineUnlocked)
            return false;

        NeuvilletteSettings settings = _cache.Value;
        if (!settings.Act4Unlocked && !_act4UnlockMigrationInProgress)
        {
            _act4UnlockMigrationInProgress = true;
            try
            {
                _cache.Modify(value =>
                {
                    if (value.Act4Unlocked)
                        return;

                    value.Act4Unlocked = true;
                    value.Act4Enabled = true;
                });
                _cache.Save();
                MainFile.Logger.Info("Migrated revealed Neuvillette chapter 7 to the Act 4 setting; enabled Act 4 by default.");
            }
            finally
            {
                _act4UnlockMigrationInProgress = false;
            }
        }

        return true;
    }

    public static bool IsMultiplayerCourtEnabled()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState != null && RunSavedSettings.TryGet(runState, out var synced))
            return synced.MultiplayerCourtEnabled;

        return _cache?.Value.MultiplayerCourtEnabled ?? false;
    }

    public static bool IsSponsorRelicEnabled()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState != null && RunSavedSettings.TryGet(runState, out var synced))
            return synced.SponsorRelicEnabled;

        return _cache?.Value.SponsorRelicEnabled ?? true;
    }

    public static void SyncLocalSettingsToRunState(RunState runState)
    {
        var data = new NeuvilletteSettings
        {
            Act4Enabled = IsAct4Enabled(),
            Act4Unlocked = IsAct4Unlocked(),
            MultiplayerCourtEnabled = _cache?.Value.MultiplayerCourtEnabled ?? false,
            SponsorRelicEnabled = _cache?.Value.SponsorRelicEnabled ?? true,
        };
        RunSavedSettings.Set(runState, data);
    }

    public static RunState? TrySetActiveRunAct4Enabled(bool enabled)
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || !GameCompatibility.IsRunAuthority())
            return null;

        bool multiplayerCourtEnabled = RunSavedSettings.TryGet(runState, out var synced)
            ? synced.MultiplayerCourtEnabled
            : _cache?.Value.MultiplayerCourtEnabled ?? false;
        bool sponsorRelicEnabled = RunSavedSettings.TryGet(runState, out var synced2)
            ? synced2.SponsorRelicEnabled
            : _cache?.Value.SponsorRelicEnabled ?? true;
        RunSavedSettings.Set(runState, new NeuvilletteSettings
        {
            Act4Enabled = enabled,
            Act4Unlocked = IsAct4Unlocked(),
            MultiplayerCourtEnabled = multiplayerCourtEnabled,
            SponsorRelicEnabled = sponsorRelicEnabled,
        });
        return runState;
    }

    /// <summary>
    /// Called by the final character epoch. The first reveal intentionally opts the
    /// player in; subsequent changes remain under the settings toggle's control.
    /// </summary>
    public static void UnlockAct4()
    {
        if (_cache == null)
            return;

        _cache.Modify(settings =>
        {
            settings.Act4Unlocked = true;
            settings.Act4Enabled = true;
        });
        _cache.Save();

        RunState? runState = TrySetActiveRunAct4Enabled(true);
        if (runState != null)
            FourQuadrantsLandPatch.EnsureMarked(runState);
    }

    public static bool HasSyncedSettings(RunState runState)
    {
        return RunSavedSettings.TryGet(runState, out _);
    }
}
