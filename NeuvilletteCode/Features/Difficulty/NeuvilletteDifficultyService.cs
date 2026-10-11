using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Neuvillette.Characters.Neuvillette.Cards;
using Neuvillette.Characters.Neuvillette.Powers;
using Neuvillette.Infrastructure;
using Neuvillette.Telemetry;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils;

namespace Neuvillette.Features.Difficulty;

/// <summary>
/// Persisted with the run so combat assistance and the first-run reward guide are
/// deterministic across save/load. The vanilla Ascension level is never changed.
/// </summary>
public sealed class NeuvilletteDifficultyRunState
{
    public int SchemaVersion { get; set; } = 4;
    public bool Enabled { get; set; }
    public int RunOrdinal { get; set; }
    public int BaselineTier { get; set; }
    public int Tier { get; set; }
    public int CompletedCombats { get; set; }
    public int AdjustmentMomentum { get; set; }
    public int ReviveChargesGranted { get; set; }
    public int RemainingRevives { get; set; }
    public int CombatStartHp { get; set; }
    public int CombatStartMaxHp { get; set; }
    public string ActiveCombatKey { get; set; } = string.Empty;
    public string LastEvaluatedCombatKey { get; set; } = string.Empty;
    public int GuidedRewardCount { get; set; }
    public string LastGuidedRewardKey { get; set; } = string.Empty;
    public int LearnedMechanics { get; set; }
    public int LastSuggestedLesson { get; set; } = -1;
}

internal static class NeuvilletteDifficultyService
{
    private const int CurrentSchemaVersion = 4;
    private const int OnboardingRunLimit = 10;
    private const int GuidedRewardLimit = 3;
    private const string RunSavedDataKey = "difficulty_state";

    private static readonly ConditionalWeakTable<RunState, HashSet<string>> GuidedRewardsAppliedThisSession = new();

    private static readonly RunSavedData<NeuvilletteDifficultyRunState> SavedState =
        RunSavedDataStore.For(MainFile.ModId).Register<NeuvilletteDifficultyRunState>(
            key: RunSavedDataKey,
            defaultFactory: static () => new NeuvilletteDifficultyRunState(),
            options: new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.AlwaysWhenRegistered });

    internal static void Register()
    {
        // Ensure this key exists before a run is deserialized.
        _ = SavedState;
    }

    internal static void InitializeNewRun(RunState runState)
    {
        (int completedRuns, int wins, int losses) = GetCareerStats();
        int runOrdinal = completedRuns + 1;
        int baselineTier = GetInitialTier(runOrdinal, completedRuns, wins, losses);

        int reviveAllowance = GetReviveAllowance(baselineTier);
        var state = new NeuvilletteDifficultyRunState
        {
            SchemaVersion = CurrentSchemaVersion,
            Enabled = ContainsNeuvillette(runState)
                && NeuvilletteSettingsStore.IsRunOnboardingDifficultyEnabled(runState)
                && runOrdinal <= OnboardingRunLimit,
            RunOrdinal = runOrdinal,
            BaselineTier = baselineTier,
            Tier = baselineTier,
            ReviveChargesGranted = reviveAllowance,
            RemainingRevives = reviveAllowance,
            LearnedMechanics = NeuvilletteSettingsStore.GetOnboardingLearnedMechanics(),
        };
        SavedState.Set(runState, state);

        if (state.Enabled)
            NeuvilletteTelemetry.CaptureOnboardingRunStarted(runOrdinal, baselineTier);
    }

    internal static void EnsureRunState(RunState runState)
    {
        if (SavedState.TryGet(runState, out NeuvilletteDifficultyRunState? state))
        {
            if (state.SchemaVersion < CurrentSchemaVersion)
            {
                if (state.SchemaVersion < 4)
                {
                    int reviveAllowance = GetReviveAllowance(state.Tier);
                    state.ReviveChargesGranted = reviveAllowance;
                    state.RemainingRevives = reviveAllowance;
                }
                state.SchemaVersion = CurrentSchemaVersion;
                state.LearnedMechanics |= NeuvilletteSettingsStore.GetOnboardingLearnedMechanics();
                SavedState.Set(runState, state);
            }
            return;
        }

        // Migrates active runs created by the earlier manual-mode prototype.
        InitializeNewRun(runState);
    }

    internal static async Task OnCombatStarted(IRunState runState, ICombatState? combatState)
    {
        if (runState is not RunState concreteRun
            || combatState is null
            || !TryGetActiveState(concreteRun, out NeuvilletteDifficultyRunState state))
        {
            return;
        }

        string key = GetCombatKey(concreteRun);
        if (state.ActiveCombatKey != key)
        {
            state.ActiveCombatKey = key;
            state.CombatStartHp = combatState.Players.Sum(static player => player.Creature.CurrentHp);
            state.CombatStartMaxHp = combatState.Players.Sum(static player => player.Creature.MaxHp);
            SavedState.Set(concreteRun, state);
        }

        int reviveCharges = state.RemainingRevives;
        if (reviveCharges <= 0)
            return;

        foreach (Player player in combatState.Players.Where(static player =>
                     player.Character is Characters.Neuvillette.Neuvillette
                     || player.Character.Id.Entry == "NEUVILLETTE_CHARACTER_NEUVILLETTE"))
        {
            if (player.Creature.GetPower<OnboardingAssistancePower>() is not null)
                continue;

            await PowerCmd.Apply<OnboardingAssistancePower>(
                new ThrowingPlayerChoiceContext(),
                player.Creature,
                reviveCharges,
                player.Creature,
                null,
                silent: true);
        }

        MainFile.Logger.Info(
            $"Onboarding assistance active: run={state.RunOrdinal}/{OnboardingRunLimit}, " +
            $"tier={state.Tier}, remainingRunRevives={reviveCharges}, reviveHp=30%.");
    }

    internal static void OnCombatEnded(IRunState runState, ICombatState? combatState)
    {
        if (runState is not RunState concreteRun
            || combatState is null
            || !GameCompatibility.IsRunAuthority()
            || !TryGetActiveState(concreteRun, out NeuvilletteDifficultyRunState state))
        {
            return;
        }

        string key = GetCombatKey(concreteRun);
        if (state.ActiveCombatKey != key || state.LastEvaluatedCombatKey == key)
            return;

        state.LastEvaluatedCombatKey = key;
        state.CompletedCombats++;
        int previousTier = state.Tier;
        UpdateAdaptiveTier(state, combatState);
        GrantAdditionalRevivesForTier(state);
        SavedState.Set(concreteRun, state);

        int currentHp = combatState.Players.Sum(static player => Math.Max(0, player.Creature.CurrentHp));
        int turns = combatState.Players
            .Select(static player => player.PlayerCombatState?.TurnNumber ?? 0)
            .DefaultIfEmpty(0)
            .Max();
        NeuvilletteTelemetry.CaptureOnboardingCombat(
            state.RunOrdinal,
            state.CompletedCombats,
            previousTier,
            state.Tier,
            state.CombatStartHp,
            currentHp,
            Math.Max(1, state.CombatStartMaxHp),
            turns);
    }

    /// <summary>
    /// During the first three runs, preserve two random options and put one
    /// approachable engine piece into a decreasing number of combat rewards.
    /// </summary>
    internal static bool TryGuideCardReward(
        IRunState runState,
        Player player,
        List<CardCreationResult> rewardOptions,
        CardCreationOptions creationOptions)
    {
        if (runState is not RunState concreteRun
            || concreteRun.Players.Count != 1
            || player.Character is not Characters.Neuvillette.Neuvillette
            || rewardOptions.Count == 0
            || creationOptions.Source != CardCreationSource.Encounter
            || creationOptions.RarityOdds != CardRarityOddsType.RegularEncounter
            || !creationOptions.Flags.HasFlag(CardCreationFlags.IsFromCombat)
            || !TryGetActiveState(concreteRun, out NeuvilletteDifficultyRunState state)
            || state.RunOrdinal > 3
            || state.GuidedRewardCount >= GetGuidedRewardLimit(state.RunOrdinal))
        {
            return false;
        }

        string rewardKey = GetCombatKey(concreteRun) + $":{player.NetId}";
        HashSet<string> sessionApplications = GuidedRewardsAppliedThisSession.GetOrCreateValue(concreteRun);
        if (!sessionApplications.Add(rewardKey))
            return false;

        int guideIndex;
        if (state.LastGuidedRewardKey == rewardKey)
        {
            // Loading the same reward repeats the same suggestion instead of
            // advancing the guide or rerolling its lesson.
            guideIndex = Math.Max(0, state.GuidedRewardCount - 1);
        }
        else
        {
            guideIndex = state.GuidedRewardCount;
            state.GuidedRewardCount++;
            state.LastGuidedRewardKey = rewardKey;
            SavedState.Set(concreteRun, state);
        }

        (CardModel? suggestedCanonical, int lesson) = GetSuggestedCard(state, guideIndex, creationOptions, player);
        if (suggestedCanonical is null)
            return false;

        state.LastSuggestedLesson = lesson;
        SavedState.Set(concreteRun, state);

        if (rewardOptions.Any(option => option.Card.Id == suggestedCanonical.Id))
            return true;

        CardModel suggestedCard = concreteRun.CreateCard(suggestedCanonical, player);
        rewardOptions[^1].ModifyCard(suggestedCard);
        MainFile.Logger.Info(
            $"Onboarding reward {guideIndex + 1}/{GetGuidedRewardLimit(state.RunOrdinal)}: " +
            $"lesson={lesson}, inserted {suggestedCard.Id} into {rewardKey}.");
        NeuvilletteTelemetry.CaptureOnboardingReward(
            state.RunOrdinal,
            guideIndex + 1,
            lesson,
            suggestedCard.Id.Entry);
        return true;
    }

    internal static string GetStatusText(I18N i18n)
    {
        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        int runOrdinal;
        int tier;
        int remainingRevives;
        int learnedMechanics;

        if (runState != null && SavedState.TryGet(runState, out NeuvilletteDifficultyRunState? state))
        {
            if (!state.Enabled)
            {
                string disabledKey = state.RunOrdinal > OnboardingRunLimit
                    ? "neuvillette.settings.difficulty.status.complete"
                    : "neuvillette.settings.difficulty.status.disabled";
                string disabledFallback = state.RunOrdinal > OnboardingRunLimit
                    ? "Onboarding complete · Ascension rules only."
                    : "Disabled · Ascension rules are unchanged.";
                return i18n.Get(disabledKey, disabledFallback);
            }

            runOrdinal = state.RunOrdinal;
            tier = state.Tier;
            remainingRevives = state.RemainingRevives;
            learnedMechanics = state.LearnedMechanics;
        }
        else
        {
            if (!NeuvilletteSettingsStore.IsOnboardingDifficultyEnabled())
            {
                return i18n.Get(
                    "neuvillette.settings.difficulty.status.disabled",
                    "Disabled · Ascension rules are unchanged.");
            }

            (int completedRuns, int wins, int losses) = GetCareerStats();
            runOrdinal = completedRuns + 1;
            if (runOrdinal > OnboardingRunLimit)
            {
                return i18n.Get(
                    "neuvillette.settings.difficulty.status.complete",
                    "Onboarding complete · Ascension rules only.");
            }

            tier = GetInitialTier(runOrdinal, completedRuns, wins, losses);
            remainingRevives = GetReviveAllowance(tier);
            learnedMechanics = NeuvilletteSettingsStore.GetOnboardingLearnedMechanics();
        }

        string key = tier <= -2
            ? "neuvillette.settings.difficulty.status.strong_aid"
            : tier == -1
                ? "neuvillette.settings.difficulty.status.aid"
                : "neuvillette.settings.difficulty.status.vanilla";
        string fallback = tier <= -2
            ? "Run {Run}/10 · Strong assistance"
            : tier == -1
                ? "Run {Run}/10 · Light assistance"
                : "Run {Run}/10 · Vanilla combat values";
        string stage = i18n.Get(key, fallback)
            .Replace("{Run}", Math.Clamp(runOrdinal, 1, OnboardingRunLimit).ToString())
            .Replace("{Revives}", Math.Max(0, remainingRevives).ToString());
        string lessonKey = (learnedMechanics & 0b111) switch
        {
            0b111 => "neuvillette.settings.difficulty.lesson.complete",
            var mask when (mask & (1 << 0)) == 0 => "neuvillette.settings.difficulty.lesson.surge",
            var mask when (mask & (1 << 1)) == 0 => "neuvillette.settings.difficulty.lesson.sourcewater",
            _ => "neuvillette.settings.difficulty.lesson.payoff",
        };
        string lessonFallback = (learnedMechanics & 0b111) switch
        {
            0b111 => "Core mechanics understood",
            var mask when (mask & (1 << 0)) == 0 => "Guide: Surge and Ebb",
            var mask when (mask & (1 << 1)) == 0 => "Guide: Sourcewater Droplets",
            _ => "Guide: spending Sourcewater Droplets",
        };
        return $"{stage} · {i18n.Get(lessonKey, lessonFallback)}";
    }

    internal static void RecordReviveConsumed(Creature creature)
    {
        if (!GameCompatibility.IsRunAuthority()
            || creature.Player?.RunState is not RunState runState
            || !TryGetActiveState(runState, out NeuvilletteDifficultyRunState state)
            || state.RemainingRevives <= 0)
        {
            return;
        }

        state.RemainingRevives--;
        SavedState.Set(runState, state);
        MainFile.Logger.Info(
            $"Onboarding revive consumed: run={state.RunOrdinal}/{OnboardingRunLimit}, " +
            $"remainingRunRevives={state.RemainingRevives}.");
    }

    internal static void RecordCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.IsAutoPlay
            || !cardPlay.IsFirstInSeries
            || cardPlay.Player.RunState is not RunState runState
            || !GameCompatibility.IsRunAuthority()
            || !TryGetActiveState(runState, out NeuvilletteDifficultyRunState state)
            || cardPlay.Player.Character is not Characters.Neuvillette.Neuvillette)
        {
            return;
        }

        int learned = state.LearnedMechanics;
        CardModel card = cardPlay.Card;
        if (card is SurgeCard)
            learned |= 1 << 0;

        bool hasDroplets = cardPlay.Player.Creature.GetPowerAmount<SourcewaterDroplet>() > 0m;
        if (hasDroplets)
            learned |= 1 << 1;

        if (hasDroplets && IsSourcewaterPayoff(card))
            learned |= 1 << 2;

        int newlyLearned = learned & ~state.LearnedMechanics;
        if (newlyLearned == 0)
            return;

        state.LearnedMechanics = learned;
        SavedState.Set(runState, state);
        NeuvilletteSettingsStore.MarkOnboardingMechanicsLearned(newlyLearned);
        NeuvilletteTelemetry.CaptureOnboardingLesson(state.RunOrdinal, learned, newlyLearned);
        MainFile.Logger.Info($"Onboarding lessons learned: mask={learned}, newlyLearned={newlyLearned}.");
    }

    private static int GetGuidedRewardLimit(int runOrdinal) => Math.Clamp(4 - runOrdinal, 1, GuidedRewardLimit);

    private static (CardModel? Card, int Lesson) GetSuggestedCard(
        NeuvilletteDifficultyRunState state,
        int guideIndex,
        CardCreationOptions options,
        Player player)
    {
        CardModel[][] lessons =
        [
            [ModelDb.Card<Adjournment>(), ModelDb.Card<WalkIntoRain>()],
            [ModelDb.Card<StilledWell>(), ModelDb.Card<SourceOfLife>()],
            [ModelDb.Card<SurgingTorrent>(), ModelDb.Card<AweInspiring>()],
        ];

        HashSet<ModelId> possibleIds = options.GetPossibleCards(player).Select(static card => card.Id).ToHashSet();
        HashSet<ModelId> deckIds = player.Deck.Cards.Select(static card => card.Id).ToHashSet();
        int? firstUnlearned = Enumerable.Range(0, lessons.Length)
            .Where(lesson => (state.LearnedMechanics & (1 << lesson)) == 0)
            .Cast<int?>()
            .FirstOrDefault();
        int startingLesson = firstUnlearned
            ?? Math.Clamp(guideIndex + state.RunOrdinal - 1, 0, lessons.Length - 1);

        for (int offset = 0; offset < lessons.Length; offset++)
        {
            int lesson = (startingLesson + offset) % lessons.Length;
            CardModel? card = lessons[lesson]
                .FirstOrDefault(candidate => possibleIds.Contains(candidate.Id) && !deckIds.Contains(candidate.Id))
                ?? lessons[lesson].FirstOrDefault(candidate => possibleIds.Contains(candidate.Id));
            if (card is not null)
                return (card, lesson);
        }

        return (null, startingLesson);
    }

    private static bool IsSourcewaterPayoff(CardModel card) => card is
        StilledWell or
        SourceOfLife or
        SurgingTorrent or
        AweInspiring or
        EquitableJudgment or
        ObjectionOverruled or
        Retrial or
        OceanicEdict;

    private static bool TryGetActiveState(RunState runState, out NeuvilletteDifficultyRunState state)
    {
        if (!SavedState.TryGet(runState, out state!) || !state.Enabled)
            return false;

        state.BaselineTier = Math.Clamp(state.BaselineTier, -2, 0);
        state.Tier = Math.Clamp(state.Tier, -2, state.BaselineTier);
        return true;
    }

    private static void UpdateAdaptiveTier(NeuvilletteDifficultyRunState state, ICombatState combatState)
    {
        int currentHp = combatState.Players.Sum(static player => Math.Max(0, player.Creature.CurrentHp));
        int currentMaxHp = combatState.Players.Sum(static player => Math.Max(1, player.Creature.MaxHp));
        int startMaxHp = Math.Max(1, state.CombatStartMaxHp);
        decimal hpLossRatio = Math.Max(0, state.CombatStartHp - currentHp) / (decimal)startMaxHp;
        decimal remainingHpRatio = currentHp / (decimal)Math.Max(1, currentMaxHp);
        int turns = combatState.Players
            .Select(static player => player.PlayerCombatState?.TurnNumber ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        bool struggled = remainingHpRatio <= 0.35m || hpLossRatio >= 0.25m;
        bool comfortable = hpLossRatio <= 0.05m && remainingHpRatio >= 0.70m && turns is > 0 and <= 3;

        if (struggled)
            state.AdjustmentMomentum = Math.Max(-2, state.AdjustmentMomentum - 1);
        else if (comfortable)
            state.AdjustmentMomentum = Math.Min(2, state.AdjustmentMomentum + 1);
        else if (state.AdjustmentMomentum != 0)
            state.AdjustmentMomentum -= Math.Sign(state.AdjustmentMomentum);

        if (state.AdjustmentMomentum <= -2)
        {
            state.Tier--;
            state.AdjustmentMomentum = 0;
        }
        else if (state.AdjustmentMomentum >= 2)
        {
            state.Tier++;
            state.AdjustmentMomentum = 0;
        }

        // Assistance may grow when the player struggles, but never turns into a
        // penalty or rises above this run's experience-based baseline.
        state.Tier = Math.Clamp(state.Tier, -2, state.BaselineTier);
    }

    private static int GetInitialTier(int runOrdinal, int completedRuns, int wins, int losses)
    {
        int tier = runOrdinal switch
        {
            <= 1 => -2,
            <= 3 => -1,
            _ => 0,
        };

        if (completedRuns >= 2 && wins == 0)
            tier--;
        if (completedRuns >= 4 && wins == 0)
            tier--;
        else if (completedRuns >= 2 && wins * 2 >= completedRuns)
            tier++;

        return Math.Clamp(tier, -2, 0);
    }

    private static int GetReviveAllowance(int tier) => tier <= -2 ? 2 : tier == -1 ? 1 : 0;

    private static void GrantAdditionalRevivesForTier(NeuvilletteDifficultyRunState state)
    {
        int allowance = GetReviveAllowance(state.Tier);
        if (allowance <= state.ReviveChargesGranted)
            return;

        state.RemainingRevives += allowance - state.ReviveChargesGranted;
        state.ReviveChargesGranted = allowance;
    }

    private static (int CompletedRuns, int Wins, int Losses) GetCareerStats()
    {
        try
        {
            ModelId id = ModelDb.Character<Characters.Neuvillette.Neuvillette>().Id;
            CharacterStats? stats = SaveManager.Instance.Progress.GetStatsForCharacter(id);
            int wins = Math.Max(0, stats?.TotalWins ?? 0);
            int losses = Math.Max(0, stats?.TotalLosses ?? 0);
            return (wins + losses, wins, losses);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Could not read Neuvillette run history for onboarding difficulty: {ex.Message}");
            return (0, 0, 0);
        }
    }

    private static bool ContainsNeuvillette(IRunState runState) =>
        runState.Players.Any(static player =>
            player.Character is Characters.Neuvillette.Neuvillette
            || player.Character.Id.Entry == "NEUVILLETTE_CHARACTER_NEUVILLETTE");

    private static string GetCombatKey(RunState runState)
    {
        var coord = runState.CurrentMapCoord;
        string mapPoint = coord.HasValue ? $"{coord.Value.col}:{coord.Value.row}" : "none";
        string roomId = runState.CurrentRoom?.ModelId?.Entry ?? "none";
        return $"{runState.CurrentActIndex}:{mapPoint}:{runState.CurrentRoomCount}:{roomId}";
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.TryModifyCardRewardOptions))]
internal static class NeuvilletteOnboardingCardRewardPatch
{
    [HarmonyPostfix]
    private static void GuideFirstRunRewards(
        IRunState runState,
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions,
        ref bool __result)
    {
        if (NeuvilletteDifficultyService.TryGuideCardReward(
                runState,
                player,
                cardRewardOptions,
                creationOptions))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
internal static class NeuvilletteDifficultyCombatStartPatch
{
    [HarmonyPostfix]
    private static void RecordStartingHealth(IRunState runState, ICombatState? combatState, ref Task __result)
    {
        __result = AfterOriginal(__result, runState, combatState);
    }

    private static async Task AfterOriginal(Task original, IRunState runState, ICombatState? combatState)
    {
        await original;
        await NeuvilletteDifficultyService.OnCombatStarted(runState, combatState);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
internal static class NeuvilletteDifficultyCombatEndPatch
{
    [HarmonyPostfix]
    private static void EvaluateCombat(IRunState runState, ICombatState? combatState, ref Task __result)
    {
        __result = AfterOriginal(__result, runState, combatState);
    }

    private static async Task AfterOriginal(Task original, IRunState runState, ICombatState? combatState)
    {
        await original;
        NeuvilletteDifficultyService.OnCombatEnded(runState, combatState);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class NeuvilletteOnboardingCardPlayedPatch
{
    [HarmonyPostfix]
    private static void LearnFromManualCardPlay(CardPlay cardPlay)
    {
        NeuvilletteDifficultyService.RecordCardPlayed(cardPlay);
    }
}
