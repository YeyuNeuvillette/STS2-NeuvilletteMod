using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Neuvillette.Patches;

internal static class NeuvilletteWaterDamageAudioState
{
    private static readonly AsyncLocal<int> Suppressions = new();

    public static void Begin(CardModel? cardSource, int monsterTargetCount)
    {
        Suppressions.Value = IsWaterAttack(cardSource) ? monsterTargetCount : 0;
    }

    public static bool Consume()
    {
        if (Suppressions.Value <= 0)
            return false;

        Suppressions.Value--;
        return true;
    }

    private static bool IsWaterAttack(CardModel? card)
    {
        return card?.GetType().FullName is
            "Neuvillette.Characters.Neuvillette.Cards.CaneStrike" or
            "Neuvillette.Characters.Neuvillette.Cards.Downpour" or
            "Neuvillette.Characters.Neuvillette.Cards.Indignation" or
            "Neuvillette.Characters.Neuvillette.Cards.Punishment" or
            "Neuvillette.Characters.Neuvillette.Cards.RagingFlurry" or
            "Neuvillette.Characters.Neuvillette.Cards.Silence" or
            "Neuvillette.Characters.Neuvillette.Cards.StrikeNeuvillette" or
            "Neuvillette.Characters.Neuvillette.Cards.SurgingTorrent" or
            "Neuvillette.Characters.Neuvillette.Cards.TimelyRain" or
            "Neuvillette.Characters.Neuvillette.Cards.WarmCurrent" or
            "Neuvillette.Characters.Neuvillette.Cards.WaterSplash";
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new Type[]
{
    typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(MegaCrit.Sts2.Core.ValueProps.ValueProp),
    typeof(Creature), typeof(CardModel), typeof(CardPlay)
})]
internal static class NeuvilletteWaterDamageAudioSingleTargetPatch
{
    public static void Prefix(Creature target, CardModel? cardSource)
    {
        NeuvilletteWaterDamageAudioState.Begin(cardSource, target.IsMonster ? 1 : 0);
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new Type[]
{
    typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal),
    typeof(MegaCrit.Sts2.Core.ValueProps.ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)
})]
internal static class NeuvilletteWaterDamageAudioMultiTargetPatch
{
    public static void Prefix(IEnumerable<Creature>? targets, CardModel? cardSource)
    {
        var monsterTargetCount = targets?.Count(target => target.IsMonster) ?? 0;
        NeuvilletteWaterDamageAudioState.Begin(cardSource, monsterTargetCount);
    }
}

[HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.PlayDamage), new Type[]
{
    typeof(MegaCrit.Sts2.Core.Models.MonsterModel), typeof(int)
})]
internal static class NeuvilletteWaterDamageAudioSuppressVanillaPatch
{
    public static bool Prefix()
    {
        return !NeuvilletteWaterDamageAudioState.Consume();
    }
}
