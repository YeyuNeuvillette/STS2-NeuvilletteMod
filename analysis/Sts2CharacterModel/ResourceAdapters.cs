namespace Sts2CharacterModel;

public sealed record ResourceState(
    decimal Hp,
    decimal MaxHp,
    decimal Block,
    decimal Energy,
    int HandSize,
    int DrawPileSize,
    int DiscardPileSize,
    int ExhaustPileSize,
    int HpLossEvents,
    decimal Strength,
    decimal VulnerableMultiplier,
    decimal SourcewaterDroplets = 0,
    decimal SurgeDebt = 0,
    decimal OratricePoints = 0,
    decimal LivingWater = 0);

public sealed record ResourceTransition(ResourceState State, MetricVector Delta, bool Resolved, string Notes);

public interface IResourceAdapter
{
    string CharacterId { get; }
    ResourceTransition Apply(CardSpec card, ResourceState state, ScenarioSpec scenario);
}

public sealed class IroncladResourceAdapter : IResourceAdapter
{
    public string CharacterId => "IRONCLAD";

    public ResourceTransition Apply(CardSpec card, ResourceState state, ScenarioSpec scenario)
    {
        if (card.Evidence == EvidenceLevel.Unresolved)
            return new ResourceTransition(state, card.Metrics, false, card.UnresolvedComponents);

        var metrics = card.Metrics;
        var hpCost = metrics.SelfHpCost ?? 0;
        var next = state with
        {
            Hp = Math.Max(0, state.Hp - hpCost),
            MaxHp = state.MaxHp + (metrics.MaxHpGain ?? 0),
            Block = state.Block + (metrics.Block ?? 0),
            Energy = state.Energy - ParseCost(card.Cost) + (metrics.Energy ?? 0),
            HandSize = Math.Min(10, state.HandSize - 1 + (int)(metrics.Draw ?? 0)),
            ExhaustPileSize = state.ExhaustPileSize + (card.Exhaust ? 1 : 0),
            HpLossEvents = state.HpLossEvents + (hpCost > 0 ? 1 : 0)
        };
        return new ResourceTransition(next, metrics, true,
            "仅应用已提取的直接资源变化；持续能力、目标选择和战斗钩子由专属效果模型进一步处理。未解析项绝不按 0 处理。");
    }

    private static int ParseCost(string cost) => int.TryParse(cost, out var value) ? value : 0;
}

public sealed class NeuvilletteResourceAdapter : IResourceAdapter
{
    public string CharacterId => "NEUVILLETTE";

    public ResourceTransition Apply(CardSpec card, ResourceState state, ScenarioSpec scenario)
    {
        var metrics = card.Metrics;
        var numericCost = int.TryParse(card.Cost, out var parsed) ? parsed : 0;
        var hpCost = metrics.SelfHpCost ?? 0m;
        var next = state with
        {
            Hp = Math.Max(0m, state.Hp - hpCost),
            MaxHp = state.MaxHp + (metrics.MaxHpGain ?? 0m),
            Block = state.Block + (metrics.Block ?? 0m),
            Energy = state.Energy - numericCost + (metrics.Energy ?? 0m),
            HandSize = Math.Min(10, state.HandSize - 1 + (int)(metrics.Draw ?? 0m)),
            ExhaustPileSize = state.ExhaustPileSize + (card.Exhaust ? 1 : 0),
            HpLossEvents = state.HpLossEvents + (hpCost > 0m ? 1 : 0),
            SourcewaterDroplets = Math.Min(6m, state.SourcewaterDroplets + (hpCost > 0m ? 1m : 0m))
        };
        var resolved = card.Evidence != EvidenceLevel.Unresolved && card.UnresolvedComponents.Length == 0;
        return new ResourceTransition(next, metrics, resolved,
            resolved
                ? "仅落实直接资源变化；源水之滴上限为 6。潮涌债务、谕示阈值与持续能力由条件曲线处理。"
                : $"存在未解析机制：{card.UnresolvedComponents}。未解析部分未按零值处理。");
    }

    public static decimal SettleSurgeDebt(decimal currentHp, decimal maxHp, decimal debt) =>
        Math.Min(currentHp, Math.Max(currentHp - debt, maxHp * 0.5m));

    public static decimal SubmitPoints(decimal submittedCardCost, decimal proceduralJustice) =>
        10m + 10m * Math.Max(0m, submittedCardCost) + proceduralJustice;
}
