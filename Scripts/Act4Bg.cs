using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Neuvillette.Scripts;

internal partial class Act4Bg : NCombatBackground
{
    private Sprite2D? _normalSprite;
    private Sprite2D? _bellySprite;

    public override void _Ready()
    {
        base._Ready();

        var layer00 = GetNode<Control>("Layer_00");
        _normalSprite = layer00.GetNode<Sprite2D>("Act4BgSprite");
        _bellySprite = layer00.GetNode<Sprite2D>("Act4BgBellySprite");
    }

    public void SetBellyState(bool isInBelly)
    {
        if (_normalSprite == null || _bellySprite == null)
            return;

        _normalSprite.Visible = !isInBelly;
        _bellySprite.Visible = isInBelly;
    }
}