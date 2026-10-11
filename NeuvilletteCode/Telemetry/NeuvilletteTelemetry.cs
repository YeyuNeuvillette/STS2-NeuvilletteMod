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
                host: "https://telemetry.yutopiatelemetry.xyz",
                projectApiKey: "proxy"),
            Requests =
            [
                TelemetryRequest.BasicUsage(
                    ModSettingsText.Literal("Send version, platform, language, and anonymous install ID to estimate compatibility issues.")),
                TelemetryRequest.RunHistoryFiltered(
                    ModSettingsText.Literal("Send completed run history data to analyze balance."),
                    captureFilter: context => context.SourceData is STS2RitsuLib.RunEndedEvent evt
                        && !evt.IsAbandoned
                        && evt.Run.Players.Any(p => p.CharacterId?.Entry == "NEUVILLETTE_CHARACTER_NEUVILLETTE"),
                    sharedContributionSubscriptions: []),
                TelemetryRequest.Custom(
                    "onboarding_experience",
                    ModSettingsText.Literal("Send anonymous onboarding milestones and combat difficulty summaries to improve the first ten Neuvillette runs.")),
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

    public static void CaptureOnboardingRunStarted(int runOrdinal, int tier)
    {
        Client.Capture(
            eventName: "onboarding.run_started",
            requestId: "onboarding_experience",
            properties: new Dictionary<string, object?>
            {
                ["run_ordinal"] = runOrdinal,
                ["tier"] = tier,
            });
    }

    public static void CaptureOnboardingReward(int runOrdinal, int rewardIndex, int lesson, string cardId)
    {
        Client.Capture(
            eventName: "onboarding.reward_guided",
            requestId: "onboarding_experience",
            properties: new Dictionary<string, object?>
            {
                ["run_ordinal"] = runOrdinal,
                ["reward_index"] = rewardIndex,
                ["lesson"] = lesson,
                ["card_id"] = cardId,
            });
    }

    public static void CaptureOnboardingLesson(int runOrdinal, int learnedMask, int newlyLearnedMask)
    {
        Client.Capture(
            eventName: "onboarding.lesson_learned",
            requestId: "onboarding_experience",
            properties: new Dictionary<string, object?>
            {
                ["run_ordinal"] = runOrdinal,
                ["learned_mask"] = learnedMask,
                ["newly_learned_mask"] = newlyLearnedMask,
            });
    }

    public static void CaptureOnboardingCombat(
        int runOrdinal,
        int combatIndex,
        int previousTier,
        int currentTier,
        int startingHp,
        int endingHp,
        int startingMaxHp,
        int turns)
    {
        Client.Capture(
            eventName: "onboarding.combat_completed",
            requestId: "onboarding_experience",
            properties: new Dictionary<string, object?>
            {
                ["run_ordinal"] = runOrdinal,
                ["combat_index"] = combatIndex,
                ["previous_tier"] = previousTier,
                ["current_tier"] = currentTier,
                ["hp_loss_ratio"] = Math.Max(0, startingHp - endingHp) / (decimal)Math.Max(1, startingMaxHp),
                ["remaining_hp_ratio"] = Math.Max(0, endingHp) / (decimal)Math.Max(1, startingMaxHp),
                ["turns"] = turns,
            });
    }
}
