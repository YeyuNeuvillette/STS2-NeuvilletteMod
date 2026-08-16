using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace Neuvillette.Scripts;

public partial class NoSfxHyperbeamVfx : Node2D
{
    public static readonly string ScenePath = "res://scenes/vfx/vfx_hyperbeam_no_sfx.tscn";

    [Export] private Array<GpuParticles2D> _anticipationParticles = new();
    [Export] private Array<GpuParticles2D> _laserParticles = new();
    [Export] private Array<GpuParticles2D> _laserEndParticles = new();
    [Export] private Line2D? _laserLine;
    [Export] private Node2D? _laserContainer;

    public const float AnticipationDuration = 0.525f;
    public const float LaserDuration = 0.5f;
    private CancellationTokenSource? _cts;

    public override void _ExitTree() => _cts?.Cancel();

    public static NoSfxHyperbeamVfx? Create(Creature owner, Creature target)
    {
        if (TestMode.IsOn) return null;
        var source = NCombatRoom.Instance?.GetCreatureNode(owner);
        var destination = NCombatRoom.Instance?.GetCreatureNode(target);
        if (source == null || destination == null) return null;

        var origin = source.VfxSpawnPosition;
        if (owner.Player?.Character is Defect) origin += Defect.EyelineOffset;
        return Create(origin, destination.VfxSpawnPosition);
    }

    public static NoSfxHyperbeamVfx? Create(Vector2 source, Vector2 target)
    {
        if (TestMode.IsOn) return null;
        var vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NoSfxHyperbeamVfx>(PackedScene.GenEditState.Disabled);
        vfx.GlobalPosition = source;
        vfx.RotationDegrees = Mathf.RadToDeg((target - source).Angle());
        return vfx;
    }

    public override void _Ready() => TaskHelper.RunSafely(PlaySequence());

    private void ShowLaser(bool showing)
    {
        foreach (var particle in _laserParticles)
        {
            particle.Visible = showing;
            if (showing) particle.Restart();
        }
        if (_laserLine != null) _laserLine.Visible = showing;
        if (_laserContainer != null) _laserContainer.Visible = showing;
    }

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();
        ShowLaser(false);
        foreach (var particle in _anticipationParticles) particle.Restart();
        await Cmd.Wait(AnticipationDuration, _cts.Token);
        ShowLaser(true);
        NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Normal);
        await Cmd.Wait(LaserDuration, _cts.Token);
        ShowLaser(false);
        foreach (var particle in _laserEndParticles) particle.Restart();
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
        await Cmd.Wait(2f, _cts.Token);
        this.QueueFreeSafely();
    }
}
