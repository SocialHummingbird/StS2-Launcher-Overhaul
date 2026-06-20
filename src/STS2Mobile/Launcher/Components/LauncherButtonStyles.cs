using Godot;

namespace STS2Mobile.Launcher.Components;

internal static partial class LauncherButtonStyles
{
    internal static void ApplyPrimaryAction(Button button, float scale)
        => Apply(
            button,
            scale,
            LauncherComponentTheme.OrangeAccent,
            LauncherComponentTheme.OrangeHot,
            LauncherComponentTheme.DarkInk,
            borderWidth: 2
        );

    internal static void ApplySafeAction(Button button, float scale)
        => Apply(
            button,
            scale,
            LauncherComponentTheme.CyanDim,
            LauncherComponentTheme.CyanAccent,
            LauncherComponentTheme.CyanAccent,
            borderWidth: 2,
            filled: false
        );

    internal static void ApplySupportAction(Button button, float scale)
        => Apply(
            button,
            scale,
            LauncherComponentTheme.ButtonNormal,
            LauncherComponentTheme.CyanDim,
            LauncherComponentTheme.TextPrimary,
            borderWidth: 1
        );

    internal static void ApplyCloudPullAction(Button button, float scale)
        => Apply(
            button,
            scale,
            new Color(0.07f, 0.18f, 0.15f),
            new Color(0.24f, 0.7f, 0.36f),
            LauncherComponentTheme.TextPrimary,
            borderWidth: 2
        );

    internal static void ApplyDangerAction(Button button, float scale)
        => Apply(
            button,
            scale,
            new Color(0.22f, 0.07f, 0.07f),
            new Color(0.92f, 0.24f, 0.18f),
            LauncherComponentTheme.TextPrimary,
            borderWidth: 2
        );
}
