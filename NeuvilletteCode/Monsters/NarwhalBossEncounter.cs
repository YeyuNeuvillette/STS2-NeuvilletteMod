using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Neuvillette.Characters.Neuvillette.Act;

namespace Neuvillette.Monsters;

[RegisterActEncounter(typeof(NeuvilletteAct))]
public class NarwhalBossEncounter : ModEncounterTemplate
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<AllDevouringNarwhal>()];

    public override RoomType RoomType => RoomType.Boss;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        [(ModelDb.Monster<AllDevouringNarwhal>().ToMutable(), null)];
}