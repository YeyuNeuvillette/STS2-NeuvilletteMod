using System.Text.Json.Serialization;

namespace Sts2CharacterModel;

public enum EvidenceLevel
{
    Exact,
    Derived,
    Simulated,
    Interpretive,
    Unresolved
}

public sealed record SourceReference(
    string RelativePath,
    int StartLine,
    string Sha256,
    DateTime LastWriteTimeUtc);

public sealed record CharacterSpec(
    string Id,
    int Ascension,
    int MaxHp,
    decimal StartingHpAfterAncient,
    int StartingGold,
    int MaxEnergy,
    int HandDraw,
    int HandLimit,
    int PotionSlots,
    IReadOnlyList<string> StartingDeck,
    IReadOnlyList<string> StartingRelics,
    EvidenceLevel Evidence,
    string Notes);

public sealed record MetricVector(
    decimal? Damage,
    decimal? Block,
    decimal? Draw,
    decimal? Energy,
    decimal? SelfHpCost,
    decimal? MaxHpGain,
    decimal? DamagePerEnergy,
    decimal? BlockPerEnergy,
    decimal? NetHandDelta,
    decimal? EnergyFeasibility,
    decimal? StrengthSensitivity,
    decimal? TargetSensitivity,
    decimal? ScalingSlope,
    decimal? ConditionalFailureRate);

public sealed record CardSpec(
    string Id,
    string ClassName,
    string DisplayName,
    string Description,
    string Rarity,
    string Type,
    string Target,
    string Cost,
    bool Upgraded,
    bool Exhaust,
    bool Retain,
    bool Innate,
    string UnlockSource,
    string Archetypes,
    string DynamicVariables,
    MetricVector Metrics,
    string EffectModel,
    string UnresolvedComponents,
    EvidenceLevel Evidence,
    string Confidence,
    string SourcePath,
    int SourceLine);

public sealed record RelicSpec(
    string Id,
    string ClassName,
    string DisplayName,
    string Rarity,
    string Scope,
    bool Included,
    string MetricTouches,
    string DynamicVariables,
    string EffectModel,
    EvidenceLevel Evidence,
    string ExclusionReason,
    string SourcePath,
    int SourceLine);

public sealed record PotionSpec(
    string Id,
    string ClassName,
    string DisplayName,
    string Rarity,
    string Scope,
    bool Included,
    string MetricTouches,
    string DynamicVariables,
    string EffectModel,
    EvidenceLevel Evidence,
    string ExclusionReason,
    string SourcePath,
    int SourceLine);

public sealed record AncientOfferingSpec(
    string Ancient,
    int Act,
    string OfferingClass,
    string DisplayName,
    string SlotModel,
    string Availability,
    string ProbabilityModel,
    EvidenceLevel Evidence,
    string SourcePath,
    int SourceLine);

public sealed record EncounterSpec(
    string Act,
    string EncounterClass,
    string Category,
    string Monsters,
    int MinHpA10,
    int MaxHpA10,
    decimal T1Incoming,
    decimal T3Incoming,
    decimal T5Incoming,
    decimal T8Incoming,
    decimal MaxSingleHit,
    int MaxHitCount,
    bool AddsStatus,
    bool AppliesDebuff,
    bool Scales,
    string PressureTags,
    string MoveGraphCoverage,
    EvidenceLevel Evidence,
    string SourcePaths);

public sealed record ScenarioSpec(
    string Id,
    string Act,
    string Encounter,
    int TurnLimit,
    int StartingHp,
    int StartingEnergy,
    int EnemyCount,
    string DeckDefinition,
    string Policy,
    EvidenceLevel Evidence,
    string Assumptions);

public sealed record DynamicCurvePoint(
    string CardId,
    bool Upgraded,
    string Variable,
    decimal Input,
    decimal SecondaryInput,
    decimal Output,
    string Unit,
    string Formula,
    EvidenceLevel Evidence);

public sealed record MapMetric(
    string Act,
    string Population,
    string PointType,
    int Samples,
    decimal Mean,
    decimal P05,
    decimal P50,
    decimal P95,
    decimal Minimum,
    decimal Maximum,
    EvidenceLevel Evidence);

public sealed record RewardMetric(
    string Source,
    string Rarity,
    int Samples,
    decimal Probability,
    decimal Ci95HalfWidth,
    string OffsetBehavior,
    EvidenceLevel Evidence);

public sealed record PotionRewardMetric(
    string Pattern,
    int Samples,
    decimal DropRate,
    decimal Ci95HalfWidth,
    decimal EndingPityMean,
    EvidenceLevel Evidence);

public sealed record UnknownResolutionMetric(
    int Visit,
    string RoomType,
    int Samples,
    decimal Probability,
    decimal Ci95HalfWidth,
    EvidenceLevel Evidence);

public sealed record BossPairSpec(
    string FirstBoss,
    string SecondBoss,
    int CombinedHp,
    decimal CombinedT8Incoming,
    int BetweenFightHeal,
    string StateTransition,
    EvidenceLevel Evidence);

public sealed record ValidationResult(
    string Test,
    bool Passed,
    string Expected,
    string Actual,
    string Details);

public sealed record SourceConflict(
    string Topic,
    string DocumentClaim,
    string SourceFinding,
    string Resolution,
    EvidenceLevel Evidence,
    string Reference);

public sealed record ResourceCurvePoint(
    string Mechanic,
    decimal Input,
    decimal SecondaryInput,
    decimal Output,
    string Unit,
    string Formula,
    string Boundary,
    EvidenceLevel Evidence,
    string SourcePath,
    int SourceLine);

public sealed record Act4RouteStage(
    int Order,
    string Stage,
    string Requirement,
    string Gain,
    string OpportunityCost,
    bool RequiredForAct4,
    EvidenceLevel Evidence,
    string SourcePath,
    int SourceLine);

public sealed record Act4MapPointSpec(
    int Sequence,
    string PointType,
    int Column,
    int Row,
    bool Optional,
    string Function,
    EvidenceLevel Evidence,
    string SourcePath,
    int SourceLine);

public sealed record ArchetypeMetric(
    string Archetype,
    int CardStates,
    int CardIdentities,
    decimal MeanBaseDamage,
    decimal MeanBaseBlock,
    decimal MeanSelfHpCost,
    decimal MeanDraw,
    decimal MeanEnergy,
    decimal MeanNumericCost,
    decimal UnresolvedRate,
    string PrincipalResource,
    string FailureCondition,
    EvidenceLevel Evidence);

public sealed class BuildSummary
{
    public required string ToolVersion { get; init; }
    public required string Character { get; init; }
    public required int Ascension { get; init; }
    public required ulong Seed { get; init; }
    public required int MapSamplesPerAct { get; init; }
    public required int RewardSamples { get; init; }
    public required CharacterSpec Baseline { get; init; }
    public required Dictionary<string, int> Counts { get; init; }
    public required Dictionary<string, decimal> Aggregates { get; init; }
    public required IReadOnlyList<ValidationResult> Validations { get; init; }
    public required string SourceFingerprint { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
    [JsonIgnore]
    public bool IsValid => Validations.All(v => v.Passed);
}
