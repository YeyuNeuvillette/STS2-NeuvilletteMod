using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Neuvillette.Characters.Neuvillette.Ancients;
using Neuvillette.Characters.Neuvillette.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Neuvillette.Characters.Neuvillette.Events;

[RegisterSharedEvent]
public sealed class FourQuadrantsLand : ModEventTemplate
{
    private static readonly string PortraitPath = $"{MainFile.ResPath}/images/events/{MainFile.ModId.ToLowerInvariant()}_event_four_quadrants_land.png";

    public override EventAssetProfile AssetProfile => new(InitialPortraitPath: PortraitPath);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, Take, InitialOptionKey("TAKE"), HoverTipFactory.FromRelic<Relics.Persona>()),
            new EventOption(this, Leave, InitialOptionKey("LEAVE")),
        ];
    }

    private async Task Take()
    {
        await RelicCmd.Obtain<Relics.Persona>(base.Owner!);

        var acts = base.Owner?.RunState.Acts;
        var relicType1 = GetRelicTypeForAct(acts, 0);
        var relicType2 = GetRelicTypeForAct(acts, 1);
        var relicType3 = GetRelicTypeForAct(acts, 2);

        var locString = L10NLookup($"{Id.Entry}.pages.TAKE.description");
        locString.Add("Sense1", GetSenseDescription(relicType1));
        locString.Add("Sense2", GetSenseDescription(relicType2));
        locString.Add("Sense3", GetSenseDescription(relicType3));
        SetEventFinished(locString);
    }

    private Task Leave()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEAVE.description"));
        return Task.CompletedTask;
    }

    private static Type? GetRelicTypeForAct(IReadOnlyList<ActModel>? acts, int actIndex)
    {
        if (acts == null || actIndex >= acts.Count)
            return ArchitectAncient.GetRelicTypeForAct(actIndex);
        var bossEntry = acts[actIndex].BossEncounter?.Id.Entry;
        if (bossEntry != null)
        {
            var mapped = ArchitectAncient.GetRelicTypeForBoss(bossEntry);
            if (mapped != null)
                return mapped;
        }
        return ArchitectAncient.GetRelicTypeForAct(actIndex);
    }

    private string GetSenseDescription(Type? relicType)
    {
        return relicType?.Name switch
        {
            nameof(TimeSandglass) => L10NLookup($"{Id.Entry}.senses.TIME_SANDGLASS").GetFormattedText(),
            nameof(InjectReagent) => L10NLookup($"{Id.Entry}.senses.INJECT_REAGENT").GetFormattedText(),
            nameof(GuileCandle) => L10NLookup($"{Id.Entry}.senses.GUILE_CANDLE").GetFormattedText(),
            nameof(DemonKnowledge) => L10NLookup($"{Id.Entry}.senses.DEMON_KNOWLEDGE").GetFormattedText(),
            nameof(BottledSandCavern) => L10NLookup($"{Id.Entry}.senses.BOTTLED_SAND_CAVERN").GetFormattedText(),
            nameof(CrabShellShield) => L10NLookup($"{Id.Entry}.senses.CRAB_SHELL_SHIELD").GetFormattedText(),
            nameof(KindredFruitBasket) => L10NLookup($"{Id.Entry}.senses.KINDRED_FRUIT_BASKET").GetFormattedText(),
            nameof(InkSpiritGel) => L10NLookup($"{Id.Entry}.senses.INK_SPIRIT_GEL").GetFormattedText(),
            nameof(CeremonialSilverBell) => L10NLookup($"{Id.Entry}.senses.CEREMONIAL_SILVER_BELL").GetFormattedText(),
            nameof(SleepingShell) => L10NLookup($"{Id.Entry}.senses.SLEEPING_SHELL").GetFormattedText(),
            nameof(WaterfallBonsai) => L10NLookup($"{Id.Entry}.senses.WATERFALL_BONSAI").GetFormattedText(),
            nameof(ExoticFishSashimi) => L10NLookup($"{Id.Entry}.senses.EXOTIC_FISH_SASHIMI").GetFormattedText(),
            _ => "???",
        };
    }
}