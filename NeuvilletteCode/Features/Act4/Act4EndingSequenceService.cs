using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace Neuvillette.Features.Act4;

internal static class Act4EndingSequenceService
{
    private static Task? _activeSequence;

    public static Task PlayThenWinAsync(RunManager runManager)
    {
        if (_activeSequence is { IsCompleted: false })
            return _activeSequence;

        _activeSequence = PlayThenWinCoreAsync(runManager);
        return _activeSequence;
    }

    private static async Task PlayThenWinCoreAsync(RunManager runManager)
    {
        Act4EndingSequenceOverlay? overlay = null;
        try
        {
            var game = NGame.Instance;
            if (game == null)
            {
                MainFile.Logger.Warn("[Act4 Ending] NGame is unavailable; skipping ending CG sequence.");
            }
            else
            {
                overlay = Act4EndingSequenceOverlay.Create();
                game.AddChild(overlay);
                await overlay.PlayAsync();
            }
        }
        catch (Exception exception)
        {
            // The CG is presentation-only. Never allow a missing/corrupt asset to block victory.
            MainFile.Logger.Error($"[Act4 Ending] Ending CG sequence failed; continuing to victory. {exception}");
            overlay?.QueueFree();
            overlay = null;
        }

        await runManager.WinRun();

        if (overlay != null && GodotObject.IsInstanceValid(overlay))
            await overlay.RevealVictoryAndDisposeAsync();
    }
}

internal sealed partial class Act4EndingSequenceOverlay : CanvasLayer
{
    private const float BackdropFadeSeconds = 0.7f;
    private const float PanelFadeSeconds = 0.9f;
    private const float ComicFadeOutSeconds = 0.75f;
    private const float VictoryRevealSeconds = 0.7f;
    private const double PanelDurationSeconds = 3.6;
    private const double InputGuardSeconds = 0.3;

    private const string ComicMasterPath =
        "res://Neuvillette/images/ui/ending_cg/act4_ending_comic_master_v5.png";

    private const string PanelMaskShaderCode = """
        shader_type canvas_item;

        uniform int panel_index = 0;
        varying vec4 canvas_item_modulate;

        void vertex() {
            // Capture the CanvasItem/parent modulation before Godot combines
            // COLOR with the texture for the fragment stage.
            canvas_item_modulate = COLOR;
        }

        void fragment() {
            // Sample the texture exactly once. Reading fragment COLOR here would
            // multiply it by Godot's implicit texture sample and create ghosting.
            vec4 source = texture(TEXTURE, UV) * canvas_item_modulate;

            // Measured from the v5 master texture (1672 x 941). Both cuts follow
            // the centers of the deliberately angled black comic gutters.
            float right_gutter_center = 0.67255 - 0.1075 * UV.y;
            float diagonal_gutter_center = 0.6328 - 0.3057 * UV.x;
            bool in_left_column = UV.x < right_gutter_center;
            bool visible = false;

            if (panel_index == 0) {
                visible = in_left_column && UV.y < diagonal_gutter_center;
            } else if (panel_index == 1) {
                visible = in_left_column && UV.y >= diagonal_gutter_center;
            } else {
                visible = !in_left_column;
            }

            COLOR = visible ? source : vec4(0.0);
        }
        """;

    private readonly Control _root = new();
    private readonly ColorRect _backdrop = new();
    private readonly Control _comicLayer = new();
    private readonly TextureRect[] _panels = new TextureRect[3];
    private TaskCompletionSource? _advance;
    private double _remainingSeconds;
    private double _inputGuardSeconds;
    private bool _isPlaying;

    private Act4EndingSequenceOverlay()
    {
        Name = "NeuvilletteAct4EndingSequence";
        Layer = 1000;
        ProcessMode = ProcessModeEnum.Always;

        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _backdrop.Color = new Color(0f, 0f, 0f, 0f);
        _backdrop.MouseFilter = Control.MouseFilterEnum.Stop;
        _backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_backdrop);

        _comicLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
        _comicLayer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_comicLayer);

        var maskShader = new Shader { Code = PanelMaskShaderCode };
        for (var index = 0; index < _panels.Length; index++)
        {
            var material = new ShaderMaterial { Shader = maskShader };
            material.SetShaderParameter("panel_index", index);

            var panel = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = new Color(1f, 1f, 1f, 0f),
                Material = material
            };
            panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _comicLayer.AddChild(panel);
            _panels[index] = panel;
        }
    }

    public static Act4EndingSequenceOverlay Create() => new();

    public async Task PlayAsync()
    {
        var comicMaster = LoadComicMaster();
        if (comicMaster == null)
        {
            QueueFree();
            throw new InvalidOperationException("The Act 4 ending comic master texture could not be loaded.");
        }

        foreach (var panel in _panels)
            panel.Texture = comicMaster;

        // Allow one transparent frame to render before covering the combat
        // scene. Without this, adding the CanvasLayer and starting a tween in
        // the same frame can look like an instantaneous cut to black.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _isPlaying = true;
        SetProcess(true);
        SetProcessInput(true);

        var fadeToBlack = CreateTween();
        fadeToBlack.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        fadeToBlack.TweenProperty(_backdrop, "color", Colors.Black, BackdropFadeSeconds);
        await ToSignal(fadeToBlack, Tween.SignalName.Finished);

        MainFile.Logger.Info("[Act4 Ending] Cumulatively revealing three panels from the unified ending comic master.");
        foreach (var panel in _panels)
        {
            var fadeIn = CreateTween();
            fadeIn.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            fadeIn.TweenProperty(panel, "modulate", Colors.White, PanelFadeSeconds);
            await ToSignal(fadeIn, Tween.SignalName.Finished);

            await WaitForAdvanceAsync();
        }

        _isPlaying = false;
        var fadeComicToBlack = CreateTween();
        fadeComicToBlack.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        fadeComicToBlack.TweenProperty(
            _comicLayer,
            "modulate",
            new Color(1f, 1f, 1f, 0f),
            ComicFadeOutSeconds);
        await ToSignal(fadeComicToBlack, Tween.SignalName.Finished);
    }

    public async Task RevealVictoryAndDisposeAsync()
    {
        // WinRun swaps the underlying screen. Give it a frame to settle, then
        // reveal the victory screen from the retained black backdrop.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var revealVictory = CreateTween();
        revealVictory.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        revealVictory.TweenProperty(
            _backdrop,
            "color",
            new Color(0f, 0f, 0f, 0f),
            VictoryRevealSeconds);
        await ToSignal(revealVictory, Tween.SignalName.Finished);

        foreach (var panel in _panels)
            panel.Texture = null;
        QueueFree();
    }

    public override void _Process(double delta)
    {
        if (!_isPlaying || _advance == null)
            return;

        _inputGuardSeconds = Math.Max(0.0, _inputGuardSeconds - delta);
        _remainingSeconds -= delta;
        if (_remainingSeconds <= 0.0)
            Advance();
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!_isPlaying)
            return;

        GetViewport().SetInputAsHandled();
        if (_inputGuardSeconds > 0.0 || _advance == null)
            return;

        var shouldAdvance = inputEvent switch
        {
            InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } => true,
            InputEventScreenTouch { Pressed: true } => true,
            InputEventJoypadButton { Pressed: true } => true,
            InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space or Key.Enter or Key.Escape } => true,
            _ => false
        };

        if (shouldAdvance)
            Advance();
    }

    private Task WaitForAdvanceAsync()
    {
        _remainingSeconds = PanelDurationSeconds;
        _inputGuardSeconds = InputGuardSeconds;
        _advance = new TaskCompletionSource();
        return _advance.Task;
    }

    private void Advance()
    {
        var advance = _advance;
        _advance = null;
        advance?.TrySetResult();
    }

    private static Texture2D? LoadComicMaster()
    {
        var texture = GD.Load<Texture2D>(ComicMasterPath);
        if (texture == null)
            MainFile.Logger.Error($"[Act4 Ending] Failed to load ending comic master: {ComicMasterPath}");
        return texture;
    }
}
