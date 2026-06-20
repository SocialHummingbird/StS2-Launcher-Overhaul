using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class LoginSection
{
    private static Label CreateCredentialHelpLabel(float scale, bool compact)
    {
        var compactAndroid = compact && OperatingSystem.IsAndroid();
        var label = new StyledLabel(
            CredentialHelpText(compact),
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        label.AutowrapMode = compactAndroid
            ? TextServer.AutowrapMode.WordSmart
            : TextServer.AutowrapMode.WordSmart;
        label.ClipText = false;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.CustomMinimumSize = new Vector2(
            0,
            compactAndroid
                ? LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.CompactCredentialHelpHeight, scale)
                : (compact ? 30f * scale : 38f * scale)
        );
        label.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        return label;
    }

    private static string CredentialHelpText(bool compact)
    {
        if (!OperatingSystem.IsAndroid())
            return "Use the visible Steam fields above. StS2 Mobile does not store your Steam password.";

        return compact
            ? "Password manager can appear.\nSteam password is not stored."
            : "Use the integrated Steam login panel. Android/Samsung/Google password suggestions may appear there; StS2 Mobile does not store your Steam password.";
    }
}
