using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Neuvillette.Scripts;

internal partial class NarwhalCreatureVisuals : NCreatureVisuals
{
    private Sprite2D? _normalSprite;
    private Sprite2D? _bellySprite;

    public override void _Ready()
    {
        base._Ready();

        var visuals = GetNode<Node2D>("%Visuals");
        _normalSprite = visuals.GetNode<Sprite2D>("AllDevouringNarwhal");
        _bellySprite = visuals.GetNode<Sprite2D>("???");
    }

    public void SetBellyState(bool isInBelly)
    {
        if (_normalSprite == null || _bellySprite == null)
            return;

        _normalSprite.Visible = !isInBelly;
        _bellySprite.Visible = isInBelly;
    }
}