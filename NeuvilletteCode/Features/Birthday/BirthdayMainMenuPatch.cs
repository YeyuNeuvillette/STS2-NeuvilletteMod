using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Neuvillette.Features.Birthday;

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
[HarmonyAfter("com.ritsukage.sts2-RitsuLib")]
[HarmonyPriority(Priority.Last)]
internal static class BirthdayMainMenuPatch
{
    private const string RitsuSettingsGroupName = "RitsuLibMainMenuModSettings";
    private const string GreetingNodeName = "NeuvilletteBirthdayGreeting";

    [HarmonyPostfix]
    private static void Postfix(NMainMenu __instance)
    {
        if (!BirthdayEnergyIcons.IsActive)
            return;

        Callable.From(() => EnsureGreeting(__instance)).CallDeferred();
    }

    private static void EnsureGreeting(NMainMenu mainMenu)
    {
        if (!GodotObject.IsInstanceValid(mainMenu) || !BirthdayEnergyIcons.IsActive)
            return;

        var settingsGroup = mainMenu.GetNodeOrNull<Control>(RitsuSettingsGroupName);
        if (settingsGroup == null || !GodotObject.IsInstanceValid(settingsGroup))
            return;

        if (settingsGroup.GetNodeOrNull<BirthdayGreetingLabel>(GreetingNodeName) != null)
            return;

        var label = new BirthdayGreetingLabel
        {
            Name = GreetingNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            OffsetLeft = -300f,
            OffsetTop = 68f,
            OffsetRight = 64f,
            OffsetBottom = 100f,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        label.AddThemeColorOverride("font_color", new(0.91f, 0.86359f, 0.7462f));
        label.AddThemeColorOverride("font_shadow_color", new(0f, 0f, 0f, 0.72f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);

        if (mainMenu.GetNodeOrNull<Label>("%ReleaseInfo") is { } releaseInfo)
        {
            label.AddThemeFontOverride("font", releaseInfo.GetThemeFont("font"));
            label.AddThemeFontSizeOverride("font_size", releaseInfo.GetThemeFontSize("font_size"));
        }

        settingsGroup.AddChild(label);
    }
}

internal sealed partial class BirthdayGreetingLabel : Label
{
    private const string LocalizationKey = "neuvillette.easter_egg.birthday_greeting";
    private const string FallbackText = "Happy Birthday, Yeyu!";

    public override void _Ready()
    {
        NeuvilletteSettingsStore.Localization.Changed += RefreshText;
        RefreshText();
    }

    public override void _ExitTree()
    {
        NeuvilletteSettingsStore.Localization.Changed -= RefreshText;
    }

    private void RefreshText()
    {
        Text = NeuvilletteSettingsStore.Localization.Get(LocalizationKey, FallbackText);
    }
}
