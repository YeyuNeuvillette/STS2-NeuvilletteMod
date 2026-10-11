using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace Neuvillette.Scripts;

/// <summary>
/// A target-centered rendition of Final Judgment's card art: a bronze judicial
/// hammer descends through a pale courtroom spotlight and detonates into gold
/// and Hydro-colored shards on impact.
/// </summary>
public partial class FinalJudgmentVfx : Node2D
{
    private const float AnticipationDuration = 0.24f;
    private const float StrikeDuration = 0.13f;
    private const float ImpactLeadDuration = 0.06f;
    private const float CleanupDuration = 0.48f;

    private CancellationTokenSource? _cts;
    private JudgmentBackdropVisual? _backdrop;
    private Node2D? _gavelPivot;
    private JudgmentGavelVisual? _gavel;
    private JudgmentImpactVisual? _impact;

    public override void _ExitTree() => _cts?.Cancel();

    /// <summary>
    /// Plays through the instant of impact. The remaining flash fades on its own,
    /// allowing the card effect to resolve while the impact is still visible.
    /// </summary>
    public static async Task Play(Creature target)
    {
        if (TestMode.IsOn)
            return;

        var room = NCombatRoom.Instance;
        var targetNode = room?.GetCreatureNode(target);
        if (room == null || targetNode == null)
            return;

        var vfx = new FinalJudgmentVfx
        {
            Name = nameof(FinalJudgmentVfx),
            GlobalPosition = targetNode.VfxSpawnPosition,
            ZAsRelative = false,
            ZIndex = 100
        };
        room.CombatVfxContainer.AddChildSafely(vfx);
        await vfx.PlayUntilImpact();
    }

    private async Task PlayUntilImpact()
    {
        _cts = new CancellationTokenSource();

        _backdrop = new JudgmentBackdropVisual
        {
            Modulate = new Color(1f, 1f, 1f, 0f),
            Scale = new Vector2(0.88f, 0.88f)
        };
        AddChild(_backdrop);

        const float initialRotation = 55f;
        const float anticipationRotation = 45f;
        const float impactRotation = 0f;
        const float gavelScale = 1.04f;

        var impactContactOffset = JudgmentGavelVisual.ContactPointFromPivot
            .Rotated(Mathf.DegToRad(impactRotation)) * gavelScale;
        var impactPivotPosition = -impactContactOffset;

        // All animation is applied to this pivot at the middle of the handle.
        // The rendered hammer is a fixed child, so the head and handle can only
        // move as one rigid body and can never appear to hinge independently.
        _gavelPivot = new Node2D
        {
            // The grip does not translate during the swing. The hammer head
            // follows a clean circular arc around this fixed point.
            Position = impactPivotPosition,
            RotationDegrees = initialRotation,
            Scale = new Vector2(gavelScale, gavelScale),
            Modulate = new Color(1f, 1f, 1f, 0f)
        };
        AddChild(_gavelPivot);

        _gavel = new JudgmentGavelVisual
        {
            // Move the artwork so the parent origin lies at the handle center.
            Position = -JudgmentGavelVisual.GripPointInArtwork
        };
        _gavelPivot.AddChild(_gavel);

        var anticipation = CreateTween().SetParallel();
        anticipation.TweenProperty(_backdrop, "modulate", new Color(1f, 1f, 1f, 0.82f), AnticipationDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        anticipation.TweenProperty(_backdrop, "scale", Vector2.One, AnticipationDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        anticipation.TweenProperty(_gavelPivot, "modulate", Colors.White, 0.1f);
        anticipation.TweenProperty(_gavelPivot, "rotation_degrees", anticipationRotation, AnticipationDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await Cmd.Wait(AnticipationDuration, _cts.Token);

        var strike = CreateTween();
        // A single rigid rotation around the stationary grip produces the
        // entire strike. No concurrent translation or scaling distorts the arc.
        strike.TweenProperty(_gavelPivot, "rotation_degrees", impactRotation, StrikeDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        await Cmd.Wait(StrikeDuration, _cts.Token);

        _impact = new JudgmentImpactVisual
        {
            Scale = new Vector2(0.28f, 0.28f),
            Modulate = Colors.White
        };
        AddChild(_impact);

        SfxCmd.Play("heavy_attack.mp3", 1.05f);
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Normal, 90f);
        NGame.Instance?.DoHitStop(ShakeStrength.Medium, ShakeDuration.Short);

        var burst = CreateTween().SetParallel();
        burst.TweenProperty(_impact, "scale", new Vector2(1.32f, 1.32f), 0.22f)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.Out);
        burst.TweenProperty(_impact, "modulate", new Color(1f, 1f, 1f, 0f), 0.38f)
            .SetDelay(0.1f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        var rebound = CreateTween().SetParallel();
        rebound.TweenProperty(_gavelPivot, "rotation_degrees", 10f, 0.18f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        rebound.TweenProperty(_gavelPivot, "modulate", new Color(1f, 1f, 1f, 0f), 0.27f)
            .SetDelay(0.12f);

        var backdropFade = CreateTween();
        backdropFade.TweenProperty(_backdrop, "modulate", new Color(1f, 1f, 1f, 0f), 0.34f)
            .SetDelay(0.08f);

        _ = TaskHelper.RunSafely(CleanupAfterImpact());
        await Cmd.Wait(ImpactLeadDuration, _cts.Token);
    }

    private async Task CleanupAfterImpact()
    {
        if (_cts == null)
            return;

        await Cmd.Wait(CleanupDuration, _cts.Token);
        this.QueueFreeSafely();
    }
}

internal partial class JudgmentBackdropVisual : Node2D
{
    private static readonly Color CyanBeam = new(0.56f, 0.92f, 1f, 0.17f);
    private static readonly Color GoldBeam = new(1f, 0.88f, 0.42f, 0.13f);

    public override void _Draw()
    {
        DrawColoredPolygon(
            [new Vector2(-115f, -520f), new Vector2(105f, -520f), new Vector2(220f, 75f), new Vector2(-230f, 75f)],
            CyanBeam);
        DrawColoredPolygon(
            [new Vector2(-42f, -520f), new Vector2(42f, -520f), new Vector2(104f, 58f), new Vector2(-108f, 58f)],
            GoldBeam);

        DrawArc(Vector2.Zero, 92f, 0f, Mathf.Tau, 72, new Color(0.48f, 0.9f, 1f, 0.42f), 3f, true);
        DrawArc(Vector2.Zero, 126f, 0.18f, Mathf.Tau - 0.24f, 72, new Color(1f, 0.79f, 0.26f, 0.34f), 5f, true);

        DrawCircle(new Vector2(-148f, -128f), 7f, new Color(0.42f, 0.88f, 1f, 0.52f));
        DrawCircle(new Vector2(164f, -176f), 5f, new Color(1f, 0.86f, 0.42f, 0.58f));
        DrawCircle(new Vector2(-196f, -248f), 4f, new Color(1f, 0.9f, 0.55f, 0.48f));
        DrawCircle(new Vector2(206f, -74f), 6f, new Color(0.45f, 0.9f, 1f, 0.45f));
    }
}

internal partial class JudgmentGavelVisual : Node2D
{
    public static readonly Vector2 GripPointInArtwork = new(320f, 0f);
    public static readonly Vector2 ContactPointFromPivot = new(-330f, 127f);

    private static readonly Color BronzeDark = new("6a371f");
    private static readonly Color Bronze = new("b86f32");
    private static readonly Color BronzeLight = new("f0ad52");
    private static readonly Color Engraving = new("7d4526");

    public override void _Draw()
    {
        // The base geometry is orthogonal: vertical head, perfectly horizontal
        // handle. Any tilt seen on screen therefore comes only from the shared
        // rigid-body rotation, never from a skewed handle mesh.
        DrawRect(new Rect2(60f, -21f, 260f, 42f), BronzeDark);
        DrawRect(new Rect2(66f, -14f, 254f, 28f), Bronze);
        DrawLine(new Vector2(75f, -7f), new Vector2(309f, -7f), BronzeLight, 5f, true);
        DrawCircle(GripPointInArtwork, 22f, BronzeDark);
        DrawCircle(GripPointInArtwork, 16f, Bronze);

        // Tall, vertically weighted hammer head matching the card illustration.
        DrawColoredPolygon(
            [new Vector2(-70f, -126f), new Vector2(45f, -126f), new Vector2(70f, -105f), new Vector2(73f, 102f), new Vector2(49f, 127f), new Vector2(-70f, 127f), new Vector2(-94f, 103f), new Vector2(-94f, -104f)],
            BronzeDark);
        DrawColoredPolygon(
            [new Vector2(-62f, -113f), new Vector2(38f, -113f), new Vector2(57f, -96f), new Vector2(60f, 94f), new Vector2(42f, 113f), new Vector2(-62f, 113f), new Vector2(-80f, 94f), new Vector2(-80f, -96f)],
            Bronze);
        DrawColoredPolygon(
            [new Vector2(-60f, -106f), new Vector2(33f, -106f), new Vector2(47f, -94f), new Vector2(-68f, -94f)],
            BronzeLight);
        DrawRect(new Rect2(-101f, -111f, 22f, 220f), BronzeDark);
        DrawRect(new Rect2(59f, -111f, 22f, 220f), BronzeDark);
        DrawRect(new Rect2(-87f, -92f, 153f, 18f), BronzeDark);
        DrawRect(new Rect2(-87f, 76f, 157f, 21f), BronzeDark);
        DrawLine(new Vector2(-56f, 103f), new Vector2(37f, 103f), BronzeLight, 5f, true);

        // A centered socket joins the straight handle to the middle of the head.
        DrawColoredPolygon(
            [new Vector2(42f, -32f), new Vector2(72f, -35f), new Vector2(112f, -28f), new Vector2(128f, -12f), new Vector2(126f, 5f), new Vector2(110f, 19f), new Vector2(74f, 26f), new Vector2(44f, 23f)],
            BronzeDark);
        DrawColoredPolygon(
            [new Vector2(51f, -23f), new Vector2(71f, -25f), new Vector2(105f, -19f), new Vector2(116f, -9f), new Vector2(115f, 1f), new Vector2(104f, 10f), new Vector2(73f, 17f), new Vector2(52f, 15f)],
            Bronze);
        DrawLine(new Vector2(77f, -24f), new Vector2(108f, -17f), BronzeLight, 5f, true);

        // Simplified balance, sword, and shield engravings from the card art.
        DrawLine(new Vector2(-54f, -46f), new Vector2(-54f, 4f), Engraving, 5f, true);
        DrawLine(new Vector2(-73f, -34f), new Vector2(-35f, -34f), Engraving, 5f, true);
        DrawLine(new Vector2(-70f, -31f), new Vector2(-77f, -11f), Engraving, 3f, true);
        DrawLine(new Vector2(-38f, -31f), new Vector2(-31f, -11f), Engraving, 3f, true);
        DrawArc(new Vector2(-73f, -13f), 7f, 0f, Mathf.Pi, 16, Engraving, 3f, true);
        DrawArc(new Vector2(-35f, -13f), 7f, 0f, Mathf.Pi, 16, Engraving, 3f, true);

        DrawLine(new Vector2(-12f, -55f), new Vector2(-12f, 20f), Engraving, 6f, true);
        DrawLine(new Vector2(-24f, -39f), new Vector2(0f, -39f), Engraving, 5f, true);
        DrawColoredPolygon(
            [new Vector2(23f, -49f), new Vector2(49f, -38f), new Vector2(47f, 10f), new Vector2(24f, 28f), new Vector2(2f, 10f), new Vector2(0f, -38f)],
            Engraving);
        DrawColoredPolygon(
            [new Vector2(24f, -38f), new Vector2(40f, -31f), new Vector2(38f, 4f), new Vector2(24f, 16f), new Vector2(10f, 4f), new Vector2(8f, -31f)],
            BronzeLight);
    }
}

internal partial class JudgmentImpactVisual : Node2D
{
    private static readonly Color Gold = new("ffd45f");
    private static readonly Color GoldLight = new("fff4bd");
    private static readonly Color Hydro = new("55d9ff");
    private static readonly Color HydroDark = new("2477c9");

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 48f, new Color(1f, 0.96f, 0.72f, 0.9f));
        DrawCircle(Vector2.Zero, 24f, Colors.White);
        DrawArc(Vector2.Zero, 74f, 0f, Mathf.Tau, 64, new Color(Hydro, 0.8f), 8f, true);
        DrawArc(Vector2.Zero, 106f, 0.12f, Mathf.Tau - 0.16f, 72, new Color(Gold, 0.72f), 5f, true);

        for (var i = 0; i < 16; i++)
        {
            var angle = Mathf.Tau * i / 16f;
            var direction = Vector2.FromAngle(angle);
            var tangent = direction.Orthogonal();
            var inner = 38f + i % 3 * 5f;
            var outer = 145f + i % 4 * 28f;
            var halfWidth = 7f + i % 2 * 4f;
            var color = i % 2 == 0 ? Gold : Hydro;
            DrawColoredPolygon(
                [direction * inner - tangent * halfWidth, direction * outer, direction * inner + tangent * halfWidth],
                color);
        }

        for (var i = 0; i < 8; i++)
        {
            var angle = Mathf.Tau * (i + 0.5f) / 8f;
            var direction = Vector2.FromAngle(angle);
            var tangent = direction.Orthogonal();
            var center = direction * (112f + i % 3 * 18f);
            var color = i % 2 == 0 ? GoldLight : HydroDark;
            DrawColoredPolygon(
                [center - tangent * 7f - direction * 10f, center + tangent * 8f, center + direction * 21f],
                color);
        }

        DrawLine(new Vector2(-188f, 54f), new Vector2(188f, 54f), new Color(Gold, 0.72f), 8f, true);
        DrawLine(new Vector2(-142f, 69f), new Vector2(142f, 69f), new Color(Hydro, 0.6f), 5f, true);
    }
}
