using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace Neuvillette.Scripts;

public partial class NoSfxHyperbeamImpactVfx : Node2D
{
    public static readonly string ScenePath = "res://scenes/vfx/vfx_hyperbeam_impact_no_sfx.tscn";

    [Export] private Array<GpuParticles2D> _impactStartParticles = new();
    [Export] private Array<GpuParticles2D> _impactEndParticles = new();
    private CancellationTokenSource? _cts;

    public override void _ExitTree() => _cts?.Cancel();

    public static NoSfxHyperbeamImpactVfx? Create(Creature owner, Creature target)
    {
        if (TestMode.IsOn) return null;
        var source = NCombatRoom.Instance?.GetCreatureNode(owner);
        var destination = NCombatRoom.Instance?.GetCreatureNode(target);
        if (source == null || destination == null) return null;

        var origin = source.VfxSpawnPosition;
        if (owner.Player?.Character is MegaCrit.Sts2.Core.Models.Characters.Defect)
            origin += MegaCrit.Sts2.Core.Models.Characters.Defect.EyelineOffset;
        return Create(origin, destination.VfxSpawnPosition);
    }

    public static NoSfxHyperbeamImpactVfx? Create(Vector2 source, Vector2 target)
    {
        if (TestMode.IsOn) return null;
        var vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NoSfxHyperbeamImpactVfx>(PackedScene.GenEditState.Disabled);
        vfx.GlobalPosition = target;
        vfx.RotationDegrees = Mathf.RadToDeg((target - source).Angle());
        return vfx;
    }

    public override void _Ready() => TaskHelper.RunSafely(PlaySequence());

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();
        foreach (var particle in _impactStartParticles)
        {
            particle.Visible = true;
            particle.Restart();
        }
        await Cmd.Wait(NoSfxHyperbeamVfx.LaserDuration, _cts.Token);
        foreach (var particle in _impactStartParticles) particle.Visible = false;
        foreach (var particle in _impactEndParticles) particle.Restart();
        await Cmd.Wait(2f, _cts.Token);
        this.QueueFreeSafely();
    }
}
