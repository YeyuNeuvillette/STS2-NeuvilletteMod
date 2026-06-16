using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;
using Neuvillette.Characters.Neuvillette.Powers;
using Neuvillette.Extensions;

namespace Neuvillette.Characters.Neuvillette;

[RegisterOrb]
public sealed class OceanOrb : ModOrbTemplate
{
    public override decimal PassiveVal => 2m;
    public override decimal EvokeVal => 8m;
    private string OrbAssetFileName => Id.Entry.ToOrbArtFileName();
    private string LegacyOrbAssetFileName => nameof(OceanOrb).ToLegacyCompactFileName();

    public override Color DarkenedColor => new(0.1f, 0.2f, 0.5f);
    public override OrbAssetProfile AssetProfile => new(
        ResolveExistingIconPath(),
        ResolveExistingScenePath());

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();

        var surgeAmount = GetModifiedSurgeValue((int)PassiveVal);
        await CreatureCmd.Heal(Owner.Creature, surgeAmount);
        await PowerCmd.Apply<SurgePower>(choiceContext, Owner.Creature, surgeAmount, Owner.Creature, null);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext choiceContext)
    {
        PlayEvokeSfx();

        var surgeAmount = GetModifiedSurgeValue(8);
        await CreatureCmd.Heal(Owner.Creature, surgeAmount);
        await PowerCmd.Apply<SurgePower>(choiceContext, Owner.Creature, surgeAmount, Owner.Creature, null);
        return [];
    }

    private string ResolveExistingIconPath()
    {
        var candidates = new[]
        {
            $"{OrbAssetFileName}.svg".OrbImgPath(),
            $"{LegacyOrbAssetFileName}.svg".OrbImgPath(),
        };

        foreach (var candidate in candidates)
        {
            if (GodotResourcePath.ResourceExists(candidate))
                return candidate;
        }

        return candidates[0];
    }

    private string ResolveExistingScenePath()
    {
        var candidates = new[]
        {
            $"{OrbAssetFileName}.tscn".OrbScenePath(),
            $"{LegacyOrbAssetFileName}.tscn".OrbScenePath(),
        };

        foreach (var candidate in candidates)
        {
            if (GodotResourcePath.ResourceExists(candidate))
                return candidate;
        }

        return candidates[0];
    }

    private int GetModifiedSurgeValue(int baseValue)
    {
        var value = baseValue;
        foreach (var power in Owner.Creature.Powers)
        {
            if (power is LivingWaterPower livingWaterPower)
                value += (int)livingWaterPower.Amount;
        }

        return value;
    }
}