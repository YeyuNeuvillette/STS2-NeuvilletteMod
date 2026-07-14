using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace Neuvillette;

public sealed class NeuvilletteSettings
{
    public bool Act4Enabled { get; set; } = true;
    public bool MultiplayerCourtEnabled { get; set; }
}

internal static class NeuvilletteSettingsStore
{
    public const string SettingsKey = "settings";
    public const string SettingsFileName = "settings.json";
    private const string RunSavedSettingsKey = "settings_sync";

    private const string LocalizationPckFolder = $"{MainFile.ResPath}/localization";

    private static ModDataStoreCache<NeuvilletteSettings>? _cache;

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
        return _cache?.Value.Act4Enabled ?? true;
    }

    public static bool IsMultiplayerCourtEnabled()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState != null && RunSavedSettings.TryGet(runState, out var synced))
            return synced.MultiplayerCourtEnabled;

        return _cache?.Value.MultiplayerCourtEnabled ?? false;
    }

    public static void SyncLocalSettingsToRunState(RunState runState)
    {
        var data = new NeuvilletteSettings
        {
            Act4Enabled = _cache?.Value.Act4Enabled ?? true,
            MultiplayerCourtEnabled = _cache?.Value.MultiplayerCourtEnabled ?? false,
        };
        RunSavedSettings.Set(runState, data);
    }

    public static bool HasSyncedSettings(RunState runState)
    {
        return RunSavedSettings.TryGet(runState, out _);
    }
}