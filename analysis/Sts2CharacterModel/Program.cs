using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace Sts2CharacterModel;

internal static class Program
{
    private const string ToolVersion = "2.0.0";

    public static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            var options = BuildOptions.Parse(args);
            if (options is null)
            {
                PrintUsage();
                return 2;
            }

            return Build(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"fatal: {ex.GetBaseException().Message}");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int Build(BuildOptions options)
    {
        if (options.Character.Equals("Neuvillette", StringComparison.OrdinalIgnoreCase))
        {
            var modSource = options.ModSource ?? Path.GetFullPath(Path.Combine(options.Source, "..", "Neuvillette", "Neuvillette"));
            return NeuvilletteModelBuilder.Build(
                options.Source, modSource, options.Output, options.Ascension, options.Seed,
                options.MapSamples, options.RewardSamples, options.Act4Enabled, options.SponsorRelicsEnabled);
        }
        if (!options.Character.Equals("Ironclad", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("v2 当前实现 Ironclad 与 Neuvillette 规范数据。", nameof(options.Character));
        if (options.Ascension != 10)
            throw new ArgumentException("本基准必须以 A10 运行；其他进阶需另建场景。", nameof(options.Ascension));
        if (!Directory.Exists(options.Source))
            throw new DirectoryNotFoundException(options.Source);

        Directory.CreateDirectory(options.Output);
        Console.WriteLine("[1/5] 扫描卡牌、遗物、药水和先古供物…");
        var tracker = new SourceTracker(options.Source);
        var scanner = new SourceScanner(options.Source, tracker);
        var cards = scanner.ScanCards();
        var characterRelics = scanner.ScanCharacterRelics();
        var characterPotions = scanner.ScanCharacterPotions();
        var sharedRelics = scanner.ScanSharedRelics();
        var sharedPotions = scanner.ScanSharedPotions();
        var ancients = scanner.ScanAncients();
        var curves = scanner.BuildDynamicCurves(cards);

        Console.WriteLine("[2/5] 扫描精英、Boss 与普通遭遇压力前沿…");
        var encounters = new EncounterScanner(options.Source, tracker).Scan();

        Console.WriteLine($"[3/5] 复现 A10 地图生成：每幕 {options.MapSamples:N0} 张…");
        var maps = new MapSimulator().Run(options.MapSamples, options.Seed);

        Console.WriteLine($"[4/5] 复现奖励保底：{options.RewardSamples:N0} 个样本流…");
        var rewards = RewardSimulator.Run(options.RewardSamples, options.Seed);
        var unknowns = UnknownResolutionSimulator.Run(options.RewardSamples, options.Seed);

        var baseline = BuildBaseline();
        var validations = Validate(cards, characterRelics, characterPotions, sharedRelics, sharedPotions,
            ancients, encounters.Critical, encounters.BossPairs, curves, maps, rewards);
        var conflicts = BuildConflicts(options.Source);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["cardStates"] = cards.Count,
            ["cardIdentities"] = cards.Select(x => x.Id).Distinct().Count(),
            ["characterRelics"] = characterRelics.Count,
            ["characterPotions"] = characterPotions.Count,
            ["sharedRelicsScanned"] = sharedRelics.Count,
            ["sharedRelicsIncluded"] = sharedRelics.Count(x => x.Included),
            ["sharedPotionsScanned"] = sharedPotions.Count,
            ["sharedPotionsIncluded"] = sharedPotions.Count(x => x.Included),
            ["ancients"] = ancients.Select(x => x.Ancient).Distinct().Count(),
            ["ancientOfferings"] = ancients.Count,
            ["bosses"] = encounters.Critical.Count(x => x.Category == "Boss"),
            ["elites"] = encounters.Critical.Count(x => x.Category == "Elite"),
            ["normalFrontier"] = encounters.Frontier.Count,
            ["act3BossOrderedPairs"] = encounters.BossPairs.Count,
            ["unresolvedCardStates"] = cards.Count(x => x.Evidence == EvidenceLevel.Unresolved || x.UnresolvedComponents.Length > 0)
        };
        var aggregates = new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["baseHp"] = baseline.MaxHp,
            ["a10OpeningHp"] = baseline.StartingHpAfterAncient,
            ["burningBloodHeal"] = 6,
            ["startingDeckSize"] = baseline.StartingDeck.Count,
            ["mapCount"] = options.MapSamples * 4,
            ["regularRewardRareProbability"] = rewards.Cards.First(x => x.Source == "Regular" && x.Rarity == "Rare").Probability,
            ["representativeRewardRareProbability"] = rewards.Cards.First(x => x.Source == "RepresentativeRunStream" && x.Rarity == "Rare").Probability
        };

        var generatedAt = tracker.References.Count == 0
            ? DateTime.UnixEpoch
            : tracker.References.Max(x => x.LastWriteTimeUtc);
        var summary = new BuildSummary
        {
            ToolVersion = ToolVersion,
            Character = "Ironclad",
            Ascension = options.Ascension,
            Seed = options.Seed,
            MapSamplesPerAct = options.MapSamples,
            RewardSamples = options.RewardSamples,
            Baseline = baseline,
            Counts = counts,
            Aggregates = aggregates,
            Validations = validations,
            SourceFingerprint = tracker.Fingerprint(),
            GeneratedAtUtc = generatedAt
        };

        Console.WriteLine("[5/5] 写入可复算附件并执行验收…");
        WriteCsv(options.Output, "cards.csv", cards);
        WriteCsv(options.Output, "relics_character.csv", characterRelics);
        WriteCsv(options.Output, "potions_character.csv", characterPotions);
        WriteCsv(options.Output, "relics_shared_scan.csv", sharedRelics);
        WriteCsv(options.Output, "potions_shared_scan.csv", sharedPotions);
        WriteCsv(options.Output, "ancient_offerings.csv", ancients);
        WriteCsv(options.Output, "dynamic_curves.csv", curves);
        WriteCsv(options.Output, "encounters_critical.csv", encounters.Critical);
        WriteCsv(options.Output, "encounters_normal_frontier.csv", encounters.Frontier);
        WriteCsv(options.Output, "boss_pairs_a10.csv", encounters.BossPairs);
        WriteCsv(options.Output, "map_metrics.csv", maps);
        WriteCsv(options.Output, "card_reward_metrics.csv", rewards.Cards);
        WriteCsv(options.Output, "potion_reward_metrics.csv", rewards.Potions);
        WriteCsv(options.Output, "unknown_resolution_metrics.csv", unknowns);
        WriteCsv(options.Output, "source_conflicts.csv", conflicts);
        WriteCsv(options.Output, "validation.csv", validations);
        WriteJson(Path.Combine(options.Output, "baseline.json"), baseline);
        WriteJson(Path.Combine(options.Output, "source_manifest.json"), new
        {
            sourceRoot = Path.GetFullPath(options.Source),
            fingerprint = summary.SourceFingerprint,
            files = tracker.References.OrderBy(x => x.RelativePath, StringComparer.Ordinal).ToArray(),
            externalFiles = ExternalReferences(options.Source)
        });
        WriteJson(Path.Combine(options.Output, "summary.json"), summary);

        foreach (var failed in validations.Where(x => !x.Passed))
            Console.Error.WriteLine($"FAIL {failed.Test}: expected={failed.Expected}; actual={failed.Actual}; {failed.Details}");
        Console.WriteLine($"源码指纹: {summary.SourceFingerprint}");
        Console.WriteLine($"验收: {validations.Count(x => x.Passed)}/{validations.Count}；输出: {Path.GetFullPath(options.Output)}");
        return summary.IsValid ? 0 : 3;
    }

    private static CharacterSpec BuildBaseline() => new(
        "IRONCLAD", 10, 80, 64, 99, 3, 5, 10, 2,
        ["STRIKE_IRONCLAD", "STRIKE_IRONCLAD", "STRIKE_IRONCLAD", "STRIKE_IRONCLAD", "STRIKE_IRONCLAD",
         "DEFEND_IRONCLAD", "DEFEND_IRONCLAD", "DEFEND_IRONCLAD", "DEFEND_IRONCLAD", "BASH", "ASCENDERS_BANE"],
        ["BURNING_BLOOD"], EvidenceLevel.Derived,
        "A10 累积：先古治疗只恢复已损失生命的 80%，因此满血上限 80 的首战起始生命为 64；A4 为 2 药水栏，A5 加入灾厄。燃烧之血每场战斗胜利后恢复 6。"
    );

    private static List<ValidationResult> Validate(
        IReadOnlyList<CardSpec> cards, IReadOnlyList<RelicSpec> relics, IReadOnlyList<PotionSpec> potions,
        IReadOnlyList<RelicSpec> sharedRelics, IReadOnlyList<PotionSpec> sharedPotions,
        IReadOnlyList<AncientOfferingSpec> ancients, IReadOnlyList<EncounterSpec> critical,
        IReadOnlyList<BossPairSpec> pairs, IReadOnlyList<DynamicCurvePoint> curves,
        IReadOnlyList<MapMetric> maps,
        (IReadOnlyList<RewardMetric> Cards, IReadOnlyList<PotionRewardMetric> Potions) rewards)
    {
        var tests = new List<ValidationResult>();
        void Count(string name, int expected, int actual) => tests.Add(new(name, expected == actual, expected.ToString(), actual.ToString(), "精确清单计数"));
        Count("card identities", 90, cards.Select(x => x.Id).Distinct().Count());
        Count("card base+upgrade states", 180, cards.Count);
        Count("character relics", 8, relics.Count);
        Count("character potions", 3, potions.Count);
        Count("shared relic scan", 118, sharedRelics.Count);
        Count("shared potion scan", 45, sharedPotions.Count);
        Count("ancients", 8, ancients.Select(x => x.Ancient).Distinct().Count());
        Count("bosses", 12, critical.Count(x => x.Category == "Boss"));
        Count("elites", 12, critical.Count(x => x.Category == "Elite"));
        Count("A10 ordered act3 boss pairs", 6, pairs.Count);
        void EncounterGolden(string name, int hp, decimal t1, decimal t3)
        {
            var encounter = critical.SingleOrDefault(x => x.EncounterClass == name);
            tests.Add(new($"golden {name} HP", encounter?.MaxHpA10 == hp, hp.ToString(), encounter?.MaxHpA10.ToString() ?? "missing", "A10 阶段/多怪有效生命门槛"));
            tests.Add(new($"golden {name} T1/T3", encounter?.T1Incoming == t1 && encounter?.T3Incoming == t3, $"{t1}/{t3}", encounter is null ? "missing" : $"{encounter.T1Incoming}/{encounter.T3Incoming}", "按源码固定行动序列，不计玩家减益后的放大"));
        }
        EncounterGolden("VantomBoss", 183, 8, 52);
        EncounterGolden("AeonglassBoss", 535, 26, 76);
        EncounterGolden("TestSubjectBoss", 636, 22, 49);

        void CardMetric(string id, bool upgraded, string metric, decimal expected, Func<CardSpec, decimal?> get)
        {
            var card = cards.SingleOrDefault(x => x.Id == id && x.Upgraded == upgraded);
            var actual = card is null ? null : get(card);
            tests.Add(new($"golden {id}{(upgraded ? "+" : string.Empty)} {metric}", actual == expected,
                expected.ToString(), actual?.ToString() ?? "missing", "源码实例化金样"));
        }
        CardMetric("BATTLE_TRANCE", false, "draw", 3, x => x.Metrics.Draw);
        CardMetric("BATTLE_TRANCE", true, "draw", 4, x => x.Metrics.Draw);
        CardMetric("BODY_SLAM", false, "cost", 1, x => decimal.TryParse(x.Cost, out var v) ? v : null);
        CardMetric("BODY_SLAM", true, "cost", 0, x => decimal.TryParse(x.Cost, out var v) ? v : null);
        CardMetric("CORRUPTION", false, "cost", 3, x => decimal.TryParse(x.Cost, out var v) ? v : null);
        CardMetric("CORRUPTION", true, "cost", 2, x => decimal.TryParse(x.Cost, out var v) ? v : null);
        CardMetric("OFFERING", false, "draw", 3, x => x.Metrics.Draw);
        CardMetric("OFFERING", true, "draw", 5, x => x.Metrics.Draw);
        tests.Add(new("golden Ashen Strike curve", curves.Any(x => x.CardId == "ASHEN_STRIKE" && x.Upgraded && x.Input == 3 && x.Output == 18), "18", curves.FirstOrDefault(x => x.CardId == "ASHEN_STRIKE" && x.Upgraded && x.Input == 3)?.Output.ToString() ?? "missing", "6+4×3"));
        tests.Add(new("golden Fiend Fire curve", curves.Any(x => x.CardId == "FIEND_FIRE" && x.Upgraded && x.Input == 6 && x.Output == 60), "60", curves.FirstOrDefault(x => x.CardId == "FIEND_FIRE" && x.Upgraded && x.Input == 6)?.Output.ToString() ?? "missing", "10×6"));
        tests.Add(new("golden Whirlwind curve", curves.Any(x => x.CardId == "WHIRLWIND" && x.Upgraded && x.Input == 3 && x.SecondaryInput == 3 && x.Output == 72), "72", curves.FirstOrDefault(x => x.CardId == "WHIRLWIND" && x.Upgraded && x.Input == 3 && x.SecondaryInput == 3)?.Output.ToString() ?? "missing", "8×3×3"));

        foreach (var act in new[] { "Overgrowth", "Underdocks", "Hive", "Glory" })
        {
            var actRows = maps.Where(x => x.Act == act).ToList();
            tests.Add(new($"map samples {act}", actRows.Count == 24 && actRows.All(x => x.Samples > 0), "24 populated metrics", $"{actRows.Count} metrics", "6 房型×4 统计总体"));
            var eliteMax = actRows.SingleOrDefault(x => x.Population == "AllNodes" && x.PointType == "Elite")?.Maximum ?? 0;
            tests.Add(new($"map elite quota {act}", eliteMax >= 6, ">=6", eliteMax.ToString(), "A10 地图精英目标由源码逐图生成"));
        }
        foreach (var source in new[] { "Regular", "Elite", "Boss", "Shop", "RepresentativeRunStream" })
        {
            var sum = rewards.Cards.Where(x => x.Source == source).Sum(x => x.Probability);
            tests.Add(new($"reward probability sum {source}", Math.Abs(sum - 1) <= 0.000001m, "1", sum.ToString(), "三稀有度概率和"));
        }
        return tests;
    }

    private static IReadOnlyList<SourceConflict> BuildConflicts(string sourceRoot)
    {
        var facts = Path.GetFullPath(Path.Combine(sourceRoot, "..", "Neuvillette", "Neuvillette", "docs", "game-facts.md"));
        return
        [
            new("BASELINE_VERSION", "game-facts.md 标注 v0.110.1", "code 目录无正式版本标识", "采用 source_manifest.json 指纹锁定当前源码快照", EvidenceLevel.Exact, File.Exists(facts) ? facts : "docs/game-facts.md"),
            new("SOURCE_PRECEDENCE", "流程文档作为索引", "反编译源码作为数值真值", "发生冲突时采用源码并在此表登记", EvidenceLevel.Exact, File.Exists(facts) ? facts : "docs/game-facts.md"),
            new("LEGACY_RANKING", "旧草稿使用固定格挡/抽牌/稀有度折算", "v2 保持原生单位与条件边际值", "旧草稿保留但不作为 v2 证据", EvidenceLevel.Interpretive, "docs/IRONCLAD_CARD_VALUE_RANKING.md")
        ];
    }

    private static object[] ExternalReferences(string sourceRoot)
    {
        var facts = Path.GetFullPath(Path.Combine(sourceRoot, "..", "Neuvillette", "Neuvillette", "docs", "game-facts.md"));
        if (!File.Exists(facts)) return [];
        var info = new FileInfo(facts);
        return [new { path = facts, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(facts))).ToLowerInvariant(), lastWriteTimeUtc = info.LastWriteTimeUtc }];
    }

    private static void WriteCsv<T>(string directory, string name, IEnumerable<T> records) =>
        CsvWriter.Write(Path.Combine(directory, name), records);

    private static void WriteJson<T>(string path, T value)
    {
        var settings = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(value, settings) + Environment.NewLine, new UTF8Encoding(true));
    }

    private static void PrintUsage() => Console.WriteLine(
        "dotnet run --project analysis/Sts2CharacterModel -- build --source <game-code> --character <Ironclad|Neuvillette> --ascension 10 --seed 20260814 --out <dir> [--mod-source <NeuvilletteMod>] [--act4 true] [--sponsor-relics false] [--map-samples 100000] [--reward-samples 100000]");

    private sealed record BuildOptions(
        string Source, string? ModSource, string Character, int Ascension, ulong Seed, string Output,
        int MapSamples, int RewardSamples, bool Act4Enabled, bool SponsorRelicsEnabled)
    {
        public static BuildOptions? Parse(string[] args)
        {
            if (args.Length == 0 || !args[0].Equals("build", StringComparison.OrdinalIgnoreCase)) return null;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i + 1 < args.Length; i += 2) values[args[i]] = args[i + 1];
            if (!values.TryGetValue("--source", out var source) || !values.TryGetValue("--out", out var output)) return null;
            return new BuildOptions(
                Path.GetFullPath(source),
                values.TryGetValue("--mod-source", out var modSource) ? Path.GetFullPath(modSource) : null,
                values.GetValueOrDefault("--character", "Ironclad"),
                int.Parse(values.GetValueOrDefault("--ascension", "10")),
                ulong.Parse(values.GetValueOrDefault("--seed", "20260814")), Path.GetFullPath(output),
                int.Parse(values.GetValueOrDefault("--map-samples", "100000")),
                int.Parse(values.GetValueOrDefault("--reward-samples", "100000")),
                bool.Parse(values.GetValueOrDefault("--act4", "true")),
                bool.Parse(values.GetValueOrDefault("--sponsor-relics", "false")));
        }
    }
}
