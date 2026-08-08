using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static (
        RichTextLabel Log,
        VBoxContainer Drawer,
        Button Toggle
    ) BuildLogColumn(
        LauncherLayoutProfile profile,
        VBoxContainer root,
        Action<InputEvent> dismissKeyboard
    )
    {
        var scale = profile.Scale;
        var drawer = new VBoxContainer();
        drawer.Visible = false;
        drawer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        drawer.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );

        var toggle = new StyledButton(
            profile.Compact ? "" : DiagnosticsToggleText(visible: false),
            scale,
            fontSize: profile.Compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : 14,
            height: profile.Compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : 48
        );
        LauncherButtonStyles.ApplySupportAction(toggle, scale);
        SetDiagnosticsToggleText(toggle, profile, visible: false);
        toggle.Pressed += () =>
        {
            drawer.Visible = !drawer.Visible;
            SetDiagnosticsToggleText(toggle, profile, drawer.Visible);
        };
        root.AddChild(toggle);

        var title = new StyledLabel(
            "Help & Reports",
            scale,
            fontSize: profile.Compact
                ? LauncherSectionMetrics.PromptFontSize
                : LauncherViewLayoutMetrics.LogTitleFontSize,
            align: HorizontalAlignment.Left
        );
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        drawer.AddChild(title);

        var help = new StyledLabel(
            profile.Compact
                ? "Problem details and help reports. Review before sharing."
                : "Hidden by default. Create a help report when sharing launcher issue details.",
            scale,
            fontSize: profile.Compact
                ? LauncherSectionMetrics.ProgressFontSize
                : 11,
            align: HorizontalAlignment.Left
        );
        help.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        help.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextMuted
        );
        drawer.AddChild(help);

        var log = BuildLogView(profile);
        log.CustomMinimumSize = new Vector2(0, DiagnosticsLogHeight(profile));
        log.GuiInput += input => dismissKeyboard(input);
        drawer.AddChild(log);
        root.AddChild(drawer);
        return (log, drawer, toggle);
    }
}
