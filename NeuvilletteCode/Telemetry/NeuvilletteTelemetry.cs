using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;

namespace Neuvillette.Telemetry;

public static class NeuvilletteTelemetry
{
    private const string ApplicantId = MainFile.ModId;
    private static ITelemetryClient Client = null!;

    public static void Register()
    {
        TelemetryRegistry.RegisterApplicant(new TelemetryApplicant
        {
            ApplicantId = ApplicantId,
            OwnerModId = MainFile.ModId,
            DisplayName = "Neuvillette",
            DisplayNameText = ModSettingsText.Literal("Neuvillette"),
            Adapter = new PostHogTelemetryAdapter(
                host: "https://neuvillette.neuvillette.workers.dev",
                projectApiKey: "proxy"),
            Requests =
            [
                TelemetryRequest.BasicUsage(
                    ModSettingsText.Literal("Send version, platform, language, and anonymous install ID to estimate compatibility issues.")),
                TelemetryRequest.RunHistory(
                    ModSettingsText.Literal("Send completed run history data to analyze balance.")),
                TelemetryRequest.Diagnostics(
                    ModSettingsText.Literal("Send exceptions and diagnostic context to locate crashes.")),
            ],
        });

        Client = TelemetryApi.GetClient(ApplicantId);
    }

    public static void CaptureAct4Reached()
    {
        Client.Capture(
            eventName: "act4.reached",
            requestId: "basic_usage",
            properties: new Dictionary<string, object?>
            {
                ["act4_enabled"] = NeuvilletteSettingsStore.IsAct4Enabled(),
            });
    }

    public static void CaptureNarwhalDefeated()
    {
        Client.Capture(
            eventName: "narwhal.defeated",
            requestId: "basic_usage",
            properties: new Dictionary<string, object?>
            {
                ["act4_enabled"] = NeuvilletteSettingsStore.IsAct4Enabled(),
            });
    }
}