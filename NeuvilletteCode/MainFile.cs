using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using Neuvillette.Characters.Neuvillette.Patches;
using Neuvillette.Infrastructure;
using Neuvillette.Telemetry;
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
        GameCompatibility.Validate();

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        NeuvilletteSettingsStore.Register();
        RegisterSettingsPage();
        NeuvilletteTelemetry.Register();

        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted);
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(OnRunLoaded);

        QueueNeuvilletteFmodAfterDeferredInitialization();

        Logger.Info("Neuvillette mod initialized successfully");
    }

    private static void OnRunStarted(RunStartedEvent e)
    {
        if (GameCompatibility.IsRunAuthority())
            NeuvilletteSettingsStore.SyncLocalSettingsToRunState(e.RunState);
    }

    private static void OnRunLoaded(RunLoadedEvent e)
    {
        if (GameCompatibility.IsRunAuthority())
            NeuvilletteSettingsStore.SyncLocalSettingsToRunState(e.RunState);

        if (GameCompatibility.IsRunAuthority() && e.RunState is RunState runState)
            FourQuadrantsLandPatch.EnsureMarked(runState);
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
                    .WithVisibleWhen(NeuvilletteSettingsStore.IsAct4Unlocked)
                    .WithTitle(ModSettingsText.I18N(i18n, "neuvillette.settings.section.act4.title", "Act 4"))
                    .AddToggle(
                        "act4_enabled",
                        ModSettingsText.I18N(i18n, "neuvillette.settings.act4.enabled.label", "Enable Act 4"),
                        new ModSettingsValueBinding<NeuvilletteSettings, bool>(
                            ModId,
                            NeuvilletteSettingsStore.SettingsKey,
                            SaveScope.Global,
                            s => s.Act4Enabled,
                            SetAct4Enabled),
                        ModSettingsText.I18N(i18n, "neuvillette.settings.act4.enabled.description",
                            "When enabled, proceed to Act 4 after Act 3. When disabled, the run ends after Act 3 as in vanilla.")))
                .AddSection("multiplayer_court", section => section
                    .WithTitle(ModSettingsText.I18N(i18n, "neuvillette.settings.section.multiplayer_court.title", "Multiplayer Submit"))
                    .AddToggle(
                        "multiplayer_court_enabled",
                        ModSettingsText.I18N(i18n, "neuvillette.settings.multiplayer_court.enabled.label", "Enable Submit Cards in Multiplayer"),
                        new ModSettingsValueBinding<NeuvilletteSettings, bool>(
                            ModId,
                            NeuvilletteSettingsStore.SettingsKey,
                            SaveScope.Global,
                            s => s.MultiplayerCourtEnabled,
                            (s, value) => s.MultiplayerCourtEnabled = value),
                        ModSettingsText.I18N(i18n, "neuvillette.settings.multiplayer_court.enabled.description",
                            "When enabled, Submit-related cards can appear in multiplayer. When disabled (default), these cards are excluded from multiplayer.")))
                .AddSection("sponsor_relic", section => section
                    .WithTitle(ModSettingsText.I18N(i18n, "neuvillette.settings.section.sponsor_relic.title", "Sponsor Relic"))
                    .AddToggle(
                        "sponsor_relic_enabled",
                        ModSettingsText.I18N(i18n, "neuvillette.settings.sponsor_relic.enabled.label", "Enable Sponsor Relic"),
                        new ModSettingsValueBinding<NeuvilletteSettings, bool>(
                            ModId,
                            NeuvilletteSettingsStore.SettingsKey,
                            SaveScope.Global,
                            s => s.SponsorRelicEnabled,
                            (s, value) => s.SponsorRelicEnabled = value),
                        ModSettingsText.I18N(i18n, "neuvillette.settings.sponsor_relic.enabled.description",
                            "When enabled, Sponsor relics (e.g. Cat Cake) can appear in runs. When disabled, they are excluded."))));
    }

    private static void SetAct4Enabled(NeuvilletteSettings settings, bool enabled)
    {
        settings.Act4Enabled = enabled;
        RunState? runState = NeuvilletteSettingsStore.TrySetActiveRunAct4Enabled(enabled);
        if (runState != null)
            FourQuadrantsLandPatch.EnsureMarked(runState);
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
