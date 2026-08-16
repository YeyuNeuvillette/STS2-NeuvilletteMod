using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace Neuvillette.Scripts;

public partial class NeuvilletteAttackVfx : Node2D
{
    public const string ScenePath = "res://scenes/vfx/neuvillette_attack_vfx.tscn";
    private const float FrameDuration = 0.03f;
    private const float AnimationScale = 0.9f;
    private const int FrameCount = 17;

    private Sprite2D? _sprite;
    private CancellationTokenSource? _cts;

    public override void _ExitTree() => _cts?.Cancel();

    public static NeuvilletteAttackVfx? Create(Creature target)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null)
        {
            return null;
        }

        var vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NeuvilletteAttackVfx>(PackedScene.GenEditState.Disabled);
        vfx.GlobalPosition = targetNode.VfxSpawnPosition;
        return vfx;
    }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Frame");
        _sprite.Scale = Vector2.One * AnimationScale;
        TaskHelper.RunSafely(PlaySequence());
    }

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();

        for (var index = 0; index < FrameCount; index++)
        {
            var path = $"res://Neuvillette/images/characters/Neuvillette/attack/action-action_{index:00}.png";
            _sprite!.Texture = PreloadManager.Cache.GetTexture2D(path);
            await Cmd.Wait(FrameDuration, _cts.Token);
        }

        this.QueueFreeSafely();
    }
}
