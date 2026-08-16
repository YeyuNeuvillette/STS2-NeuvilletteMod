using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace Neuvillette.Scripts;

public partial class DownpourRainVfx : GpuParticles2D
{
    public const string ScenePath = "res://scenes/vfx/downpour_rain.tscn";
    public const float Duration = 3.2f;
    private CancellationTokenSource? _cts;

    public override void _ExitTree() => _cts?.Cancel();

    public static DownpourRainVfx? Create()
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        return MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<DownpourRainVfx>(PackedScene.GenEditState.Disabled);
    }

    public override void _Ready() => TaskHelper.RunSafely(PlaySequence());

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();
        Restart();
        await Cmd.Wait(Duration, _cts.Token);
        Emitting = false;
        this.QueueFreeSafely();
    }
}
