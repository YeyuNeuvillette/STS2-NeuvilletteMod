using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Sts2CharacterModel;

internal static class NeuvilletteModelBuilder
{
    private const string ToolVersion = "2.1.0";

    public static int Build(
        string gameSource, string modSource, string output, int ascension, ulong seed,
        int mapSamples, int rewardSamples, bool act4Enabled, bool sponsorRelicsEnabled)
    {
        if (ascension != 10) throw new ArgumentException("Neuvillette v2 基准固定为 A10。", nameof(ascension));
        if (!Directory.Exists(gameSource)) throw new DirectoryNotFoundException(gameSource);
        if (!Directory.Exists(modSource)) throw new DirectoryNotFoundException(modSource);
        Directory.CreateDirectory(output);

        Console.WriteLine("[1/6] 扫描 Neuvillette 主牌池、衍生牌、遗物、药水与角色资源…");
        var modTracker = new SourceTracker(modSource);
        var scanner = new NeuvilletteSourceScanner(modSource, modTracker, sponsorRelicsEnabled);
        var cards = scanner.ScanCards("NeuvilletteCardPool");
        var generatedCards = scanner.ScanGeneratedCards();
        var relics = scanner.ScanRelics("NeuvilletteRelicPool", "Character");
        var storyRelics = scanner.ScanStoryRelics();
        var potions = scanner.ScanPotions();
        var curves = scanner.BuildDynamicCurves();
        var resourceCurves = scanner.BuildResourceCurves();
        var archetypes = BuildArchetypeMetrics(cards);

        Console.WriteLine("[2/6] 审计前三幕敌方压力与第四幕吞星之鲸状态机…");
        var gameTracker = new SourceTracker(gameSource);
        var gameScanner = new SourceScanner(gameSource, gameTracker);
        var sharedRelics = gameScanner.ScanSharedRelics();
        var sharedPotions = gameScanner.ScanSharedPotions();
        var ancientOfferings = gameScanner.ScanAncients();
        var gameEncounters = new EncounterScanner(gameSource, gameTracker).Scan();
        var critical = gameEncounters.Critical.Concat([BuildNarwhalEncounter()]).ToList();

        Console.WriteLine($"[3/6] 复现标准地图：每幕 {mapSamples:N0} 张，并固化第四幕线性地图…");
        var maps = new MapSimulator().Run(mapSamples, seed);
        var act4Map = BuildAct4Map();
        var routeStages = BuildAct4Route();

        Console.WriteLine($"[4/6] 复现 A10 奖励保底与未知节点：{rewardSamples:N0} 个样本流…");
        var rewards = RewardSimulator.Run(rewardSamples, seed);
        var unknowns = UnknownResolutionSimulator.Run(rewardSamples, seed);

        var baseline = new CharacterSpec(
            "NEUVILLETTE", 10, 50, 40, 99, 3, 5, 10, 2,
            ["STRIKE_NEUVILLETTE", "STRIKE_NEUVILLETTE", "STRIKE_NEUVILLETTE", "STRIKE_NEUVILLETTE",
             "DEFEND_NEUVILLETTE", "DEFEND_NEUVILLETTE", "DEFEND_NEUVILLETTE", "DEFEND_NEUVILLETTE",
             "EQUITABLE_JUDGMENT", "TIDE", "ASCENDERS_BANE"],
            ["AS_WATER_SEEKS_EQUILIBRIUM"], EvidenceLevel.Derived,
            "基础最大生命 50；A2 先古恢复率 80%，故 A10 首战 40；A4 为 2 药水栏，A5 加入灾厄。第四幕开启但需在第三幕结束前锻成水仙十字之剑；赞助者遗物关闭。"
        );

        var validations = Validate(cards, generatedCards, relics, storyRelics, potions, curves,
            resourceCurves, critical, act4Map, routeStages, act4Enabled, sponsorRelicsEnabled);
        validations.Add(new("base shared relic scan", sharedRelics.Count == 118, "118", sharedRelics.Count.ToString(), "全池扫描；相关项由 Included 标记进入详细模型"));
        validations.Add(new("base shared potion scan", sharedPotions.Count == 45, "45", sharedPotions.Count.ToString(), "全池扫描；相关项由 Included 标记进入详细模型"));
        validations.Add(new("base ancients", ancientOfferings.Select(x => x.Ancient).Distinct().Count() == 8, "8", ancientOfferings.Select(x => x.Ancient).Distinct().Count().ToString(), "原版八名先古全部供物"));
        var counts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["cardStates"] = cards.Count,
            ["cardIdentities"] = cards.Select(x => x.Id).Distinct().Count(),
            ["generatedCardStates"] = generatedCards.Count,
            ["generatedCardIdentities"] = generatedCards.Select(x => x.Id).Distinct().Count(),
            ["characterRelics"] = relics.Count,
            ["storyRelics"] = storyRelics.Count,
            ["characterPotions"] = potions.Count,
            ["sharedRelicsScanned"] = sharedRelics.Count,
            ["sharedRelicsIncluded"] = sharedRelics.Count(x => x.Included),
            ["sharedPotionsScanned"] = sharedPotions.Count,
            ["sharedPotionsIncluded"] = sharedPotions.Count(x => x.Included),
            ["baseAncients"] = ancientOfferings.Select(x => x.Ancient).Distinct().Count(),
            ["baseAncientOfferings"] = ancientOfferings.Count,
            ["melusineStickers"] = generatedCards.Select(x => x.Id).Distinct().Count(id => id.EndsWith("_STICKER", StringComparison.Ordinal)),
            ["baseGameBosses"] = gameEncounters.Critical.Count(x => x.Category == "Boss"),
            ["baseGameElites"] = gameEncounters.Critical.Count(x => x.Category == "Elite"),
            ["act4Bosses"] = 1,
            ["unresolvedCardStates"] = cards.Count(x => x.UnresolvedComponents.Length > 0)
        };
        var aggregates = new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["baseHp"] = 50,
            ["a10OpeningHp"] = 40,
            ["startingDeckSize"] = 11,
            ["sourcewaterCap"] = 6,
            ["surgeSettlementFloorFraction"] = 0.5m,
            ["oratriceThreshold"] = 100,
            ["act4NarwhalHpA10"] = 1444,
            ["act4ForcedBarrierDamage"] = 320,
            ["act4NominalEffectiveDamageRequirement"] = 1764,
            ["wishGoldCost"] = 100,
            ["mapCount"] = mapSamples * 4,
            ["regularRewardRareProbability"] = rewards.Cards.First(x => x.Source == "Regular" && x.Rarity == "Rare").Probability
        };
        var fingerprint = CombinedFingerprint(gameTracker.Fingerprint(), modTracker.Fingerprint(), act4Enabled, sponsorRelicsEnabled);
        var generatedAt = modTracker.References.Concat(gameTracker.References).Select(x => x.LastWriteTimeUtc).DefaultIfEmpty(DateTime.UnixEpoch).Max();
        var summary = new BuildSummary
        {
            ToolVersion = ToolVersion,
            Character = "Neuvillette",
            Ascension = ascension,
            Seed = seed,
            MapSamplesPerAct = mapSamples,
            RewardSamples = rewardSamples,
            Baseline = baseline,
            Counts = counts,
            Aggregates = aggregates,
            Validations = validations,
            SourceFingerprint = fingerprint,
            GeneratedAtUtc = generatedAt
        };

        Console.WriteLine("[5/6] 写入 CSV/JSON 复算附件…");
        WriteCsv(output, "cards.csv", cards);
        WriteCsv(output, "cards_generated.csv", generatedCards);
        WriteCsv(output, "relics_character.csv", relics);
        WriteCsv(output, "relics_story_and_sponsor.csv", storyRelics);
        WriteCsv(output, "potions_character.csv", potions);
        WriteCsv(output, "relics_shared_scan.csv", sharedRelics);
        WriteCsv(output, "potions_shared_scan.csv", sharedPotions);
        WriteCsv(output, "ancient_offerings.csv", ancientOfferings);
        WriteCsv(output, "dynamic_curves.csv", curves);
        WriteCsv(output, "resource_curves.csv", resourceCurves);
        WriteCsv(output, "archetype_metrics.csv", archetypes);
        WriteCsv(output, "encounters_critical.csv", critical);
        WriteCsv(output, "encounters_normal_frontier.csv", gameEncounters.Frontier);
        WriteCsv(output, "boss_pairs_a10.csv", gameEncounters.BossPairs);
        WriteCsv(output, "map_metrics.csv", maps);
        WriteCsv(output, "act4_map.csv", act4Map);
        WriteCsv(output, "act4_route_gate.csv", routeStages);
        WriteCsv(output, "card_reward_metrics.csv", rewards.Cards);
        WriteCsv(output, "potion_reward_metrics.csv", rewards.Potions);
        WriteCsv(output, "unknown_resolution_metrics.csv", unknowns);
        WriteCsv(output, "validation.csv", validations);
        WriteCsv(output, "source_conflicts.csv", BuildConflicts());
        WriteJson(Path.Combine(output, "baseline.json"), baseline);
        WriteJson(Path.Combine(output, "configuration.json"), new
        {
            character = "Neuvillette", ascension, seed, act4Enabled, sponsorRelicsEnabled,
            multiplayer = false, allContentUnlocked = true,
            gameSource = Path.GetFullPath(gameSource), modSource = Path.GetFullPath(modSource)
        });
        WriteJson(Path.Combine(output, "source_manifest.json"), new
        {
            gameSourceRoot = Path.GetFullPath(gameSource), modSourceRoot = Path.GetFullPath(modSource), fingerprint,
            gameFiles = gameTracker.References.OrderBy(x => x.RelativePath, StringComparer.Ordinal).ToArray(),
            modFiles = modTracker.References.OrderBy(x => x.RelativePath, StringComparer.Ordinal).ToArray()
        });
        WriteJson(Path.Combine(output, "summary.json"), summary);

        Console.WriteLine("[6/6] 执行清单、资源边界与第四幕金样验收…");
        foreach (var failed in validations.Where(x => !x.Passed))
            Console.Error.WriteLine($"FAIL {failed.Test}: expected={failed.Expected}; actual={failed.Actual}; {failed.Details}");
        Console.WriteLine($"源码指纹: {fingerprint}");
        Console.WriteLine($"验收: {validations.Count(x => x.Passed)}/{validations.Count}；输出: {Path.GetFullPath(output)}");
        return summary.IsValid ? 0 : 3;
    }

    private static IReadOnlyList<ArchetypeMetric> BuildArchetypeMetrics(IReadOnlyList<CardSpec> cards)
    {
        var tags = new[] { "潮涌", "源水之滴", "呈堂", "美露莘", "自伤", "衡平推裁", "防御", "高费" };
        return tags.Select(tag =>
        {
            var rows = cards.Where(x => x.Archetypes.Split('|').Contains(tag, StringComparer.Ordinal)).ToList();
            decimal Avg(Func<CardSpec, decimal?> f) => rows.Count == 0 ? 0m : rows.Average(x => f(x) ?? 0m);
            return new ArchetypeMetric(tag, rows.Count, rows.Select(x => x.Id).Distinct().Count(),
                Avg(x => x.Metrics.Damage), Avg(x => x.Metrics.Block), Avg(x => x.Metrics.SelfHpCost),
                Avg(x => x.Metrics.Draw), Avg(x => x.Metrics.Energy),
                rows.Count == 0 ? 0m : rows.Average(x => decimal.TryParse(x.Cost, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : 0m),
                rows.Count == 0 ? 0m : (decimal)rows.Count(x => x.UnresolvedComponents.Length > 0) / rows.Count,
                tag switch { "潮涌" => "生命/潮落债务", "源水之滴" => "0..6 源水", "呈堂" => "谕示值/消耗堆", "美露莘" => "13 张无放回贴纸池", _ => "手牌/能量/生命" },
                tag switch { "潮涌" => "战后债务结算或满血溢出", "源水之滴" => "6 层上限与生成时点", "呈堂" => "无合法目标或关键牌不愿消耗", "美露莘" => "随机池与手牌空间", _ => "条件不成立/资源不足" },
                EvidenceLevel.Derived);
        }).ToList();
    }

    private static EncounterSpec BuildNarwhalEncounter() => new(
        "NeuvilletteAct", "AllDevouringNarwhal", "Act4Boss", "ALL_DEVOURING_NARWHAL",
        1444, 1444, 22, 77, 132, 222, 40, 2, true, true, true,
        "1444HP|75%/25%强制腹中阶段|两次160持久格挡|裂隙每回合末-5最大生命并为Boss+5最大生命|渴求随机污染3张手牌|吞噬削减攻击牌25%未格挡伤害并转为本回合敌伤|玩家破腹后满血且清除减益",
        "T1..T8 为不触发血线的第一阶段名义序列：22,25,15x2,25,15x2,20(+5力量),30,20x2。实际状态机在 75% 与25%血线立即结束玩家回合并进入腹中，故死亡率/击杀回合必须条件模拟。",
        EvidenceLevel.Derived,
        "NeuvilletteCode/Monsters/AllDevouringNarwhal.cs|NeuvilletteCode/Monsters/Powers/BeastOfStarsPower.cs|NeuvilletteCode/Monsters/Powers/DevourPower.cs|NeuvilletteCode/Monsters/Cards/RiftCard.cs");

    private static IReadOnlyList<Act4MapPointSpec> BuildAct4Map() =>
    [
        new(0, "Ancient", 0, 0, false, "建筑师：按前三幕 Boss 各给一个对应遗物选项，三选一。", EvidenceLevel.Exact, "NeuvilletteCode/Features/Act4/Act4MapService.cs", 23),
        new(1, "RestSite", 3, 1, false, "休息/升级；持 Persona 时也有冥想/锻剑选项。", EvidenceLevel.Exact, "NeuvilletteCode/Features/Act4/Act4MapService.cs", 18),
        new(2, "Treasure", 3, 2, false, "第三幕资源外观的宝箱房。", EvidenceLevel.Exact, "NeuvilletteCode/Features/Act4/Act4MapService.cs", 19),
        new(3, "Shop", 3, 3, false, "最终商店；若已有人格面具但未买愿望，固定追加 100 金愿望。", EvidenceLevel.Exact, "NeuvilletteCode/Features/Act4/Act4MapService.cs", 20),
        new(4, "RestSite", 3, 4, false, "Boss 前最后整备。", EvidenceLevel.Exact, "NeuvilletteCode/Features/Act4/Act4MapService.cs", 21),
        new(5, "Boss", 0, 6, false, "吞星之鲸。", EvidenceLevel.Exact, "NeuvilletteCode/Features/Act4/Act4MapService.cs", 24)
    ];

    private static IReadOnlyList<Act4RouteStage> BuildAct4Route() =>
    [
        new(1, "四方之地", "前三幕被固定标记的可达未知点；玩家选择拿取。", "Persona；每场战斗 T1 +1 能量，并标记一名未来精英。", "占用1个未知节点，且必须主动改道/选择拿取。", true, EvidenceLevel.Exact, "NeuvilletteCode/Features/Map/FourQuadrantsMarkerService.cs", 10),
        new(2, "人格精英", "击败 Persona 随机标记且仍可达的精英。", "Soul；战斗开始 +1 力量。", "精英被随机强化为+2力量、2人工制品或7多层护甲之一；承担精英战与路线风险。", true, EvidenceLevel.Exact, "NeuvilletteCode/Characters/Neuvillette/Relics/Persona.cs", 213),
        new(3, "冥想", "持 Persona 且未有 Memory 时，在休息处选择冥想并变换1张牌。", "Memory；战斗开始 +1 敏捷。", "放弃休息/升级并承担一次随机变换方差。", true, EvidenceLevel.Exact, "NeuvilletteCode/Characters/Neuvillette/Relics/MeditateRestSiteOption.cs", 31),
        new(4, "购买愿望", "持 Persona 后到商店支付100金。", "Wish；每场战斗 T1 多抽1。", "至少100金及一个商店访问；固定槽不进入常规遗物池。", true, EvidenceLevel.Exact, "NeuvilletteCode/Features/Shop/WishShopService.cs", 13),
        new(5, "锻剑", "同时持 Persona/Soul/Memory/Wish，在休息处选择锻剑。", "移除四件任务遗物，获得 NarzissenkreuzSword：战斗开始+1力量/+1敏捷，T1+1能量/+1抽牌。", "再放弃一次休息/升级；资源效果等价合并，但不可再使用 Persona 地图能力。", true, EvidenceLevel.Exact, "NeuvilletteCode/Characters/Neuvillette/Relics/ForgeSwordRestSiteOption.cs", 32),
        new(6, "进入第四幕", "第三幕结束时所有玩家均持 NarzissenkreuzSword。", "保留 NeuvilletteAct；否则从 Acts 列表移除。", "单人即玩家本人必须完成全部链条。", true, EvidenceLevel.Exact, "NeuvilletteCode/Features/Act4/Act4CompatibilityService.cs", 22)
    ];

    private static List<ValidationResult> Validate(
        IReadOnlyList<CardSpec> cards, IReadOnlyList<CardSpec> generatedCards,
        IReadOnlyList<RelicSpec> relics, IReadOnlyList<RelicSpec> storyRelics, IReadOnlyList<PotionSpec> potions,
        IReadOnlyList<DynamicCurvePoint> curves, IReadOnlyList<ResourceCurvePoint> resourceCurves,
        IReadOnlyList<EncounterSpec> encounters, IReadOnlyList<Act4MapPointSpec> act4Map,
        IReadOnlyList<Act4RouteStage> route, bool act4, bool sponsor)
    {
        var tests = new List<ValidationResult>();
        void Count(string test, int expected, int actual, string note = "精确清单计数") => tests.Add(new(test, expected == actual, expected.ToString(), actual.ToString(), note));
        Count("Neuvillette card identities", 88, cards.Select(x => x.Id).Distinct().Count());
        Count("Neuvillette card base+upgrade states", 176, cards.Count);
        Count("generated card identities", 18, generatedCards.Select(x => x.Id).Distinct().Count(), "13贴纸+4Token+1事件牌");
        Count("Melusine stickers", 13, generatedCards.Select(x => x.Id).Distinct().Count(x => x.EndsWith("_STICKER", StringComparison.Ordinal)));
        Count("character relics", 28, relics.Count);
        Count("story and sponsor relics", 6, storyRelics.Count);
        Count("character potions", 3, potions.Count);
        Count("Act4 linear points", 6, act4Map.Count);
        Count("Act4 route gates", 6, route.Count);
        tests.Add(new("Act4 enabled", act4, "true", act4.ToString().ToLowerInvariant(), "用户基准"));
        tests.Add(new("sponsor relics disabled", !sponsor, "false", sponsor.ToString().ToLowerInvariant(), "用户基准"));
        var catCake = storyRelics.SingleOrDefault(x => x.ClassName == "CatCake");
        tests.Add(new("CatCake excluded", catCake is { Included: false }, "excluded", catCake is null ? "missing" : catCake.Included ? "included" : "excluded", "SponsorRelicEnabled 唯一调用点"));
        void CardGolden(string id, bool upgraded, decimal? damage, decimal? block, string cost)
        {
            var row = cards.SingleOrDefault(x => x.Id == id && x.Upgraded == upgraded);
            var pass = row != null && row.Metrics.Damage == damage && row.Metrics.Block == block && row.Cost == cost;
            tests.Add(new($"golden {id}{(upgraded ? "+" : "")}", pass, $"d={damage};b={block};c={cost}", row == null ? "missing" : $"d={row.Metrics.Damage};b={row.Metrics.Block};c={row.Cost}", "源码静态金样"));
        }
        CardGolden("STRIKE_NEUVILLETTE", false, 6, null, "1");
        CardGolden("STRIKE_NEUVILLETTE", true, 9, null, "1");
        CardGolden("DEFEND_NEUVILLETTE", false, null, 5, "1");
        CardGolden("DEFEND_NEUVILLETTE", true, null, 8, "1");
        CardGolden("TIDE", false, null, 4, "1");
        CardGolden("TIDE", true, null, 6, "1");
        tests.Add(new("golden Surging Torrent 6 droplets", curves.Any(x => x.CardId == "SURGING_TORRENT" && !x.Upgraded && x.Input == 6 && x.Output == 37), "37", curves.FirstOrDefault(x => x.CardId == "SURGING_TORRENT" && !x.Upgraded && x.Input == 6)?.Output.ToString() ?? "missing", "7+5×6"));
        tests.Add(new("golden Equitable Judgment+ glory3", curves.Any(x => x.CardId == "EQUITABLE_JUDGMENT" && x.Upgraded && x.Variable == "PastDraconicGlories" && x.Input == 3 && x.Output == 80), "80", curves.FirstOrDefault(x => x.CardId == "EQUITABLE_JUDGMENT" && x.Upgraded && x.Variable == "PastDraconicGlories" && x.Input == 3)?.Output.ToString() ?? "missing", "20+20×3"));
        tests.Add(new("golden Submit cost3", resourceCurves.Any(x => x.Mechanic == "SubmitOratrice" && x.Input == 3 && x.SecondaryInput == 0 && x.Output == 40), "40", resourceCurves.FirstOrDefault(x => x.Mechanic == "SubmitOratrice" && x.Input == 3 && x.SecondaryInput == 0)?.Output.ToString() ?? "missing", "10+10×cost"));
        tests.Add(new("golden Surge settlement floor", resourceCurves.Any(x => x.Mechanic == "SurgeSettlement" && x.Input == 30 && x.SecondaryInput == 20 && x.Output == 25), "25", resourceCurves.FirstOrDefault(x => x.Mechanic == "SurgeSettlement" && x.Input == 30 && x.SecondaryInput == 20)?.Output.ToString() ?? "missing", "max(30-20,50×50%)"));
        tests.Add(new("golden Surge below-floor no heal", resourceCurves.Any(x => x.Mechanic == "SurgeSettlement" && x.Input == 20 && x.SecondaryInput == 20 && x.Output == 20), "20", resourceCurves.FirstOrDefault(x => x.Mechanic == "SurgeSettlement" && x.Input == 20 && x.SecondaryInput == 20)?.Output.ToString() ?? "missing", "债务结算不治疗"));
        var whale = encounters.SingleOrDefault(x => x.EncounterClass == "AllDevouringNarwhal");
        tests.Add(new("golden Narwhal A10 HP", whale?.MaxHpA10 == 1444, "1444", whale?.MaxHpA10.ToString() ?? "missing", "ToughEnemies 分支"));
        tests.Add(new("golden Narwhal T1/T3", whale?.T1Incoming == 22 && whale.T3Incoming == 77, "22/77", whale == null ? "missing" : $"{whale.T1Incoming}/{whale.T3Incoming}", "未触发血线的名义序列"));
        return tests;
    }

    private static IReadOnlyList<SourceConflict> BuildConflicts() =>
    [
        new("ACT4_SWITCH", "“默认开启第四层”可能被理解为必进第四幕", "开启只把第四幕加入 Acts；第三幕结束时若未持剑会移除", "报告开关开启与任务链实际可达为两个变量", EvidenceLevel.Exact, "NeuvilletteCode/Features/Act4/Act4CompatibilityService.cs"),
        new("SPONSOR_SWITCH", "Mod 设置默认值为 true", "用户基准指定不启用；唯一受控内容为 CatCake", "CatCake 在共享池扫描中保留但 Included=false", EvidenceLevel.Exact, "NeuvilletteCode/Characters/Neuvillette/Relics/CatCake.cs"),
        new("COMBAT_OUTCOME", "静态卡面可直接汇成唯一总分", "潮涌债务、随机贴纸、敌方意图与鲸鱼阶段造成强状态依赖", "只给原生维度、响应曲线和条件结论；未解析项不按0", EvidenceLevel.Interpretive, "docs/NEUVILLETTE_MODEL_METHODOLOGY_V2.md")
    ];

    private static string CombinedFingerprint(string game, string mod, bool act4, bool sponsor)
    {
        var bytes = Encoding.UTF8.GetBytes($"{game}\n{mod}\nact4={act4}\nsponsor={sponsor}");
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static void WriteCsv<T>(string directory, string name, IEnumerable<T> rows) => CsvWriter.Write(Path.Combine(directory, name), rows);
    private static void WriteJson<T>(string path, T value)
    {
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };
        File.WriteAllText(path, JsonSerializer.Serialize(value, options) + Environment.NewLine, new UTF8Encoding(true));
    }
}

internal sealed class NeuvilletteSourceScanner
{
    private static readonly string[] CoreEvidenceFiles =
    [
        "docs/game-facts.md",
        "NeuvilletteCode/NeuvilletteSettings.cs",
        "NeuvilletteCode/Characters/Neuvillette/Neuvillette.cs",
        "NeuvilletteCode/Characters/Neuvillette/Act/NeuvilletteAct.cs",
        "NeuvilletteCode/Characters/Neuvillette/Ancients/ArchitectAncient.cs",
        "NeuvilletteCode/Characters/Neuvillette/Patches/NeuvilletteActPatch.cs",
        "NeuvilletteCode/Characters/Neuvillette/Patches/FourQuadrantsLandPatch.cs",
        "NeuvilletteCode/Characters/Neuvillette/Relics/ForgeSwordRestSiteOption.cs",
        "NeuvilletteCode/Characters/Neuvillette/Relics/MeditateRestSiteOption.cs",
        "NeuvilletteCode/Features/Act4/Act4CompatibilityService.cs",
        "NeuvilletteCode/Features/Act4/Act4MapService.cs",
        "NeuvilletteCode/Features/Act4/Act4RewardService.cs",
        "NeuvilletteCode/Features/Act4/Act4RoomService.cs",
        "NeuvilletteCode/Features/Map/FourQuadrantsMarkerService.cs",
        "NeuvilletteCode/Features/Map/MapRouteService.cs",
        "NeuvilletteCode/Features/Map/PersonaEliteMarkerService.cs",
        "NeuvilletteCode/Features/Shop/WishShopService.cs",
        "NeuvilletteCode/Characters/Neuvillette/Powers/SourcewaterDroplet.cs",
        "NeuvilletteCode/Characters/Neuvillette/Powers/SurgePower.cs",
        "NeuvilletteCode/Characters/Neuvillette/Powers/OratricePower.cs",
        "NeuvilletteCode/Characters/Neuvillette/Powers/ContemptOfCourtPower.cs",
        "NeuvilletteCode/Characters/Neuvillette/Powers/LivingWaterPower.cs",
        "NeuvilletteCode/Characters/Neuvillette/Powers/LeviathanFormPower.cs",
        "NeuvilletteCode/Characters/Neuvillette/Powers/ProceduralJusticePower.cs",
        "NeuvilletteCode/Characters/Neuvillette/Powers/PastDraconicGloriesPower.cs",
        "NeuvilletteCode/Monsters/AllDevouringNarwhal.cs",
        "NeuvilletteCode/Monsters/NarwhalBossEncounter.cs",
        "NeuvilletteCode/Monsters/Afflictions/CravingAffliction.cs",
        "NeuvilletteCode/Monsters/Cards/RiftCard.cs",
        "NeuvilletteCode/Monsters/Powers/AppetitePower.cs",
        "NeuvilletteCode/Monsters/Powers/BeastOfStarsPower.cs",
        "NeuvilletteCode/Monsters/Powers/DevourPower.cs",
        "NeuvilletteCode/Monsters/Powers/HostilityPower.cs",
        "NeuvilletteCode/Monsters/Powers/PhantomPower.cs"
    ];
    private static readonly Regex ClassRegex = new(@"public\s+sealed\s+class\s+(?<name>\w+)\s*\(\s*\)\s*:\s*(?<base>\w+)\s*\(\s*(?<cost>-?\d+)\s*,\s*CardType\.(?<type>\w+)\s*,\s*CardRarity\.(?<rarity>\w+)\s*,\s*TargetType\.(?<target>\w+)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex VarRegex = new(@"new\s+(?<kind>DamageVar|BlockVar|CardsVar|EnergyVar|HpLossVar|MaxHpVar|RepeatVar|ExtraDamageVar|CalculationBaseVar)\s*\(\s*(?:""(?<explicit>[^""]+)""\s*,\s*)?(?<value>-?\d+(?:\.\d+)?)m?", RegexOptions.Compiled);
    private static readonly Regex NamedVarRegex = new(@"new\s+DynamicVar\s*\(\s*""(?<name>[^""]+)""\s*,\s*(?<value>-?\d+(?:\.\d+)?)m?", RegexOptions.Compiled);
    private static readonly Regex PowerVarRegex = new(@"new\s+PowerVar<(?<name>\w+)>\s*\(\s*(?<value>-?\d+(?:\.\d+)?)m?", RegexOptions.Compiled);
    private readonly string _root;
    private readonly SourceTracker _tracker;
    private readonly bool _sponsorRelics;
    private readonly Dictionary<string, string> _cardLoc;
    private readonly Dictionary<string, string> _relicLoc;
    private readonly Dictionary<string, string> _potionLoc;

    public NeuvilletteSourceScanner(string root, SourceTracker tracker, bool sponsorRelics)
    {
        _root = Path.GetFullPath(root);
        _tracker = tracker;
        _sponsorRelics = sponsorRelics;
        _cardLoc = LoadLoc("Neuvillette/localization/zhs/cards.json");
        _relicLoc = LoadLoc("Neuvillette/localization/zhs/relics.json");
        _potionLoc = LoadLoc("Neuvillette/localization/zhs/potions.json");
        foreach (var relative in CoreEvidenceFiles)
            if (File.Exists(Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar))))
                _tracker.Read(relative);
    }

    public IReadOnlyList<CardSpec> ScanCards(string pool)
    {
        var files = RegisteredFiles("Cards", $"[RegisterCard(typeof({pool}))]");
        var rows = new List<CardSpec>(files.Count * 2);
        foreach (var relative in files)
        {
            var source = _tracker.Read(relative);
            rows.Add(BuildCard(relative, source, false, pool));
            rows.Add(BuildCard(relative, source, true, pool));
        }
        return rows.OrderBy(x => x.Id, StringComparer.Ordinal).ThenBy(x => x.Upgraded).ToList();
    }

    public IReadOnlyList<CardSpec> ScanGeneratedCards()
    {
        var pools = new[] { "MelusineCardPool", "TokenCardPool", "EventCardPool" };
        return pools.SelectMany(ScanCards).OrderBy(x => x.Id, StringComparer.Ordinal).ThenBy(x => x.Upgraded).ToList();
    }

    public IReadOnlyList<RelicSpec> ScanRelics(string pool, string scope) =>
        RegisteredFiles("Relics", $"[RegisterRelic(typeof({pool}))]")
            .Select(relative => BuildRelic(relative, scope)).OrderBy(x => x.Id, StringComparer.Ordinal).ToList();

    public IReadOnlyList<RelicSpec> ScanStoryRelics()
    {
        var names = new HashSet<string>(["CatCake", "Memory", "NarzissenkreuzSword", "Persona", "Soul", "Wish"], StringComparer.Ordinal);
        return RegisteredFiles("Relics", "[RegisterRelic(typeof(SharedRelicPool))]")
            .Where(x => names.Contains(Path.GetFileNameWithoutExtension(x)))
            .Select(relative => BuildRelic(relative, Path.GetFileNameWithoutExtension(relative) == "CatCake" ? "SponsorShared" : "Act4Quest"))
            .OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<PotionSpec> ScanPotions() => RegisteredFiles("Potions", "[RegisterPotion(typeof(NeuvillettePotionPool))]")
        .Select(BuildPotion).OrderBy(x => x.Id, StringComparer.Ordinal).ToList();

    public IReadOnlyList<DynamicCurvePoint> BuildDynamicCurves()
    {
        var rows = new List<DynamicCurvePoint>();
        for (var u = 0; u <= 1; u++)
        {
            for (var droplets = 0; droplets <= 6; droplets++)
            {
                rows.Add(new("SURGING_TORRENT", u == 1, "SourcewaterDroplets", droplets, 0, (u == 1 ? 9 : 7) + (u == 1 ? 7 : 5) * droplets, "damage", u == 1 ? "9+7d" : "7+5d", EvidenceLevel.Exact));
                rows.Add(new("SOURCE_OF_LIFE", u == 1, "SourcewaterDroplets", droplets, 0, droplets, "energy", "d", EvidenceLevel.Exact));
                rows.Add(new("EQUITABLE_JUDGMENT", u == 1, "SourcewaterDroplets", droplets, 0, Math.Max(0, 3 - droplets), "energy_cost", "max(0,3-d)", EvidenceLevel.Exact));
            }
            for (var glories = 0; glories <= 5; glories++)
            {
                rows.Add(new("EQUITABLE_JUDGMENT", u == 1, "PastDraconicGlories", glories, 0, (u == 1 ? 20 : 15) * (1 + glories), "aoe_damage_per_enemy", u == 1 ? "20(1+g)" : "15(1+g)", EvidenceLevel.Exact));
                rows.Add(new("EQUITABLE_JUDGMENT", u == 1, "PastDraconicGlories", glories, 0, 2 * (1 + glories), "self_hp_loss", "2(1+g)", EvidenceLevel.Exact));
            }
            for (var living = 0; living <= 4; living++)
            {
                rows.Add(new("TIDE", u == 1, "LivingWater", living, 0, (2 + living) * (u == 1 ? 2 : 1), "temporary_heal_and_debt", u == 1 ? "2(2+L)" : "2+L", EvidenceLevel.Exact));
                rows.Add(new("RED_TIDE", u == 1, "LivingWater", living, 0, (u == 1 ? 7 : 4) + living, "temporary_heal_and_debt", u == 1 ? "7+L" : "4+L", EvidenceLevel.Exact));
            }
        }
        foreach (var maxHp in new[] { 40, 50, 60, 80 })
        {
            rows.Add(new("TSUNAMI", false, "MaxHp", maxHp, 0, Math.Floor(maxHp * 0.35m), "temporary_heal_and_debt", "floor(0.35M)", EvidenceLevel.Derived));
            rows.Add(new("TSUNAMI", true, "MaxHp", maxHp, 0, Math.Floor(maxHp * 0.50m), "temporary_heal_and_debt", "floor(0.50M)", EvidenceLevel.Derived));
        }
        foreach (var hp in new[] { 20, 40, 80, 200, 500, 1000 })
            rows.Add(new("FINAL_JUDGMENT", false, "BossCurrentHp", hp, 0, hp * 0.5m, "boss_hp_loss", "0.5H；非Boss直接消灭", EvidenceLevel.Exact));
        return rows;
    }

    public IReadOnlyList<ResourceCurvePoint> BuildResourceCurves()
    {
        var rows = new List<ResourceCurvePoint>();
        for (var cost = 0; cost <= 4; cost++)
            foreach (var procedural in new[] { 0, 5, 10 })
                rows.Add(new("SubmitOratrice", cost, procedural, NeuvilletteResourceAdapter.SubmitPoints(cost, procedural), "oratrice_points", "10+10c+p", "每100点生成1张最终裁判；被呈堂牌进入消耗堆", EvidenceLevel.Exact, "NeuvilletteCode/Characters/Neuvillette/Cards/SubmitCard.cs", 37));
        foreach (var current in new[] { 20, 25, 30, 40, 50 })
            foreach (var debt in new[] { 0, 5, 10, 20, 40 })
                rows.Add(new("SurgeSettlement", current, debt, NeuvilletteResourceAdapter.SettleSurgeDebt(current, 50, debt), "post_combat_hp", "min(H,max(H-D,0.5M))", "M=50；50%只限制债务扣减，不会把原本低于50%的生命抬高；潮涌债务按标称值累计", EvidenceLevel.Exact, "NeuvilletteCode/Characters/Neuvillette/Powers/SurgePower.cs", 31));
        for (var old = 0; old <= 6; old++)
            foreach (var delta in new[] { 0, 1, 2, 4 })
                rows.Add(new("SourcewaterCap", old, delta, Math.Min(6, old + delta), "droplets", "min(6,s+Δ)", "遗物按玩家回合内每次生命变化事件+1；无效满血治疗存在跳过保护", EvidenceLevel.Exact, "NeuvilletteCode/Characters/Neuvillette/Powers/SourcewaterDroplet.cs", 20));
        return rows;
    }

    private CardSpec BuildCard(string relative, string source, bool upgraded, string pool)
    {
        var match = ClassRegex.Match(source);
        var className = match.Success ? match.Groups["name"].Value : Path.GetFileNameWithoutExtension(relative);
        var id = ToId(className);
        var baseClass = match.Success ? match.Groups["base"].Value : "UnknownCard";
        var cost = match.Success ? match.Groups["cost"].Value : ExtractCtorCost(source);
        var type = match.Success ? match.Groups["type"].Value : ExtractEnum(source, "CardType") ?? "Unknown";
        var rarity = match.Success ? match.Groups["rarity"].Value : ExtractEnum(source, "CardRarity") ?? (pool == "MelusineCardPool" ? "Special" : "Unknown");
        var target = match.Success ? match.Groups["target"].Value : ExtractEnum(source, "TargetType") ?? "Unknown";
        var vars = ExtractVariables(source);
        if (baseClass == "SurgeCard" || source.Contains(": SurgeCard", StringComparison.Ordinal))
        {
            var surge = Regex.Match(source, @"BaseSurgeValue\s*=>\s*(?<n>-?\d+)");
            if (surge.Success) vars["Surge"] = decimal.Parse(surge.Groups["n"].Value, CultureInfo.InvariantCulture);
        }
        if (upgraded)
        {
            ApplyUpgrades(source, vars);
            var energy = Regex.Match(source, @"EnergyCost\.UpgradeBy\(\s*(?<n>-?\d+)");
            if (energy.Success && int.TryParse(cost, out var c)) cost = (c + int.Parse(energy.Groups["n"].Value, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
        }
        if (source.Contains("HasEnergyCostX => true", StringComparison.Ordinal)) cost = "X";

        var repeat = source.Contains("WithHitCount", StringComparison.Ordinal) ? Get(vars, "Repeat") ?? 1m : 1m;
        var damage = source.Contains("DamageCmd.Attack", StringComparison.Ordinal)
            ? (Get(vars, "Damage") ?? Get(vars, "CalculationBase")) * repeat : null;
        var block = source.Contains("GainBlock", StringComparison.Ordinal) ? Get(vars, "Block") : null;
        decimal? draw = null;
        if (source.Contains("CardPileCmd.Draw", StringComparison.Ordinal)) draw = Get(vars, "DrawAmount") ?? Get(vars, "Cards");
        decimal? energyGain = null;
        if (source.Contains("GainEnergy", StringComparison.Ordinal)) energyGain = Get(vars, "Energy");
        var hpLoss = source.Contains("CreatureCmd.Damage", StringComparison.Ordinal) ? Get(vars, "HpLoss") : null;
        decimal? maxHp = source.Contains("GainMaxHp", StringComparison.Ordinal) ? Get(vars, "MaxHp") : source.Contains("LoseMaxHp", StringComparison.Ordinal) ? -Get(vars, "MaxHp") : null;
        var numericCost = decimal.TryParse(cost, NumberStyles.Number, CultureInfo.InvariantCulture, out var cst) ? cst : (decimal?)null;
        var metrics = new MetricVector(damage, block, draw, energyGain, hpLoss, maxHp,
            damage.HasValue && numericCost > 0 ? damage / numericCost : null,
            block.HasValue && numericCost > 0 ? block / numericCost : null,
            draw.HasValue ? draw - 1 : null,
            numericCost.HasValue ? (numericCost <= 3 ? 1m : 0m) : null,
            damage.HasValue ? repeat : null,
            target == "AllEnemies" ? 1m : null,
            null, null);
        var unresolved = Unresolved(source);
        var display = Loc(_cardLoc, $"NEUVILLETTE_CARD_{id}.title", className);
        var description = Loc(_cardLoc, $"NEUVILLETTE_CARD_{id}.description", "");
        var exhaust = source.Contains("ShouldExhaust => true", StringComparison.Ordinal)
            || source.Contains("CardKeyword.Exhaust", StringComparison.Ordinal);
        var retain = source.Contains("ShouldRetain => true", StringComparison.Ordinal)
            || source.Contains("CardKeyword.Retain", StringComparison.Ordinal);
        var innate = source.Contains("IsInnate => true", StringComparison.Ordinal)
            || source.Contains("CardKeyword.Innate", StringComparison.Ordinal)
            || (upgraded && (source.Contains("IsInnate = true", StringComparison.Ordinal) || source.Contains("AddKeyword(CardKeyword.Innate", StringComparison.Ordinal)));
        return new CardSpec(id, className, display, description, rarity, type, target, cost, upgraded,
            exhaust, retain, innate,
            pool, Archetypes(className, source, description, cost),
            string.Join('|', vars.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value.ToString(CultureInfo.InvariantCulture)}")),
            metrics,
            $"静态解析直接资源；{(baseClass == "SurgeCard" ? "潮涌=当下治疗并等量记入战后潮落债务。" : "复杂钩子见源码与未解析栏。")}",
            unresolved, unresolved.Length == 0 ? EvidenceLevel.Derived : EvidenceLevel.Unresolved,
            unresolved.Length == 0 ? "High" : "Medium",
            relative, _tracker.LineOf(source, $"class {className}"));
    }

    private RelicSpec BuildRelic(string relative, string scope)
    {
        var source = _tracker.Read(relative);
        var cls = Path.GetFileNameWithoutExtension(relative);
        var id = ToId(cls);
        var rarity = ExtractEnum(source, "RelicRarity") ?? "Unknown";
        var display = Loc(_relicLoc, $"NEUVILLETTE_RELIC_{id}.title", cls);
        var description = Loc(_relicLoc, $"NEUVILLETTE_RELIC_{id}.description", "");
        var included = cls != "CatCake" || _sponsorRelics;
        return new RelicSpec(id, cls, display, rarity, scope, included,
            Touches(description + source), VariablesText(ExtractVariables(source)), description,
            EvidenceLevel.Exact, included ? "" : "SponsorRelicEnabled=false；不进入共享遗物池。",
            relative, _tracker.LineOf(source, $"class {cls}"));
    }

    private PotionSpec BuildPotion(string relative)
    {
        var source = _tracker.Read(relative);
        var cls = Path.GetFileNameWithoutExtension(relative);
        var id = ToId(cls);
        var rarity = ExtractEnum(source, "PotionRarity") ?? "Unknown";
        var display = Loc(_potionLoc, $"NEUVILLETTE_POTION_{id}.title", cls);
        var description = Loc(_potionLoc, $"NEUVILLETTE_POTION_{id}.description", "");
        return new PotionSpec(id, cls, display, rarity, "Character", true, Touches(description + source),
            VariablesText(ExtractVariables(source)), description, EvidenceLevel.Exact, "", relative,
            _tracker.LineOf(source, $"class {cls}"));
    }

    private List<string> RegisteredFiles(string folder, string marker)
    {
        var baseDir = Path.Combine(_root, "NeuvilletteCode", "Characters", "Neuvillette", folder);
        return Directory.GetFiles(baseDir, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => File.ReadAllText(path).Contains(marker, StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(_root, path).Replace('\\', '/')).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    private Dictionary<string, string> LoadLoc(string relative)
    {
        var text = _tracker.Read(relative);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(text) ?? new();
    }

    private static Dictionary<string, decimal> ExtractVariables(string source)
    {
        var vars = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (Match m in VarRegex.Matches(source))
        {
            var kind = m.Groups["kind"].Value.Replace("Var", "", StringComparison.Ordinal);
            var name = m.Groups["explicit"].Success ? m.Groups["explicit"].Value : kind;
            vars.TryAdd(name, decimal.Parse(m.Groups["value"].Value, CultureInfo.InvariantCulture));
        }
        foreach (Match m in NamedVarRegex.Matches(source)) vars.TryAdd(m.Groups["name"].Value, decimal.Parse(m.Groups["value"].Value, CultureInfo.InvariantCulture));
        foreach (Match m in PowerVarRegex.Matches(source)) vars.TryAdd(m.Groups["name"].Value, decimal.Parse(m.Groups["value"].Value, CultureInfo.InvariantCulture));
        return vars;
    }

    private static void ApplyUpgrades(string source, IDictionary<string, decimal> vars)
    {
        foreach (Match m in Regex.Matches(source, @"DynamicVars\.(?<name>\w+)\.UpgradeValueBy\(\s*(?<n>-?\d+(?:\.\d+)?)m?"))
            if (vars.ContainsKey(m.Groups["name"].Value)) vars[m.Groups["name"].Value] += decimal.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
        foreach (Match m in Regex.Matches(source, @"DynamicVars\[""(?<name>[^""]+)""\]\.UpgradeValueBy\(\s*(?<n>-?\d+(?:\.\d+)?)m?"))
            if (vars.ContainsKey(m.Groups["name"].Value)) vars[m.Groups["name"].Value] += decimal.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
        var surge = Regex.Match(source, @"UpgradeSurgeValue\s*=>\s*(?<n>-?\d+)");
        if (surge.Success && vars.ContainsKey("Surge")) vars["Surge"] += decimal.Parse(surge.Groups["n"].Value, CultureInfo.InvariantCulture);
    }

    private static string Unresolved(string source)
    {
        var items = new List<string>();
        if (source.Contains("PowerCmd.Apply", StringComparison.Ordinal)) items.Add("持续/状态效果");
        if (source.Contains("CardSelectCmd", StringComparison.Ordinal)) items.Add("选牌决策");
        if (source.Contains("CardFactory", StringComparison.Ordinal) || source.Contains("Rng.", StringComparison.Ordinal)) items.Add("随机生成");
        if (source.Contains("CalculatedDamage", StringComparison.Ordinal) || source.Contains("TryModify", StringComparison.Ordinal)) items.Add("条件函数");
        if (source.Contains("AfterCardPlayed", StringComparison.Ordinal) || source.Contains("AfterSideTurn", StringComparison.Ordinal)) items.Add("异步钩子");
        return string.Join('|', items.Distinct(StringComparer.Ordinal));
    }

    private static string Archetypes(string cls, string source, string description, string cost)
    {
        var s = cls + source + description;
        var tags = new List<string>();
        if (s.Contains("Surge", StringComparison.OrdinalIgnoreCase) || s.Contains("潮涌", StringComparison.Ordinal)) tags.Add("潮涌");
        if (s.Contains("SourcewaterDroplet", StringComparison.OrdinalIgnoreCase) || s.Contains("源水之滴", StringComparison.Ordinal)) tags.Add("源水之滴");
        if (s.Contains("Submit", StringComparison.OrdinalIgnoreCase) || s.Contains("呈堂", StringComparison.Ordinal)) tags.Add("呈堂");
        if (s.Contains("Sticker", StringComparison.OrdinalIgnoreCase) || s.Contains("美露莘", StringComparison.Ordinal)) tags.Add("美露莘");
        if (s.Contains("HpLoss", StringComparison.OrdinalIgnoreCase) || s.Contains("失去", StringComparison.Ordinal)) tags.Add("自伤");
        if (s.Contains("EquitableJudgment", StringComparison.OrdinalIgnoreCase) || s.Contains("衡平推裁", StringComparison.Ordinal)) tags.Add("衡平推裁");
        if (s.Contains("GainBlock", StringComparison.Ordinal) || s.Contains("格挡", StringComparison.Ordinal)) tags.Add("防御");
        if (int.TryParse(cost, out var c) && c >= 2) tags.Add("高费");
        return tags.Count == 0 ? "通用" : string.Join('|', tags);
    }

    private static string Touches(string text)
    {
        var tags = new List<string>();
        void Add(string tag, params string[] words) { if (words.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase))) tags.Add(tag); }
        Add("offense", "Damage", "Strength", "伤害", "力量"); Add("defense", "Block", "Buffer", "格挡", "缓冲");
        Add("hp", "Heal", "MaxHp", "生命", "潮涌"); Add("draw", "Draw", "抽"); Add("energy", "Energy", "能量");
        Add("deck", "Card", "Upgrade", "Transform", "牌", "升级", "变换"); Add("route", "Map", "RestSite", "Shop", "地图", "休息处", "商店");
        return tags.Count == 0 ? "none" : string.Join('|', tags.Distinct(StringComparer.Ordinal));
    }

    private static decimal? Get(IReadOnlyDictionary<string, decimal> vars, string name) => vars.TryGetValue(name, out var value) ? value : null;
    private static string VariablesText(IReadOnlyDictionary<string, decimal> vars) => string.Join('|', vars.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value.ToString(CultureInfo.InvariantCulture)}"));
    private static string Loc(IReadOnlyDictionary<string, string> loc, string key, string fallback) => loc.TryGetValue(key, out var value) ? value : fallback;
    private static string? ExtractEnum(string source, string enumName) => Regex.Match(source, $@"{enumName}\.(?<v>\w+)").Groups["v"].Value is { Length: > 0 } value ? value : null;
    private static string ExtractCtorCost(string source) => Regex.Match(source, @":\s*base\s*\(\s*(?<n>-?\d+)").Groups["n"].Value is { Length: > 0 } n ? n : "?";
    private static string ToId(string name) => string.Join('_',
        Regex.Matches(name, @"[A-Z]+(?=[A-Z][a-z]|$)|[A-Z]?[a-z]+|[0-9]+")
            .Select(m => m.Value.ToUpperInvariant()));
}
