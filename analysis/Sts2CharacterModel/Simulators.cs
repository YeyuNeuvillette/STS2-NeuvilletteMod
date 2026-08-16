using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace Sts2CharacterModel;

internal sealed class MapSimulator
{
    private static readonly MapPointType[] TrackedTypes =
    [
        MapPointType.Monster, MapPointType.Elite, MapPointType.RestSite,
        MapPointType.Shop, MapPointType.Unknown, MapPointType.Treasure
    ];

    public IReadOnlyList<MapMetric> Run(int samples, ulong seed)
    {
        ConfigureAscension10();
        var acts = new (string Name, Func<ActModel> Factory, bool SecondBoss)[]
        {
            ("Overgrowth", () => new Overgrowth(), false),
            ("Underdocks", () => new Underdocks(), false),
            ("Hive", () => new Hive(), false),
            ("Glory", () => new Glory(), true)
        };
        var result = new List<MapMetric>();
        for (var actIndex = 0; actIndex < acts.Length; actIndex++)
        {
            var act = acts[actIndex];
            var data = new Dictionary<(string Population, MapPointType Type), int[]>();
            foreach (var type in TrackedTypes)
            foreach (var population in new[] { "AllNodes", "UniformRoute", "RouteMinimum", "RouteMaximum" })
                data[(population, type)] = new int[samples];

            Parallel.For(0, samples, i =>
            {
                var mapSeed = unchecked(seed + (ulong)(actIndex + 1) * 1_000_003UL + (ulong)i * 7_919UL);
                var map = new StandardActMap(new Rng(mapSeed), act.Factory(), false, false, act.SecondBoss);
                var points = map.GetAllMapPoints().ToList();
                foreach (var type in TrackedTypes)
                    data[("AllNodes", type)][i] = points.Count(p => p.PointType == type);

                var routeCounts = SampleUniformRoute(map, mapSeed ^ 0x9E3779B97F4A7C15UL);
                foreach (var type in TrackedTypes)
                {
                    data[("UniformRoute", type)][i] = routeCounts.GetValueOrDefault(type);
                    data[("RouteMinimum", type)][i] = RouteExtremum(map.StartingMapPoint, type, false, new Dictionary<MapPoint, int>());
                    data[("RouteMaximum", type)][i] = RouteExtremum(map.StartingMapPoint, type, true, new Dictionary<MapPoint, int>());
                }
            });

            foreach (var ((population, type), values) in data.OrderBy(x => x.Key.Population).ThenBy(x => x.Key.Type))
            {
                Array.Sort(values);
                result.Add(new MapMetric(act.Name, population, type.ToString(), samples,
                    decimal.Round((decimal)values.Average(), 4), Quantile(values, 0.05), Quantile(values, 0.50), Quantile(values, 0.95),
                    values[0], values[^1], EvidenceLevel.Simulated));
            }
        }
        return result;
    }

    private static Dictionary<MapPointType, int> SampleUniformRoute(ActMap map, ulong seed)
    {
        var random = new System.Random(unchecked((int)(seed ^ (seed >> 32))));
        var counts = new Dictionary<MapPointType, int>();
        var current = map.StartingMapPoint;
        var guard = 0;
        while (current.Children.Count > 0 && guard++ < 100)
        {
            var children = current.Children.OrderBy(p => p.coord.row).ThenBy(p => p.coord.col).ToArray();
            current = children[random.Next(children.Length)];
            if (TrackedTypes.Contains(current.PointType)) counts[current.PointType] = counts.GetValueOrDefault(current.PointType) + 1;
        }
        return counts;
    }

    private static int RouteExtremum(MapPoint point, MapPointType type, bool maximum, Dictionary<MapPoint, int> memo)
    {
        if (memo.TryGetValue(point, out var cached)) return cached;
        var own = point.PointType == type ? 1 : 0;
        if (point.Children.Count == 0) return memo[point] = own;
        var childValues = point.Children.Select(c => RouteExtremum(c, type, maximum, memo));
        return memo[point] = own + (maximum ? childValues.Max() : childValues.Min());
    }

    private static decimal Quantile(int[] sorted, double p)
    {
        if (sorted.Length == 0) return 0;
        var index = (sorted.Length - 1) * p;
        var low = (int)Math.Floor(index);
        var high = (int)Math.Ceiling(index);
        if (low == high) return sorted[low];
        return decimal.Round(sorted[low] + (decimal)(index - low) * (sorted[high] - sorted[low]), 4);
    }

    internal static void ConfigureAscension10()
    {
        var state = (RunState)RuntimeHelpers.GetUninitializedObject(typeof(RunState));
        var manager = RunManager.Instance;
        typeof(RunManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, state);
        typeof(RunManager).GetProperty(nameof(RunManager.AscensionManager), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(manager, new AscensionManager(10));
    }
}

internal static class RewardSimulator
{
    public static (IReadOnlyList<RewardMetric> Cards, IReadOnlyList<PotionRewardMetric> Potions) Run(int samples, ulong seed)
    {
        var random = new System.Random(unchecked((int)(seed ^ 0x51ED270BUL)));
        var cardResults = new List<RewardMetric>();
        foreach (var source in new[] { "Regular", "Elite", "Boss", "Shop" })
        {
            var counts = new Dictionary<string, int> { ["Common"] = 0, ["Uncommon"] = 0, ["Rare"] = 0 };
            for (var i = 0; i < samples; i++)
            {
                var offset = -0.05;
                var rarity = RollCard(random.NextDouble(), source, source == "Boss" ? 0 : offset);
                counts[rarity]++;
            }
            foreach (var rarity in counts.Keys)
            {
                var p = (decimal)counts[rarity] / samples;
                cardResults.Add(new RewardMetric(source, rarity, samples, p, BinomialHalfWidth(p, samples),
                    source == "Shop" ? "Reads current offset; does not advance" : source == "Boss" ? "Uses zero offset; rare resets future offset" : "Rare resets to -0.05; otherwise +0.005", EvidenceLevel.Simulated));
            }
        }

        // A representative A10 reward stream. Each encounter reward has three rarity rolls and mutates one shared offset.
        var streamCounts = new Dictionary<string, long> { ["Common"] = 0, ["Uncommon"] = 0, ["Rare"] = 0 };
        long total = 0;
        for (var i = 0; i < samples; i++)
        {
            var offset = -0.05;
            foreach (var source in RepresentativeRewardStream())
            {
                for (var candidate = 0; candidate < 3; candidate++)
                {
                    var effectiveOffset = source == "Boss" ? 0 : offset;
                    var rarity = RollCard(random.NextDouble(), source, effectiveOffset);
                    streamCounts[rarity]++;
                    total++;
                    if (source != "Shop") offset = rarity == "Rare" ? -0.05 : Math.Min(0.4, offset + 0.005);
                }
            }
        }
        foreach (var rarity in streamCounts.Keys)
        {
            var p = (decimal)streamCounts[rarity] / total;
            cardResults.Add(new RewardMetric("RepresentativeRunStream", rarity, (int)Math.Min(int.MaxValue, total), p, BinomialHalfWidth(p, total),
                "12 regular + 3 elite + 2 boss rewards, 3 candidates each; persistent A10 offset", EvidenceLevel.Simulated));
        }
        cardResults.Add(new RewardMetric("UpgradeAct1NonRare", "Upgraded", samples, 0m, 0m,
            "A7 Scarcity: base 0 + actIndex(0)×0.125; rare cards always use base 0", EvidenceLevel.Exact));
        cardResults.Add(new RewardMetric("UpgradeAct2NonRare", "Upgraded", samples, 0.125m, 0m,
            "A7 Scarcity: base 0 + actIndex(1)×0.125", EvidenceLevel.Exact));
        cardResults.Add(new RewardMetric("UpgradeAct3NonRare", "Upgraded", samples, 0.25m, 0m,
            "A7 Scarcity: base 0 + actIndex(2)×0.125", EvidenceLevel.Exact));
        cardResults.Add(new RewardMetric("UpgradeRareAnyAct", "Upgraded", samples, 0m, 0m,
            "Rare cards do not receive act-index upgrade scaling", EvidenceLevel.Exact));
        cardResults.Add(new RewardMetric("UpgradeShop", "Upgraded", samples, 0m, 0m,
            "Merchant calls RollForUpgrade with an effectively impossible base chance", EvidenceLevel.Exact));

        var potionResults = new List<PotionRewardMetric>();
        foreach (var pattern in new[] { "NormalOnly", "EliteOnly", "MixedRoute" })
        {
            long drops = 0;
            decimal ending = 0;
            var trialsPerRun = pattern == "MixedRoute" ? 15 : 12;
            for (var i = 0; i < samples; i++)
            {
                decimal pity = 0.4m;
                for (var t = 0; t < trialsPerRun; t++)
                {
                    var elite = pattern == "EliteOnly" || (pattern == "MixedRoute" && t % 5 == 4);
                    var chance = pity + (elite ? 0.125m : 0m);
                    if ((decimal)random.NextDouble() < chance)
                    {
                        drops++;
                        pity -= 0.1m;
                    }
                    else pity += 0.1m;
                }
                ending += pity;
            }
            var totalTrials = (long)samples * trialsPerRun;
            var rate = (decimal)drops / totalTrials;
            potionResults.Add(new PotionRewardMetric(pattern, (int)Math.Min(int.MaxValue, totalTrials), rate,
                BinomialHalfWidth(rate, totalTrials), ending / samples, EvidenceLevel.Simulated));
        }
        return (cardResults, potionResults);
    }

    private static IEnumerable<string> RepresentativeRewardStream()
    {
        for (var i = 0; i < 6; i++) yield return "Regular";
        yield return "Elite";
        yield return "Boss";
        for (var i = 0; i < 4; i++) yield return "Regular";
        yield return "Elite";
        yield return "Boss";
        for (var i = 0; i < 2; i++) yield return "Regular";
        yield return "Elite";
    }

    private static string RollCard(double roll, string source, double offset)
    {
        var (rare, uncommon) = source switch
        {
            "Regular" => (0.0149, 0.37),
            "Elite" => (0.05, 0.40),
            "Boss" => (1.0, 0.0),
            "Shop" => (0.045, 0.37),
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
        if (roll < rare + offset) return "Rare";
        if (roll < rare + offset + uncommon) return "Uncommon";
        return "Common";
    }

    private static decimal BinomialHalfWidth(decimal p, long n) =>
        n == 0 ? 0 : decimal.Round(1.96m * (decimal)Math.Sqrt((double)(p * (1 - p) / n)), 6);
}

internal static class UnknownResolutionSimulator
{
    public static IReadOnlyList<UnknownResolutionMetric> Run(int samples, ulong seed, int visits = 12)
    {
        var random = new System.Random(unchecked((int)(seed ^ 0xA11CE55UL)));
        var roomTypes = new[] { "Monster", "Treasure", "Shop", "Event" };
        var counts = roomTypes.ToDictionary(x => x, _ => new long[visits + 1]);
        for (var sample = 0; sample < samples; sample++)
        {
            decimal monster = 0.10m, treasure = 0.02m, shop = 0.03m;
            for (var visit = 1; visit <= visits; visit++)
            {
                var roll = (decimal)random.NextDouble();
                var room = roll <= monster ? "Monster"
                    : roll <= monster + treasure ? "Treasure"
                    : roll <= monster + treasure + shop ? "Shop" : "Event";
                counts[room][visit]++;
                counts[room][0]++;
                monster = room == "Monster" ? 0.10m : monster + 0.10m;
                treasure = room == "Treasure" ? 0.02m : treasure + 0.02m;
                shop = room == "Shop" ? 0.03m : shop + 0.03m;
            }
        }
        var result = new List<UnknownResolutionMetric>();
        foreach (var room in roomTypes)
        {
            var aggregateN = (long)samples * visits;
            var aggregateP = (decimal)counts[room][0] / aggregateN;
            result.Add(new UnknownResolutionMetric(0, room, (int)Math.Min(int.MaxValue, aggregateN), aggregateP,
                HalfWidth(aggregateP, aggregateN), EvidenceLevel.Simulated));
            for (var visit = 1; visit <= visits; visit++)
            {
                var p = (decimal)counts[room][visit] / samples;
                result.Add(new UnknownResolutionMetric(visit, room, samples, p, HalfWidth(p, samples), EvidenceLevel.Simulated));
            }
        }
        return result;
    }

    private static decimal HalfWidth(decimal p, long n) =>
        decimal.Round(1.96m * (decimal)Math.Sqrt((double)(p * (1 - p) / n)), 6);
}

internal sealed class EncounterScanner
{
    private readonly string _sourceRoot;
    private readonly SourceTracker _tracker;
    private readonly Assembly _assembly = typeof(MonsterModel).Assembly;

    public EncounterScanner(string sourceRoot, SourceTracker tracker)
    {
        _sourceRoot = sourceRoot;
        _tracker = tracker;
        MapSimulator.ConfigureAscension10();
    }

    public (IReadOnlyList<EncounterSpec> Critical, IReadOnlyList<EncounterSpec> Frontier, IReadOnlyList<BossPairSpec> BossPairs) Scan()
    {
        var acts = new[] { "Overgrowth", "Underdocks", "Hive", "Glory" };
        var critical = new List<EncounterSpec>();
        var normal = new List<EncounterSpec>();
        foreach (var act in acts)
        {
            var actPath = $"src/Core/Models/Acts/{act}.cs";
            var source = _tracker.Read(actPath);
            var encounterNames = Regex.Matches(source, @"ModelDb\.Encounter<(?<name>[A-Za-z0-9_]+)>\(\)")
                .Select(m => m.Groups["name"].Value).Distinct().ToList();
            foreach (var encounter in encounterNames)
            {
                var category = encounter.EndsWith("Boss", StringComparison.Ordinal) ? "Boss"
                    : encounter.EndsWith("Elite", StringComparison.Ordinal) ? "Elite"
                    : encounter.EndsWith("Weak", StringComparison.Ordinal) ? "Weak"
                    : "Normal";
                var spec = BuildEncounter(act, encounter, category);
                if (category is "Boss" or "Elite") critical.Add(spec); else normal.Add(spec);
            }
        }

        var frontier = normal.Where(candidate => !normal.Any(other => other.Act == candidate.Act && Dominates(other, candidate))).ToList();
        var bosses = critical.Where(x => x.Act == "Glory" && x.Category == "Boss").OrderBy(x => x.EncounterClass).ToList();
        var pairs = new List<BossPairSpec>();
        foreach (var first in bosses)
        foreach (var second in bosses.Where(x => x.EncounterClass != first.EncounterClass))
            pairs.Add(new BossPairSpec(first.EncounterClass, second.EncounterClass, first.MaxHpA10 + second.MaxHpA10,
                first.T8Incoming + second.T8Incoming, 6,
                "Separate combats; Burning Blood heals 6 after first victory if alive; no card/relic reward inserted between A10 bosses",
                EvidenceLevel.Derived));
        return (critical.OrderBy(x => x.Act).ThenBy(x => x.Category).ThenBy(x => x.EncounterClass).ToList(), frontier, pairs);
    }

    private EncounterSpec BuildEncounter(string act, string encounter, string category)
    {
        var encounterPath = $"src/Core/Models/Encounters/{encounter}.cs";
        var encounterSource = _tracker.Read(encounterPath);
        var monsterNames = Regex.Matches(encounterSource, @"ModelDb\.Monster<(?<name>[A-Za-z0-9_]+)>\(\)")
            .Select(m => m.Groups["name"].Value).Distinct().ToList();
        var minHp = 0;
        var maxHp = 0;
        var sequences = new List<List<decimal>>();
        decimal maxSingle = 0;
        var maxHits = 1;
        var addsStatus = false;
        var debuff = false;
        var scales = false;
        var paths = new List<string> { encounterPath };
        var coverage = new List<string>();
        foreach (var monsterName in monsterNames)
        {
            var monsterPath = $"src/Core/Models/Monsters/{monsterName}.cs";
            var source = _tracker.Read(monsterPath);
            paths.Add(monsterPath);
            var type = _assembly.GetType($"MegaCrit.Sts2.Core.Models.Monsters.{monsterName}");
            MonsterModel? model = null;
            try { model = type is null ? null : (MonsterModel?)Activator.CreateInstance(type); } catch { }
            var monsterMinHp = 0;
            var monsterMaxHp = 0;
            if (model is not null)
            {
                try { monsterMinHp = model.MinInitialHp; monsterMaxHp = model.MaxInitialHp; } catch { }
            }
            var values = ReadIntProperties(model, type);
            if (monsterMinHp == 0) monsterMinHp = ParseHp(source, "MinInitialHp");
            if (monsterMaxHp == 0) monsterMaxHp = ParseHp(source, "MaxInitialHp");
            // Test Subject is a three-form boss. Initial HP alone understates the damage gate.
            if (monsterName == "TestSubject")
            {
                var phase2 = values.GetValueOrDefault("SecondFormHp", ParseHp(source, "SecondFormHp"));
                var phase3 = values.GetValueOrDefault("ThirdFormHp", ParseHp(source, "ThirdFormHp"));
                monsterMinHp += phase2 + phase3;
                monsterMaxHp += phase2 + phase3;
            }
            minHp += monsterMinHp;
            maxHp += monsterMaxHp;
            var seq = ParseMoveSequence(source, values, out var monsterMaxSingle, out var monsterMaxHits, out var exactCycle);
            sequences.Add(seq);
            maxSingle = Math.Max(maxSingle, monsterMaxSingle);
            maxHits = Math.Max(maxHits, monsterMaxHits);
            addsStatus |= source.Contains("StatusIntent", StringComparison.Ordinal) || source.Contains("AddToCombat", StringComparison.Ordinal);
            debuff |= source.Contains("DebuffIntent", StringComparison.Ordinal) || source.Contains("WeakPower", StringComparison.Ordinal) || source.Contains("VulnerablePower", StringComparison.Ordinal);
            scales |= source.Contains("StrengthPower", StringComparison.Ordinal) || source.Contains("AdditionalStrength", StringComparison.Ordinal) || source.Contains("Scaling", StringComparison.Ordinal);
            coverage.Add(exactCycle ? "LinearCycle" : "PartialMoveGraph");
        }

        decimal Incoming(int turn)
        {
            decimal total = 0;
            foreach (var seq in sequences)
            {
                if (seq.Count == 0) continue;
                for (var t = 0; t < turn; t++) total += seq[t % seq.Count];
            }
            return total;
        }
        var tags = new List<string>();
        if (maxSingle >= 25) tags.Add("Burst");
        if (maxHits >= 2) tags.Add("MultiHit");
        if (monsterNames.Count > 1) tags.Add("MultiEnemy");
        if (addsStatus) tags.Add("StatusPollution");
        if (debuff) tags.Add("Debuff");
        if (scales) tags.Add("Scaling");
        return new EncounterSpec(act, encounter, category, string.Join(';', monsterNames), minHp, maxHp,
            Incoming(1), Incoming(3), Incoming(5), Incoming(8), maxSingle, maxHits,
            addsStatus, debuff, scales, string.Join(';', tags), string.Join(';', coverage.Distinct()),
            coverage.All(x => x == "LinearCycle") ? EvidenceLevel.Derived : EvidenceLevel.Unresolved,
            string.Join(';', paths));
    }

    private static Dictionary<string, int> ReadIntProperties(MonsterModel? model, Type? type)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        if (model is null || type is null) return result;
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     .Where(p => p.PropertyType == typeof(int) && p.GetIndexParameters().Length == 0))
        {
            try { result[property.Name] = (int)(property.GetValue(model) ?? 0); } catch { }
        }
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                     .Where(f => f.FieldType == typeof(int) && f.IsLiteral))
        {
            try { result[field.Name] = (int)(field.GetRawConstantValue() ?? 0); } catch { }
        }
        return result;
    }

    private static List<decimal> ParseMoveSequence(string source, Dictionary<string, int> values, out decimal maxSingle, out int maxHits, out bool exactCycle)
    {
        var sequence = new List<decimal>();
        maxSingle = 0;
        maxHits = 1;
        foreach (Match match in Regex.Matches(source, @"new\s+(?<kind>SingleAttackIntent|MultiAttackIntent)\(\s*(?<damage>[A-Za-z0-9_]+)(?:\s*,\s*(?<hits>[A-Za-z0-9_]+))?"))
        {
            var damage = Resolve(match.Groups["damage"].Value, values);
            var hits = match.Groups["kind"].Value == "MultiAttackIntent" ? Resolve(match.Groups["hits"].Value, values) : 1;
            if (hits <= 0) hits = 1;
            sequence.Add(damage * hits);
            maxSingle = Math.Max(maxSingle, damage);
            maxHits = Math.Max(maxHits, hits);
        }
        exactCycle = sequence.Count > 0 && source.Contains("FollowUpState", StringComparison.Ordinal)
                     && !source.Contains("RandomState", StringComparison.Ordinal)
                     && !source.Contains("ConditionalState", StringComparison.Ordinal);
        return sequence;
    }

    private static int Resolve(string token, IReadOnlyDictionary<string, int> values)
    {
        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) return number;
        return values.GetValueOrDefault(token, 0);
    }

    private static int ParseHp(string source, string property)
    {
        var match = Regex.Match(source, property + @"\s*=>\s*(?:AscensionHelper\.GetValueIfAscension\([^,]+,\s*)?(?<v>\d+)");
        return match.Success ? int.Parse(match.Groups["v"].Value, CultureInfo.InvariantCulture) : 0;
    }

    private static bool Dominates(EncounterSpec a, EncounterSpec b)
    {
        var greaterOrEqual = a.MaxHpA10 >= b.MaxHpA10 && a.T1Incoming >= b.T1Incoming && a.T3Incoming >= b.T3Incoming
                             && a.T5Incoming >= b.T5Incoming && a.T8Incoming >= b.T8Incoming && a.MaxSingleHit >= b.MaxSingleHit
                             && a.MaxHitCount >= b.MaxHitCount && (!b.AddsStatus || a.AddsStatus) && (!b.AppliesDebuff || a.AppliesDebuff) && (!b.Scales || a.Scales);
        var strict = a.MaxHpA10 > b.MaxHpA10 || a.T1Incoming > b.T1Incoming || a.T3Incoming > b.T3Incoming
                     || a.T5Incoming > b.T5Incoming || a.T8Incoming > b.T8Incoming || a.MaxSingleHit > b.MaxSingleHit
                     || a.MaxHitCount > b.MaxHitCount || (a.AddsStatus && !b.AddsStatus) || (a.AppliesDebuff && !b.AppliesDebuff) || (a.Scales && !b.Scales);
        return greaterOrEqual && strict;
    }
}
