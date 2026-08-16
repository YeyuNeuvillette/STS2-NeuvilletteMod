using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Sts2CharacterModel;

internal sealed class SourceTracker
{
    private readonly string _root;
    private readonly Dictionary<string, SourceReference> _references = new(StringComparer.OrdinalIgnoreCase);

    public SourceTracker(string root) => _root = Path.GetFullPath(root);

    public IReadOnlyCollection<SourceReference> References => _references.Values;

    public string Read(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_root, normalized));
        var text = File.ReadAllText(full);
        var bytes = File.ReadAllBytes(full);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var info = new FileInfo(full);
        _references[relativePath.Replace('\\', '/')] = new SourceReference(
            relativePath.Replace('\\', '/'), 1, hash, info.LastWriteTimeUtc);
        return text;
    }

    public int LineOf(string text, string token)
    {
        var index = text.IndexOf(token, StringComparison.Ordinal);
        return index < 0 ? 1 : text.AsSpan(0, index).Count('\n') + 1;
    }

    public string Fingerprint()
    {
        var canonical = string.Join('\n', _references.Values
            .OrderBy(x => x.RelativePath, StringComparer.Ordinal)
            .Select(x => $"{x.RelativePath}\t{x.Sha256}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}

internal sealed class SourceScanner
{
    private static readonly Regex ModelTypeRegex = new(@"ModelDb\.(?:Card|Relic|Potion|Encounter|Monster)<(?<name>[A-Za-z0-9_]+)>\(\)", RegexOptions.Compiled);
    private static readonly Regex PoolTypeRegex = new(@"ModelDb\.(?<kind>Card|Relic|Potion)<(?<name>[A-Za-z0-9_]+)>\(\)", RegexOptions.Compiled);
    private readonly string _sourceRoot;
    private readonly SourceTracker _tracker;
    private readonly Dictionary<string, string> _cardsLoc;
    private readonly Dictionary<string, string> _relicsLoc;
    private readonly Dictionary<string, string> _potionsLoc;
    private readonly Assembly _gameAssembly = typeof(CardModel).Assembly;

    public SourceScanner(string sourceRoot, SourceTracker tracker)
    {
        _sourceRoot = sourceRoot;
        _tracker = tracker;
        _cardsLoc = LoadLocalization("localization/zhs/cards.json");
        _relicsLoc = LoadLocalization("localization/zhs/relics.json");
        _potionsLoc = LoadLocalization("localization/zhs/potions.json");
    }

    public IReadOnlyList<CardSpec> ScanCards()
    {
        var poolPath = "src/Core/Models/CardPools/IroncladCardPool.cs";
        var pool = _tracker.Read(poolPath);
        var classNames = Regex.Matches(pool, @"ModelDb\.Card<(?<name>[A-Za-z0-9_]+)>\(\)")
            .Select(m => m.Groups["name"].Value).Distinct().ToList();
        var epochMap = BuildEpochMap();
        var result = new List<CardSpec>(classNames.Count * 2);
        foreach (var className in classNames)
        {
            var relative = $"src/Core/Models/Cards/{className}.cs";
            var source = _tracker.Read(relative);
            var id = ToModelId(className);
            for (var upgraded = 0; upgraded <= 1; upgraded++)
            {
                result.Add(BuildCard(className, id, relative, source, upgraded == 1, epochMap));
            }
        }
        return result;
    }

    public IReadOnlyList<RelicSpec> ScanCharacterRelics()
    {
        const string poolPath = "src/Core/Models/RelicPools/IroncladRelicPool.cs";
        var pool = _tracker.Read(poolPath);
        return Regex.Matches(pool, @"ModelDb\.Relic<(?<name>[A-Za-z0-9_]+)>\(\)")
            .Select(m => m.Groups["name"].Value).Distinct()
            .Select(name => BuildRelic(name, "Character", true, string.Empty)).ToList();
    }

    public IReadOnlyList<PotionSpec> ScanCharacterPotions()
    {
        const string path = "src/Core/Timeline/Epochs/Ironclad4Epoch.cs";
        var source = _tracker.Read(path);
        return Regex.Matches(source, @"ModelDb\.Potion<(?<name>[A-Za-z0-9_]+)>\(\)")
            .Select(m => m.Groups["name"].Value).Distinct()
            .Select(name => BuildPotion(name, "Character", true, string.Empty)).ToList();
    }

    public IReadOnlyList<RelicSpec> ScanSharedRelics()
    {
        const string poolPath = "src/Core/Models/RelicPools/SharedRelicPool.cs";
        var pool = _tracker.Read(poolPath);
        return Regex.Matches(pool, @"ModelDb\.Relic<(?<name>[A-Za-z0-9_]+)>\(\)")
            .Select(m => m.Groups["name"].Value).Distinct()
            .Select(name =>
            {
                var source = ReadIfExists($"src/Core/Models/Relics/{name}.cs");
                var touches = DetectTouches(source);
                var included = touches.Length > 0;
                return BuildRelic(name, "Shared", included, included ? string.Empty : "未检测到直接改变本模型指标的实现入口");
            }).ToList();
    }

    public IReadOnlyList<PotionSpec> ScanSharedPotions()
    {
        const string poolPath = "src/Core/Models/PotionPools/SharedPotionPool.cs";
        var pool = _tracker.Read(poolPath);
        return Regex.Matches(pool, @"ModelDb\.Potion<(?<name>[A-Za-z0-9_]+)>\(\)")
            .Select(m => m.Groups["name"].Value).Distinct()
            .Select(name =>
            {
                var source = ReadIfExists($"src/Core/Models/Potions/{name}.cs");
                var touches = DetectTouches(source);
                var included = touches.Length > 0;
                return BuildPotion(name, "Shared", included, included ? string.Empty : "未检测到直接改变本模型指标的实现入口");
            }).ToList();
    }

    public IReadOnlyList<AncientOfferingSpec> ScanAncients()
    {
        var definitions = new (string Name, int Act, string Slot)[]
        {
            ("Neow", 1, "2 个正面栏 + 1 个诅咒栏"),
            ("Orobas", 2, "三个独立栏位各取 1"),
            ("Pael", 2, "三个独立栏位各取 1，第二栏含重复权重"),
            ("Tezcatara", 2, "三个独立栏位各取 1"),
            ("Nonupeipe", 3, "合格池洗牌取 3"),
            ("Tanx", 3, "合格池洗牌取 3"),
            ("Vakuu", 3, "三个独立栏位各取 1"),
            ("Darv", 2, "共享先古；50% 为 2 件 Boss 遗物+尘封魔典，否则 3 件 Boss 遗物")
        };
        var result = new List<AncientOfferingSpec>();
        foreach (var def in definitions)
        {
            var path = $"src/Core/Models/Events/{def.Name}.cs";
            var source = _tracker.Read(path);
            var names = Regex.Matches(source, @"(?:RelicOption|ModelDb\.Relic)<(?<name>[A-Za-z0-9_]+)>")
                .Select(m => (Name: m.Groups["name"].Value, Line: _tracker.LineOf(source, m.Value)))
                .GroupBy(x => x.Name).Select(g => g.First()).ToList();
            foreach (var item in names)
            {
                var id = ToModelId(item.Name);
                var availability = DetectAncientAvailability(def.Name, item.Name, source);
                result.Add(new AncientOfferingSpec(
                    def.Name, def.Act, item.Name, Lookup(_relicsLoc, id, "title", item.Name), def.Slot,
                    availability, ProbabilityForAncient(def.Name, item.Name),
                    availability == "Always in base candidate set" ? EvidenceLevel.Derived : EvidenceLevel.Interpretive,
                    path, item.Line));
            }
        }
        return result;
    }

    public IReadOnlyList<DynamicCurvePoint> BuildDynamicCurves(IReadOnlyList<CardSpec> cards)
    {
        var result = new List<DynamicCurvePoint>();
        foreach (var upgraded in new[] { false, true })
        {
            var ashBase = upgraded ? 6m : 6m;
            var ashPer = upgraded ? 4m : 3m;
            foreach (var exhaust in new[] { 0m, 1m, 3m, 6m, 10m })
                result.Add(Curve("ASHEN_STRIKE", upgraded, "ExhaustCount", exhaust, 0, ashBase + ashPer * exhaust, "damage", $"6 + {(upgraded ? 4 : 3)}×ExhaustCount"));

            foreach (var block in new[] { 0m, 5m, 10m, 30m, 60m })
                result.Add(Curve("BODY_SLAM", upgraded, "CurrentBlock", block, 0, block, "damage", "CurrentBlock"));

            foreach (var hand in new[] { 0m, 2m, 4m, 6m, 9m })
                result.Add(Curve("FIEND_FIRE", upgraded, "OtherCardsInHand", hand, 0, (upgraded ? 10m : 7m) * hand, "damage", $"{(upgraded ? 10 : 7)}×OtherCardsInHand"));

            foreach (var energy in new[] { 0m, 1m, 2m, 3m, 4m, 5m })
            foreach (var targets in new[] { 1m, 2m, 3m })
                result.Add(Curve("WHIRLWIND", upgraded, "EnergyX", energy, targets, (upgraded ? 8m : 5m) * energy * targets, "total damage", $"{(upgraded ? 8 : 5)}×X×Targets"));

            var tear = cards.FirstOrDefault(c => c.Id == "TEAR_ASUNDER" && c.Upgraded == upgraded);
            if (tear?.Metrics.Damage is decimal tearDamage)
            {
                foreach (var losses in new[] { 0m, 1m, 3m, 6m })
                    result.Add(Curve("TEAR_ASUNDER", upgraded, "HpLossEvents", losses, 0, tearDamage * (1 + losses), "damage", $"{tearDamage}×(1+HpLossEvents)"));
            }
        }
        return result;
    }

    private CardSpec BuildCard(string className, string id, string relative, string source, bool upgraded, IReadOnlyDictionary<string, string> epochMap)
    {
        var type = _gameAssembly.GetType($"MegaCrit.Sts2.Core.Models.Cards.{className}");
        CardModel? card = null;
        string instantiateError = string.Empty;
        try
        {
            card = type is null ? null : (CardModel?)Activator.CreateInstance(type);
            if (card is not null && upgraded)
            {
                // The decompiled snapshot is intentionally used without booting Godot/ModelDb.
                // Mark this isolated analysis instance mutable so its real OnUpgrade method can run.
                typeof(AbstractModel).GetProperty(nameof(AbstractModel.IsMutable), BindingFlags.Instance | BindingFlags.Public)!
                    .SetValue(card, true);
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }
        }
        catch (Exception ex)
        {
            instantiateError = ex.GetBaseException().Message;
        }

        var vars = new List<(string Key, string Type, decimal Value)>();
        if (card is not null)
        {
            try
            {
                vars.AddRange(card.DynamicVars.Select(kv => (kv.Key, kv.Value.GetType().Name, kv.Value.BaseValue)));
            }
            catch (Exception ex)
            {
                instantiateError = Append(instantiateError, "DynamicVars: " + ex.GetBaseException().Message);
            }
        }

        decimal? damage = FindMetric(vars, "DamageVar", "Damage", excludeCalculated: true);
        decimal? block = FindMetric(vars, "BlockVar", "Block");
        decimal? draw = FindMetric(vars, "CardsVar", "Cards");
        decimal? energy = FindMetric(vars, "EnergyVar", "Energy");
        decimal? hpLoss = FindMetric(vars, "HpLossVar", "HpLoss");
        decimal? maxHp = FindMetric(vars, "MaxHpVar", "MaxHp");
        var cost = card is null ? ParseConstructorCost(source) : card.EnergyCost.CostsX ? "X" : card.EnergyCost.GetWithModifiers(CostModifiers.Local).ToString(CultureInfo.InvariantCulture);
        var numericCost = int.TryParse(cost, out var parsedCost) ? parsedCost : 0;
        var typeName = card?.Type.ToString() ?? ParseConstructorPart(source, 1);
        var rarity = card?.Rarity.ToString() ?? ParseConstructorPart(source, 2);
        var target = card?.TargetType.ToString() ?? ParseConstructorPart(source, 3);
        var keywords = ParseKeywords(source, upgraded);
        var effectModel = BuildEffectModel(id, source, upgraded, damage, block, draw, energy, hpLoss, maxHp);
        var unresolved = DetectUnresolved(id, source, instantiateError);
        var evidence = unresolved.Length == 0 ? (IsSimpleCard(source) ? EvidenceLevel.Exact : EvidenceLevel.Derived) : EvidenceLevel.Unresolved;
        var metrics = new MetricVector(
            damage, block, draw, energy, hpLoss, maxHp,
            numericCost > 0 && damage.HasValue ? decimal.Round(damage.Value / numericCost, 4) : null,
            numericCost > 0 && block.HasValue ? decimal.Round(block.Value / numericCost, 4) : null,
            draw.HasValue ? draw.Value - 1 : -1,
            numericCost <= 3 ? 1m : 0m,
            EstimateStrengthSensitivity(id, source),
            target == "AllEnemies" ? 1m : 0m,
            EstimateScaling(id, source, vars),
            null);
        return new CardSpec(
            id, className, Lookup(_cardsLoc, id, "title", className), Lookup(_cardsLoc, id, "description", string.Empty),
            rarity, typeName, target, cost, upgraded,
            keywords.Contains("Exhaust"), keywords.Contains("Retain"), keywords.Contains("Innate"),
            epochMap.GetValueOrDefault(className, rarity is "Basic" ? "StartingDeck" : rarity is "Ancient" ? "AncientOnly" : "BasePool"),
            DetectArchetypes(id, source),
            string.Join(';', vars.Select(v => $"{v.Key}:{v.Type}={v.Value.ToString(CultureInfo.InvariantCulture)}")),
            metrics, effectModel, unresolved, evidence, evidence switch
            {
                EvidenceLevel.Exact => "High", EvidenceLevel.Derived => "Medium", _ => "Low"
            }, relative, _tracker.LineOf(source, $"class {className}"));
    }

    private RelicSpec BuildRelic(string className, string scope, bool included, string exclusionReason)
    {
        var relative = $"src/Core/Models/Relics/{className}.cs";
        var source = _tracker.Read(relative);
        var id = ToModelId(className);
        var rarity = Regex.Match(source, @"Rarity\s*=>\s*RelicRarity\.(?<v>\w+)").Groups["v"].Value;
        var vars = ParseDynamicVarsFromText(source);
        var touches = DetectTouches(source);
        return new RelicSpec(id, className, Lookup(_relicsLoc, id, "title", className), rarity, scope, included,
            touches, vars, SummarizeImplementation(source), included ? EvidenceLevel.Derived : EvidenceLevel.Interpretive,
            exclusionReason, relative, _tracker.LineOf(source, $"class {className}"));
    }

    private PotionSpec BuildPotion(string className, string scope, bool included, string exclusionReason)
    {
        var relative = $"src/Core/Models/Potions/{className}.cs";
        var source = _tracker.Read(relative);
        var id = ToModelId(className);
        var rarity = Regex.Match(source, @"Rarity\s*=>\s*PotionRarity\.(?<v>\w+)").Groups["v"].Value;
        var touches = DetectTouches(source);
        return new PotionSpec(id, className, Lookup(_potionsLoc, id, "title", className), rarity, scope, included,
            touches, ParseDynamicVarsFromText(source), SummarizeImplementation(source),
            included ? EvidenceLevel.Derived : EvidenceLevel.Interpretive,
            exclusionReason, relative, _tracker.LineOf(source, $"class {className}"));
    }

    private Dictionary<string, string> BuildEpochMap()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var epoch in new[] { "Ironclad2Epoch", "Ironclad5Epoch", "Ironclad7Epoch" })
        {
            var source = _tracker.Read($"src/Core/Timeline/Epochs/{epoch}.cs");
            foreach (Match match in Regex.Matches(source, @"ModelDb\.Card<(?<name>[A-Za-z0-9_]+)>\(\)"))
                result[match.Groups["name"].Value] = epoch;
        }
        return result;
    }

    private Dictionary<string, string> LoadLocalization(string relative)
    {
        var json = _tracker.Read(relative);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    private string ReadIfExists(string relative)
    {
        var full = Path.Combine(_sourceRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(full) ? _tracker.Read(relative) : string.Empty;
    }

    private static string Lookup(Dictionary<string, string> dict, string id, string suffix, string fallback) =>
        dict.GetValueOrDefault($"{id}.{suffix}", fallback);

    internal static string ToModelId(string className)
    {
        var text = Regex.Replace(className, "([a-z0-9])([A-Z])", "$1_$2");
        text = Regex.Replace(text, "([A-Z]+)([A-Z][a-z])", "$1_$2");
        return text.ToUpperInvariant();
    }

    private static string ParseConstructorCost(string source)
    {
        var match = Regex.Match(source, @":\s*base\(\s*(?<v>-?\d+)");
        return match.Success ? match.Groups["v"].Value : source.Contains("HasEnergyCostX => true") ? "X" : string.Empty;
    }

    private static string ParseConstructorPart(string source, int part)
    {
        var match = Regex.Match(source, @":\s*base\(\s*-?\d+\s*,\s*CardType\.(?<type>\w+)\s*,\s*CardRarity\.(?<rarity>\w+)\s*,\s*TargetType\.(?<target>\w+)");
        return !match.Success ? string.Empty : part switch { 1 => match.Groups["type"].Value, 2 => match.Groups["rarity"].Value, 3 => match.Groups["target"].Value, _ => string.Empty };
    }

    private static decimal? FindMetric(IEnumerable<(string Key, string Type, decimal Value)> vars, string typeContains, string keyContains, bool excludeCalculated = false)
    {
        var matches = vars.Where(v => v.Type.Contains(typeContains, StringComparison.OrdinalIgnoreCase)
                                      || v.Key.Equals(keyContains, StringComparison.OrdinalIgnoreCase));
        if (excludeCalculated) matches = matches.Where(v => !v.Type.Contains("Calculated", StringComparison.OrdinalIgnoreCase));
        var values = matches.Select(v => v.Value).ToList();
        return values.Count == 0 ? null : values.Max();
    }

    private static HashSet<string> ParseKeywords(string source, bool upgraded)
    {
        var beforeUpgrade = source.Split("protected override void OnUpgrade", StringSplitOptions.None)[0];
        var keywords = Regex.Matches(beforeUpgrade, @"CardKeyword\.(?<v>\w+)").Select(m => m.Groups["v"].Value).ToHashSet();
        if (upgraded)
        {
            var upgrade = source.Contains("protected override void OnUpgrade", StringComparison.Ordinal)
                ? source[(source.IndexOf("protected override void OnUpgrade", StringComparison.Ordinal))..]
                : string.Empty;
            foreach (Match match in Regex.Matches(upgrade, @"AddKeyword\(CardKeyword\.(?<v>\w+)\)")) keywords.Add(match.Groups["v"].Value);
            foreach (Match match in Regex.Matches(upgrade, @"RemoveKeyword\(CardKeyword\.(?<v>\w+)\)")) keywords.Remove(match.Groups["v"].Value);
        }
        return keywords;
    }

    private static string BuildEffectModel(string id, string source, bool upgraded, decimal? damage, decimal? block, decimal? draw, decimal? energy, decimal? hpLoss, decimal? maxHp)
    {
        return id switch
        {
            "ASHEN_STRIKE" => $"Damage=6+{(upgraded ? 4 : 3)}×ExhaustCount",
            "BODY_SLAM" => $"Damage=CurrentBlock; Cost={(upgraded ? 0 : 1)}",
            "FIEND_FIRE" => $"Damage={(upgraded ? 10 : 7)}×OtherCardsInHand; exhaust those cards",
            "WHIRLWIND" => $"TotalDamage={(upgraded ? 8 : 5)}×EnergyX×TargetCount",
            "OFFERING" => $"HpCost=6; Energy=2; Draw={(upgraded ? 5 : 3)}; Exhaust",
            "BATTLE_TRANCE" => $"Draw={(upgraded ? 4 : 3)}; apply NoDraw for turn",
            "FEED" => $"Damage={(upgraded ? 12 : 10)}; fatal MaxHp+{(upgraded ? 4 : 3)}; Exhaust",
            "CORRUPTION" => $"Skills cost 0 and exhaust after play; setup Cost={(upgraded ? 2 : 3)}",
            "INFERNO" => $"Reactive AoE={FindFirstNumber(source, "PowerVar<InfernoPower>", upgraded ? 9 : 6)} per player-turn HP-loss event; next-turn self-damage grows per copy",
            "ANGER" => $"Damage={damage}; add an identical copy to discard",
            "ARMAMENTS" => $"Block={block}; upgrade {(upgraded ? "all upgradable cards in hand" : "one selected card in hand")}",
            "TEAR_ASUNDER" => $"Damage={damage}×(1+HpLossEventsThisCombat)",
            _ => ComposeDirect(damage, block, draw, energy, hpLoss, maxHp, source)
        };
    }

    private static string ComposeDirect(decimal? damage, decimal? block, decimal? draw, decimal? energy, decimal? hpLoss, decimal? maxHp, string source)
    {
        var parts = new List<string>();
        if (damage.HasValue) parts.Add($"Damage={damage}");
        if (block.HasValue) parts.Add($"Block={block}");
        if (draw.HasValue) parts.Add($"Draw={draw}");
        if (energy.HasValue) parts.Add($"Energy={energy}");
        if (hpLoss.HasValue) parts.Add($"HpCost={hpLoss}");
        if (maxHp.HasValue) parts.Add($"MaxHp={maxHp}");
        if (source.Contains("PowerCmd.Apply", StringComparison.Ordinal)) parts.Add("ApplyPower");
        if (source.Contains("CardPileCmd.Add", StringComparison.Ordinal)) parts.Add("MoveOrGenerateCard");
        if (source.Contains("CardCmd.Exhaust", StringComparison.Ordinal)) parts.Add("ExhaustEffect");
        return parts.Count == 0 ? "Conditional/non-scalar effect; see source" : string.Join("; ", parts);
    }

    private static string DetectUnresolved(string id, string source, string instantiateError)
    {
        var resolvedOverrides = new HashSet<string>(StringComparer.Ordinal)
        {
            "ANGER", "ARMAMENTS", "ASHEN_STRIKE", "BATTLE_TRANCE", "BODY_SLAM", "CORRUPTION", "FEED", "FIEND_FIRE", "INFERNO", "OFFERING", "TEAR_ASUNDER", "WHIRLWIND"
        };
        var parts = new List<string>();
        if (instantiateError.Length > 0) parts.Add(instantiateError);
        if (!resolvedOverrides.Contains(id))
        {
            if (source.Contains("CalculatedDamageVar", StringComparison.Ordinal)) parts.Add("state-dependent calculated damage");
            if (source.Contains("Random", StringComparison.Ordinal) || source.Contains("NextItem", StringComparison.Ordinal)) parts.Add("random selection");
            if (source.Contains("CardSelectCmd", StringComparison.Ordinal)) parts.Add("player card selection");
            if (source.Contains("PowerCmd.Apply", StringComparison.Ordinal) && !Regex.IsMatch(source, @"PowerVar<[^>]+>\(\s*\d+")) parts.Add("non-scalar power behavior");
        }
        return string.Join("; ", parts.Distinct());
    }

    private static bool IsSimpleCard(string source) =>
        !source.Contains("Calculated", StringComparison.Ordinal)
        && !source.Contains("PowerCmd.Apply", StringComparison.Ordinal)
        && !source.Contains("CardSelectCmd", StringComparison.Ordinal)
        && !source.Contains("Random", StringComparison.Ordinal)
        && !source.Contains("foreach", StringComparison.Ordinal);

    private static decimal? EstimateStrengthSensitivity(string id, string source)
    {
        if (!source.Contains("DamageCmd.Attack", StringComparison.Ordinal)) return 0;
        if (id == "WHIRLWIND" || id == "FIEND_FIRE" || id == "TEAR_ASUNDER") return null;
        var match = Regex.Match(source, @"WithHitCount\((?<v>\d+)\)");
        return match.Success ? decimal.Parse(match.Groups["v"].Value, CultureInfo.InvariantCulture) : 1m;
    }

    private static decimal? EstimateScaling(string id, string source, IEnumerable<(string Key, string Type, decimal Value)> vars)
    {
        if (id == "DEMON_FORM") return vars.FirstOrDefault(v => v.Key.Contains("Strength", StringComparison.OrdinalIgnoreCase)).Value;
        if (source.Contains("AfterPlayerTurnStart", StringComparison.Ordinal) || source.Contains("BeforeSideTurnStart", StringComparison.Ordinal)) return 1m;
        return source.Contains("PowerCmd.Apply", StringComparison.Ordinal) ? null : 0m;
    }

    private static string DetectArchetypes(string id, string source)
    {
        var tags = new List<string>();
        AddIf(tags, "Direct/Heavy", source.Contains("DamageCmd.Attack", StringComparison.Ordinal));
        AddIf(tags, "Strength/MultiHit", source.Contains("StrengthPower", StringComparison.Ordinal) || source.Contains("WithHitCount", StringComparison.Ordinal));
        AddIf(tags, "Exhaust", source.Contains("Exhaust", StringComparison.Ordinal));
        AddIf(tags, "Block", source.Contains("GainBlock", StringComparison.Ordinal) || id is "BARRICADE" or "BODY_SLAM");
        AddIf(tags, "SelfDamage/Fire", source.Contains("HpLoss", StringComparison.Ordinal) || source.Contains("Inferno", StringComparison.Ordinal) || source.Contains("Fire", StringComparison.Ordinal));
        AddIf(tags, "Energy/Engine", source.Contains("GainEnergy", StringComparison.Ordinal) || source.Contains("Draw", StringComparison.Ordinal) || source.Contains("EnergyCost", StringComparison.Ordinal));
        return string.Join(';', tags.Distinct());
    }

    private static string DetectTouches(string source)
    {
        var map = new (string Name, string[] Tokens)[]
        {
            ("Offense", ["DamageCmd", "ModifyDamage", "StrengthPower", "VulnerablePower", "ThornsPower"]),
            ("Defense", ["GainBlock", "ModifyBlock", "DexterityPower", "WeakPower", "Intangible", "Heal"]),
            ("Health", ["MaxHp", "CurrentHp", "CreatureCmd.Heal", "LoseMaxHp"]),
            ("Engine", ["CardPileCmd.Draw", "ModifyHandDraw", "Exhaust", "Discard", "Retain", "CardReward", "Upgrade"]),
            ("Energy", ["GainEnergy", "ModifyMaxEnergy", "EnergyCost", "SetToFree"]),
            ("Economy", ["Gold", "Shop", "Merchant", "Potion", "Reward"]),
            ("Map", ["ActMap", "MapPoint", "RestSite", "TreasureRoom"]),
            ("Enemy", ["Monster", "Enemy", "ArtifactPower"])
        };
        return string.Join(';', map.Where(x => x.Tokens.Any(source.Contains)).Select(x => x.Name));
    }

    private static string ParseDynamicVarsFromText(string source)
    {
        return string.Join(';', Regex.Matches(source, @"new\s+(?<type>[A-Za-z0-9_]+Var)(?:<[^>]+>)?\((?<args>[^\)]*)\)")
            .Select(m => $"{m.Groups["type"].Value}({Regex.Replace(m.Groups["args"].Value, @"\s+", " ").Trim()})")
            .Distinct());
    }

    private static string SummarizeImplementation(string source)
    {
        var methods = new[] { "AfterObtained", "AfterCombatVictory", "BeforeCombatStart", "AfterCombatStart", "AfterPlayerTurnStart", "BeforePlayerTurnEnd", "ModifyDamage", "ModifyBlock", "ModifyMaxEnergy", "TryModifyEnergyCostInCombat" };
        var present = methods.Where(m => source.Contains(m, StringComparison.Ordinal)).ToList();
        return present.Count == 0 ? "Source-defined effect; inspect implementation" : string.Join(';', present);
    }

    private static string DetectAncientAvailability(string ancient, string offering, string source)
    {
        if (source.Contains($"{offering}Option", StringComparison.Ordinal) &&
            (source.Contains("Can", StringComparison.Ordinal) || source.Contains("Where", StringComparison.Ordinal)))
            return "Conditional on deck/relic/player state";
        if (ancient == "Darv" && offering is "Ectoplasm" or "Sozu") return "Act 2 only";
        if (ancient == "Darv" && offering is "PhilosophersStone" or "VelvetChoker") return "Act 2 or later";
        return "Always in base candidate set";
    }

    private static string ProbabilityForAncient(string ancient, string offering) => ancient switch
    {
        "Neow" => "Conditional pool probability; 2 positive draws plus 1 cursed draw without replacement",
        "Orobas" => "One draw from its assigned slot; cross-character and legality branches apply",
        "Pael" => offering == "PaelsGrowth" ? "Second-slot weight 1 versus weight 2 for each eligible non-growth item" : "One draw from assigned slot; second-slot eligible items may have weight 2",
        "Tezcatara" => "Uniform within assigned slot after eligibility filtering",
        "Nonupeipe" or "Tanx" => "3 / eligible-pool-size inclusion probability",
        "Vakuu" => "Uniform within assigned slot",
        "Darv" => "50% branch: 2 boss relics + Dusty Tome; 50% branch: 3 boss relics",
        _ => "Conditional"
    };

    private static DynamicCurvePoint Curve(string id, bool upgraded, string variable, decimal input, decimal secondary, decimal output, string unit, string formula) =>
        new(id, upgraded, variable, input, secondary, output, unit, formula, EvidenceLevel.Exact);

    private static decimal FindFirstNumber(string source, string near, decimal fallback)
    {
        var index = source.IndexOf(near, StringComparison.Ordinal);
        if (index < 0) return fallback;
        var match = Regex.Match(source[index..Math.Min(source.Length, index + 120)], @"\((?<v>\d+(?:\.\d+)?)m?\)");
        return match.Success ? decimal.Parse(match.Groups["v"].Value, CultureInfo.InvariantCulture) : fallback;
    }

    private static string Append(string current, string addition) => current.Length == 0 ? addition : current + "; " + addition;
    private static void AddIf(List<string> list, string value, bool condition) { if (condition) list.Add(value); }
}
