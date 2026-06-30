using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Events;
using Neuvillette.Characters.Neuvillette.Act;
using Neuvillette.Characters.Neuvillette.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace Neuvillette.Characters.Neuvillette.Ancients;

[RegisterActAncient(typeof(NeuvilletteAct))]
public class ArchitectAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.15f, 0.12f, 0.08f, 0.75f);
    public override Color DialogueColor => new Color("3D2E1A");

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<Gavel>(),
        CreateModRelicOption<Monocle>(),
        CreateModRelicOption<StoppedPocketWatch>(),
        CreateModRelicOption<StatueFragment>(),
        CreateModRelicOption<ShatteredCrown>(),
        CreateModRelicOption<Plumule>(),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var pool1 = new[]
        {
            CreateModRelicOption<Gavel>(),
            CreateModRelicOption<Monocle>(),
        };

        var pool2 = new[]
        {
            CreateModRelicOption<StoppedPocketWatch>(),
            CreateModRelicOption<StatueFragment>(),
        };

        var pool3 = new WeightedList<EventOption>
        {
            { CreateModRelicOption<ShatteredCrown>(), 2 },
            { CreateModRelicOption<Plumule>(), 1 },
        };

        return
        [
            Rng.NextItem(pool1)!,
            Rng.NextItem(pool2)!,
            pool3.GetRandom(Rng),
        ];
    }
}