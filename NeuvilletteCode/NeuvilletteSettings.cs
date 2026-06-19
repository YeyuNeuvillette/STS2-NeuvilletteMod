using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace Neuvillette;

public sealed class NeuvilletteSettings
{
    public bool Act4Enabled { get; set; } = true;
}

internal static class NeuvilletteSettingsStore
{
    public const string SettingsKey = "settings";
    public const string SettingsFileName = "settings.json";

    private const string LocalizationPckFolder = $"{MainFile.ResPath}/localization";

    private static ModDataStoreCache<NeuvilletteSettings>? _cache;

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
}