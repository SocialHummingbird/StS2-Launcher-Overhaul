using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const string ProjectRepositoryUrl =
        "https://github.com/SocialHummingbird/StS2-Launcher-Overhaul";

    private static VBoxContainer BuildHelpAttributionFooter(float scale, bool compact)
    {
        var footer = new VBoxContainer
        {
            Name = "HelpAttributionFooter",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        footer.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(3, scale)
        );
        footer.AddChild(BuildProjectAttributionSection(scale, compact));
        footer.AddChild(BuildFmodAttributionSection(scale, compact));
        return footer;
    }

    private static VBoxContainer BuildProjectAttributionSection(float scale, bool compact)
    {
        var section = new VBoxContainer
        {
            Name = "ProjectAttribution",
            SizeFlagsHorizontal = compact
                ? Control.SizeFlags.ShrinkCenter
                : Control.SizeFlags.ExpandFill,
        };
        section.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(3, scale)
        );

        var credit = new StyledLabel(
            "Made by SocialHummingbird",
            scale,
            fontSize: compact ? 11 : 10,
            align: compact ? HorizontalAlignment.Center : HorizontalAlignment.Left
        );
        credit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        credit.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        section.AddChild(credit);

        var repositoryLink = new LinkButton
        {
            Text = "Open StS2 Launcher on GitHub",
            TooltipText = ProjectRepositoryUrl,
            AccessibilityDescription = $"Open the StS2 Launcher source repository at {ProjectRepositoryUrl}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(
                0,
                Math.Max(
                    compact ? 48 : 40,
                    LauncherViewLayoutMetrics.ScaleInt(compact ? 48 : 40, scale)
                )
            ),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        repositoryLink.AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherViewLayoutMetrics.ScaleInt(compact ? 11 : 10, scale)
        );
        repositoryLink.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        repositoryLink.AddThemeColorOverride("font_hover_color", LauncherComponentTheme.OrangeAccent);
        repositoryLink.Pressed += OpenProjectRepository;
        section.AddChild(repositoryLink);
        return section;
    }

    private static void OpenProjectRepository()
    {
        var result = OS.ShellOpen(ProjectRepositoryUrl);
        if (result != Error.Ok)
            PatchHelper.Log($"Failed to open project repository: {result}");
    }
}
