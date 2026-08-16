using Godot;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Neuvillette.Characters.Neuvillette.Vfx;

/// <summary>
/// A procedural courtroom stamp animation. The seal is deliberately parented to the card body,
/// so the game's normal exhaust VFX carries it away with the submitted card.
/// </summary>
public partial class NSubmitStampVfx : Control
{
    private static readonly Vector2 StampSize = new(184f, 292f);
    private static readonly Vector2 ImpactAnchor = new(92f, 254f);
    private static readonly Vector2 SealSize = new(136f, 136f);

    private static readonly Color WoodDark = new(0.075f, 0.055f, 0.07f, 1f);
    private static readonly Color Wood = new(0.20f, 0.105f, 0.09f, 1f);
    private static readonly Color WoodLight = new(0.42f, 0.20f, 0.13f, 1f);
    private static readonly Color BrassDark = new(0.34f, 0.23f, 0.08f, 1f);
    private static readonly Color Brass = new(0.78f, 0.60f, 0.22f, 1f);
    private static readonly Color BrassLight = new(1f, 0.86f, 0.46f, 1f);
    private static readonly Color Rubber = new(0.25f, 0.035f, 0.045f, 1f);

    public NSubmitStampVfx()
    {
        Name = nameof(NSubmitStampVfx);
        MouseFilter = MouseFilterEnum.Ignore;
        Size = StampSize;
        PivotOffset = ImpactAnchor;
        ZIndex = 500;
        ZAsRelative = false;
    }

    /// <summary>
    /// Presses the stamp onto <paramref name="cardNode"/> and leaves a persistent balance seal.
    /// </summary>
    public static async Task<bool> Play(NCard cardNode)
    {
        if (NCombatRoom.Instance?.Ui == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsInsideTree())
            return false;

        var sealPosition = GetSealPosition();
        var sealCenter = sealPosition + SealSize * 0.5f;
        var impactPosition = sealCenter - ImpactAnchor;
        var stamp = new NSubmitStampVfx
        {
            Position = impactPosition + Vector2.Up * 178f,
            Modulate = new Color(1f, 1f, 1f, 0f),
            Scale = new Vector2(0.96f, 1.04f)
        };
        cardNode.Body.AddChildSafely(stamp);
        stamp.Position = impactPosition + Vector2.Up * 178f;

        var descend = stamp.CreateTween().SetParallel();
        descend.TweenProperty(stamp, "position", impactPosition, 0.19f)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.In);
        descend.TweenProperty(stamp, "modulate", Colors.White, 0.07f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        descend.TweenProperty(stamp, "scale", Vector2.One, 0.19f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        if (!await descend.AwaitFinished(stamp))
            return false;

        if (!GodotObject.IsInstanceValid(cardNode) || !cardNode.IsInsideTree())
        {
            stamp.QueueFreeSafely();
            return false;
        }

        SfxCmd.Play(FmodSfx.cardImpactIntoSingle, 0.82f);

        var seal = new NSubmittedBalanceSeal
        {
            Size = SealSize,
            PivotOffset = SealSize * 0.5f,
            Position = sealPosition,
            RotationDegrees = -7.5f,
            Scale = Vector2.One * 0.68f,
            Modulate = new Color(1f, 1f, 1f, 0f),
            ZIndex = 100
        };
        cardNode.Body.AddChildSafely(seal);

        var burst = new NSubmittedSealBurst
        {
            Size = SealSize,
            PivotOffset = SealSize * 0.5f,
            Position = seal.Position,
            Rotation = seal.Rotation,
            Scale = Vector2.One * 0.72f,
            Modulate = new Color(1f, 1f, 1f, 0.64f),
            ZIndex = 99
        };
        cardNode.Body.AddChildSafely(burst);

        var impact = stamp.CreateTween().SetParallel();
        impact.TweenProperty(stamp, "scale", new Vector2(1.07f, 0.91f), 0.055f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        impact.TweenProperty(seal, "scale", Vector2.One * 1.08f, 0.085f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        impact.TweenProperty(seal, "modulate", Colors.White, 0.045f);
        impact.TweenProperty(burst, "scale", Vector2.One * 1.34f, 0.13f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        impact.TweenProperty(burst, "modulate", new Color(1f, 1f, 1f, 0f), 0.13f);

        if (!await impact.AwaitFinished(stamp))
            return false;

        burst.QueueFreeSafely();

        var settle = stamp.CreateTween().SetParallel();
        settle.TweenProperty(seal, "scale", Vector2.One, 0.07f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        settle.TweenProperty(stamp, "position", impactPosition + Vector2.Up * 92f, 0.17f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        settle.TweenProperty(stamp, "scale", new Vector2(0.98f, 1.02f), 0.17f);
        settle.TweenProperty(stamp, "modulate", new Color(1f, 1f, 1f, 0f), 0.14f)
            .SetDelay(0.03f);

        var completed = await settle.AwaitFinished(stamp);
        stamp.QueueFreeSafely();
        if (completed)
            await Cmd.Wait(0.08f);
        return completed;
    }

    private static Vector2 GetSealPosition() =>
        new Vector2(12f, 34f) - SealSize * 0.5f;

    public override void _Draw()
    {
        // Handle shadow and lacquered wood.
        DrawCircle(new Vector2(95f, 34f), 31f, new Color(0f, 0f, 0f, 0.30f));
        DrawRect(new Rect2(68f, 33f, 54f, 116f), WoodDark);
        DrawCircle(new Vector2(95f, 34f), 29f, Wood);
        DrawCircle(new Vector2(95f, 34f), 19f, WoodLight);
        DrawRect(new Rect2(73f, 36f, 44f, 111f), Wood);
        DrawRect(new Rect2(79f, 39f, 9f, 104f), new Color(WoodLight, 0.68f));

        // Brass collar.
        DrawRect(new Rect2(59f, 139f, 72f, 22f), BrassDark);
        DrawRect(new Rect2(63f, 137f, 64f, 18f), Brass);
        DrawRect(new Rect2(68f, 139f, 50f, 4f), BrassLight);

        // Heavy stamp head and red rubber face.
        DrawCircle(new Vector2(95f, 198f), 58f, new Color(0f, 0f, 0f, 0.28f));
        DrawRect(new Rect2(35f, 169f, 120f, 72f), WoodDark);
        DrawCircle(new Vector2(48f, 205f), 36f, WoodDark);
        DrawCircle(new Vector2(142f, 205f), 36f, WoodDark);
        DrawRect(new Rect2(42f, 174f, 106f, 60f), Wood);
        DrawRect(new Rect2(50f, 178f, 90f, 7f), new Color(WoodLight, 0.72f));
        DrawRect(new Rect2(28f, 231f, 134f, 19f), BrassDark);
        DrawRect(new Rect2(32f, 228f, 126f, 16f), Brass);
        DrawRect(new Rect2(39f, 230f, 112f, 4f), BrassLight);
        DrawRect(new Rect2(39f, 245f, 112f, 12f), Rubber);
    }
}

/// <summary>The ink mark that remains on the card until the card exhausts.</summary>
internal partial class NSubmittedBalanceSeal : Control
{
    private static readonly Color Ink = new(0.56f, 0.045f, 0.07f, 0.92f);
    private static readonly Color InkSoft = new(0.68f, 0.07f, 0.09f, 0.48f);

    public NSubmittedBalanceSeal()
    {
        Name = nameof(NSubmittedBalanceSeal);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _ExitTree()
    {
        // NCard is pooled rather than destroyed. A deferred check lets the seal survive the
        // temporary reparent into NCardExhaustVfx, but removes it once the card actually
        // leaves the scene tree for the pool so it cannot leak onto a later card visual.
        Callable.From(RemoveAfterCardReturnsToPool).CallDeferred();
    }

    private void RemoveAfterCardReturnsToPool()
    {
        if (!GodotObject.IsInstanceValid(this) || IsQueuedForDeletion())
            return;

        Node? ancestor = GetParent();
        while (ancestor != null && ancestor is not NCard)
            ancestor = ancestor.GetParent();

        if (ancestor is not NCard card || !card.IsInsideTree())
            this.QueueFreeSafelyNoPool();
    }

    public override void _Draw()
    {
        var center = Size * 0.5f;

        // Slightly imperfect double ring and fixed ink flecks keep the result stamp-like.
        DrawArc(center + new Vector2(1f, -1f), 59f, 0.08f, Mathf.Tau - 0.13f, 72, Ink, 5.5f, true);
        DrawArc(center + new Vector2(-1f, 1f), 51f, 0.20f, Mathf.Tau - 0.25f, 64, InkSoft, 2.8f, true);
        DrawArc(center, 46f, 0.34f, 2.45f, 30, Ink, 2.2f, true);
        DrawArc(center, 46f, 3.02f, 5.92f, 38, Ink, 2.2f, true);

        // Balance scale: crown, post, beam, chains, and pans.
        DrawCircle(center + new Vector2(0f, -29f), 5.5f, Ink);
        DrawLine(center + new Vector2(0f, -25f), center + new Vector2(0f, 31f), Ink, 5.5f, true);
        DrawLine(center + new Vector2(-34f, -17f), center + new Vector2(34f, -17f), Ink, 5f, true);
        DrawCircle(center + new Vector2(-34f, -17f), 3.2f, Ink);
        DrawCircle(center + new Vector2(34f, -17f), 3.2f, Ink);

        DrawLine(center + new Vector2(-34f, -14f), center + new Vector2(-46f, 10f), Ink, 2.6f, true);
        DrawLine(center + new Vector2(-34f, -14f), center + new Vector2(-22f, 10f), Ink, 2.6f, true);
        DrawLine(center + new Vector2(34f, -14f), center + new Vector2(22f, 10f), Ink, 2.6f, true);
        DrawLine(center + new Vector2(34f, -14f), center + new Vector2(46f, 10f), Ink, 2.6f, true);
        DrawLine(center + new Vector2(-47f, 10f), center + new Vector2(-21f, 10f), Ink, 3f, true);
        DrawLine(center + new Vector2(21f, 10f), center + new Vector2(47f, 10f), Ink, 3f, true);
        DrawArc(center + new Vector2(-34f, 9f), 13f, 0f, Mathf.Pi, 20, Ink, 4f, true);
        DrawArc(center + new Vector2(34f, 9f), 13f, 0f, Mathf.Pi, 20, Ink, 4f, true);
        DrawLine(center + new Vector2(-17f, 32f), center + new Vector2(17f, 32f), Ink, 5f, true);
        DrawLine(center + new Vector2(-11f, 26f), center + new Vector2(11f, 26f), Ink, 3f, true);

        DrawCircle(center + new Vector2(-52f, -31f), 2.2f, InkSoft);
        DrawCircle(center + new Vector2(49f, 34f), 1.8f, InkSoft);
        DrawCircle(center + new Vector2(-42f, 39f), 1.5f, InkSoft);
        DrawCircle(center + new Vector2(37f, -43f), 1.4f, InkSoft);
    }
}

/// <summary>A short expanding ink ring shown only at the moment of impact.</summary>
internal partial class NSubmittedSealBurst : Control
{
    private static readonly Color BurstInk = new(0.78f, 0.08f, 0.12f, 0.74f);

    public NSubmittedSealBurst()
    {
        Name = nameof(NSubmittedSealBurst);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        var center = Size * 0.5f;
        DrawArc(center, 61f, 0f, Mathf.Tau, 72, BurstInk, 4f, true);
        DrawArc(center, 55f, 0.15f, Mathf.Tau - 0.12f, 64, new Color(BurstInk, 0.48f), 2f, true);
    }
}
