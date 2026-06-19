using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace Neuvillette;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Neuvillette";
    public const string ResPath = $"res://{ModId}";

    private const string NeuvilletteFmodBankPath = "res://Neuvillette/audios/Neuvillette.bank";
    private const string NeuvilletteFmodGuidsPath = "res://Neuvillette/audios/GUIDs.txt";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    private static IDisposable? _fmodBankDeferredInitSubscription;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        NeuvilletteSettingsStore.Register();
        RegisterSettingsPage();

        QueueNeuvilletteFmodAfterDeferredInitialization();

        Logger.Info("Neuvillette mod initialized successfully");
    }

    private static void RegisterSettingsPage()
    {
        var i18n = NeuvilletteSettingsStore.Localization;
        RitsuLibFramework.RegisterModSettings(
            ModId,
            page => page
                .WithTitle(ModSettingsText.I18N(i18n, "neuvillette.settings.page.title", "Neuvillette"))
                .WithModDisplayName(ModSettingsText.I18N(i18n, "neuvillette.settings.page.title", "Neuvillette"))
                .AddSection("act4", section => section
                    .WithTitle(ModSettingsText.I18N(i18n, "neuvillette.settings.section.act4.title", "Act 4"))
                    .AddToggle(
                        "act4_enabled",
                        ModSettingsText.I18N(i18n, "neuvillette.settings.act4.enabled.label", "Enable Act 4"),
                        new ModSettingsValueBinding<NeuvilletteSettings, bool>(
                            ModId,
                            NeuvilletteSettingsStore.SettingsKey,
                            SaveScope.Global,
                            s => s.Act4Enabled,
                            (s, value) => s.Act4Enabled = value),
                        ModSettingsText.I18N(i18n, "neuvillette.settings.act4.enabled.description",
                            "When enabled, proceed to Act 4 (All-Devouring Narwhal boss fight) after Act 3. When disabled, the run ends after Act 3 as in vanilla."))));
    }

    /// <summary>
    ///     FMOD <c>FmodServer</c> is not guaranteed to exist during <see cref="ModInitializerAttribute" /> entry; loading
    ///     banks there fails silently. Align with other mods: load after <see cref="DeferredInitializationCompletedEvent" />.
    /// </summary>
    private static void QueueNeuvilletteFmodAfterDeferredInitialization()
    {
        if (_fmodBankDeferredInitSubscription != null)
            return;

        _fmodBankDeferredInitSubscription =
            RitsuLibFramework.SubscribeLifecycle<DeferredInitializationCompletedEvent>(_ =>
            {
                try
                {
                    if (FmodStudioServer.TryGet() is null)
                    {
                        Logger.Warn("FmodServer singleton missing; skipped Neuvillette FMOD bank load.");
                        return;
                    }

                    if (!FmodStudioServer.TryLoadBank(NeuvilletteFmodBankPath))
                    {
                        Logger.Warn($"Failed to load FMOD bank: {NeuvilletteFmodBankPath}");
                        return;
                    }

                    FmodStudioServer.TryWaitForAllLoads();

                    if (!FmodStudioServer.TryLoadStudioGuidMappings(NeuvilletteFmodGuidsPath))
                        Logger.Warn($"Failed to load FMOD guid map: {NeuvilletteFmodGuidsPath}");
                }
                finally
                {
                    _fmodBankDeferredInitSubscription?.Dispose();
                    _fmodBankDeferredInitSubscription = null;
                }
            });
    }
}