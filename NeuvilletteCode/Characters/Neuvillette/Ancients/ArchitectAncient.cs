using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using Neuvillette.Characters.Neuvillette.Act;
using Neuvillette.Characters.Neuvillette.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Godot;

namespace Neuvillette.Characters.Neuvillette.Ancients;

[RegisterActAncient(typeof(NeuvilletteAct))]
public class ArchitectAncient : ModAncientEventTemplate
{
    private static readonly string IconBasePath = "res://Neuvillette/images/map/architect_ancient_icon";

    private static readonly Dictionary<string, Type> BossToRelicType = new()
    {
        { "VANTOM_BOSS", typeof(InkSpiritGel) },
        { "CEREMONIAL_BEAST_BOSS", typeof(CeremonialSilverBell) },
        { "THE_KIN_BOSS", typeof(KindredFruitBasket) },
        { "WATERFALL_GIANT_BOSS", typeof(WaterfallBonsai) },
        { "SOUL_FYSH_BOSS", typeof(ExoticFishSashimi) },
        { "LAGAVULIN_MATRIARCH_BOSS", typeof(SleepingShell) },
        { "THE_INSATIABLE_BOSS", typeof(BottledSandCavern) },
        { "KNOWLEDGE_DEMON_BOSS", typeof(DemonKnowledge) },
        { "KAISER_CRAB_BOSS", typeof(CrabShellShield) },
        { "QUEEN_BOSS", typeof(GuileCandle) },
        { "TEST_SUBJECT_BOSS", typeof(InjectReagent) },
        { "AEONGLASS_BOSS", typeof(TimeSandglass) },
    };

    private static readonly Type[][] ActRelicPools =
    [
        [typeof(KindredFruitBasket), typeof(CeremonialSilverBell), typeof(InkSpiritGel), typeof(SleepingShell), typeof(WaterfallBonsai), typeof(ExoticFishSashimi)],
        [typeof(BottledSandCavern), typeof(DemonKnowledge), typeof(CrabShellShield)],
        [typeof(GuileCandle), typeof(InjectReagent), typeof(TimeSandglass)],
    ];

    public static Type? GetRelicTypeForAct(int actIndex)
    {
        if (actIndex < 0 || actIndex >= ActRelicPools.Length)
            return null;
        return ActRelicPools[actIndex][0];
    }

    public static Type? GetRelicTypeForBoss(string bossEntry)
    {
        return BossToRelicType.GetValueOrDefault(bossEntry);
    }

    public override EventAssetProfile AssetProfile => new(
        BackgroundScenePath: "res://Neuvillette/scenes/ancients/architect_ancient.tscn"
    );

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: IconBasePath + ".png",
        MapIconOutlinePath: IconBasePath + "_outline.png",
        RunHistoryIconPath: IconBasePath + ".png",
        RunHistoryIconOutlinePath: IconBasePath + "_outline.png"
    );

    public override Color ButtonColor => new(0.15f, 0.12f, 0.08f, 0.75f);
    public override Color DialogueColor => new Color("3D2E1A");

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<TimeSandglass>(),
        CreateModRelicOption<DemonKnowledge>(),
        CreateModRelicOption<InjectReagent>(),
        CreateModRelicOption<GuileCandle>(),
        CreateModRelicOption<BottledSandCavern>(),
        CreateModRelicOption<CrabShellShield>(),
        CreateModRelicOption<KindredFruitBasket>(),
        CreateModRelicOption<InkSpiritGel>(),
        CreateModRelicOption<CeremonialSilverBell>(),
        CreateModRelicOption<SleepingShell>(),
        CreateModRelicOption<WaterfallBonsai>(),
        CreateModRelicOption<ExoticFishSashimi>(),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var acts = Owner?.RunState.Acts;
        var options = new List<EventOption>();

        for (int i = Math.Min(2, (acts?.Count ?? 0) - 1); i >= 0; i--)
        {
            var bossEntry = acts![i].BossEncounter?.Id.Entry;
            if (bossEntry != null && BossToRelicType.TryGetValue(bossEntry, out var relicType))
            {
                options.Add(CreateRelicOptionByType(relicType));
            }
            else if (i < ActRelicPools.Length)
            {
                var pool = ActRelicPools[i];
                var fallbackType = pool[Rng.NextInt(pool.Length)];
                options.Add(CreateRelicOptionByType(fallbackType));
            }
            else
            {
                options.Add(CreateModRelicOption<TimeSandglass>());
            }
        }

        while (options.Count < 3)
        {
            options.Add(CreateModRelicOption<TimeSandglass>());
        }

        return options;
    }

    private EventOption CreateRelicOptionByType(Type relicType)
    {
        if (relicType == typeof(TimeSandglass)) return CreateModRelicOption<TimeSandglass>();
        if (relicType == typeof(DemonKnowledge)) return CreateModRelicOption<DemonKnowledge>();
        if (relicType == typeof(InjectReagent)) return CreateModRelicOption<InjectReagent>();
        if (relicType == typeof(GuileCandle)) return CreateModRelicOption<GuileCandle>();
        if (relicType == typeof(BottledSandCavern)) return CreateModRelicOption<BottledSandCavern>();
        if (relicType == typeof(CrabShellShield)) return CreateModRelicOption<CrabShellShield>();
        if (relicType == typeof(KindredFruitBasket)) return CreateModRelicOption<KindredFruitBasket>();
        if (relicType == typeof(InkSpiritGel)) return CreateModRelicOption<InkSpiritGel>();
        if (relicType == typeof(CeremonialSilverBell)) return CreateModRelicOption<CeremonialSilverBell>();
        if (relicType == typeof(SleepingShell)) return CreateModRelicOption<SleepingShell>();
        if (relicType == typeof(WaterfallBonsai)) return CreateModRelicOption<WaterfallBonsai>();
        if (relicType == typeof(ExoticFishSashimi)) return CreateModRelicOption<ExoticFishSashimi>();
        return CreateModRelicOption<TimeSandglass>();
    }
}