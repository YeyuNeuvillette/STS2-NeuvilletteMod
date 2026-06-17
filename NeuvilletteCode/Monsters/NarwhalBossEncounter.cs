using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Neuvillette.Characters.Neuvillette.Act;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace Neuvillette.Monsters;

[RegisterActEncounter(typeof(NeuvilletteAct))]
public class NarwhalBossEncounter : ModEncounterTemplate
{
    private static readonly string IconBasePath = "res://Neuvillette/images/map/all_devouring_narwhal_boss_icon";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<AllDevouringNarwhal>()];

    public override RoomType RoomType => RoomType.Boss;

    public override string CustomBgm => "event:/Neuvillette/music/AllDevouringNarwhalTheme";

    public override string BossNodePath => IconBasePath;

    public override MegaSkeletonDataResource? BossNodeSpineResource => null;

    public override EncounterAssetProfile AssetProfile => new(
        RunHistoryIconPath: IconBasePath + ".png",
        RunHistoryIconOutlinePath: IconBasePath + "_outline.png"
    );

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        [(ModelDb.Monster<AllDevouringNarwhal>().ToMutable(), null)];
}