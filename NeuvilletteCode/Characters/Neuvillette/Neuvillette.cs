using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using Neuvillette.Extensions;

namespace Neuvillette.Characters.Neuvillette;

[RegisterCharacter]
public class Neuvillette : ModCharacterTemplate<NeuvilletteCardPool, NeuvilletteRelicPool, NeuvillettePotionPool>
{
    public override bool RequiresEpochAndTimeline => false;
    public const string CharacterId = "Neuvillette";
    public static readonly Color Color = new("4096ee");

    public override Color NameColor => Color;
    public override Color MapDrawingColor => new("#053f95");
    public override Color EnergyLabelOutlineColor => new(0.1f, 0.1f, 1f);
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 50;
    public override int StartingGold => 99;

    public override CharacterAssetProfile AssetProfile => new(
        new CharacterSceneAssetSet(
            null,
            "neuvillette_energy_counter.tscn".CharacterScenePath(CharacterId),
            "neuvillette_merchant.tscn".CharacterScenePath(CharacterId),
            null),
        new CharacterUiAssetSet(
            "neuvillette_map.png".CharacterImgPath(CharacterId),
            null,
            "neuvillette_icon.tscn".CharacterScenePath(CharacterId),
            "neuvillette_bg.tscn".CharacterScenePath(CharacterId),
            "neuvillette_char_select.png".CharacterImgPath(CharacterId),
            "neuvillette_char_select_locked.png".CharacterImgPath(CharacterId),
            null,
            "neuvillette_map.png".CharacterImgPath(CharacterId)),
        Audio: new CharacterAudioAssetSet(
            CharacterSelectSfx: "event:/Neuvillette/sfx/Select"
        ),
        Multiplayer: new CharacterMultiplayerAssetSet(
            "multiplayer_hand_point.png".CharacterImgPath(CharacterId),
            "multiplayer_hand_rock.png".CharacterImgPath(CharacterId),
            "multiplayer_hand_paper.png".CharacterImgPath(CharacterId),
            "multiplayer_hand_scissors.png".CharacterImgPath(CharacterId))
            );

    public override CharacterWorldProceduralVisualSet? WorldProceduralVisuals =>
        CharacterWorldProceduralVisualSetBuilder.Create()
            .RestSite(builder => builder
                .Single("overgrowth_loop", "neuvillette_rest_site.png".CharacterImgPath(CharacterId))
                .Single("hive_loop", "neuvillette_rest_site.png".CharacterImgPath(CharacterId))
                .Single("glory_loop", "neuvillette_rest_site.png".CharacterImgPath(CharacterId)))
            .Build();

    public override string? PlaceholderCharacterId => "ironclad";
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;

    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
     }

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            "neuvillette.tscn".CharacterScenePath(CharacterId));
    }
}