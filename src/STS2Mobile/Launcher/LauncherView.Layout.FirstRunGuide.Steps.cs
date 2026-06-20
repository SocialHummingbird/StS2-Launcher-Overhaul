using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactSafeFlowGuideStepHeight = 42;
    private const int CompactSafeFlowGuideStepAccentWidth = 3;
    private const int CompactSafeFlowGuideStepNumberWidth = 26;
    private const int CompactSafeFlowGuideStepNumberFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactSafeFlowGuideStepTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactSafeFlowGuideStepDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactSafeFlowGuideStepRadius = 6;
    private const int CompactSafeFlowGuideStepHorizontalMargin = 7;
    private const int CompactSafeFlowGuideStepVerticalMargin = 4;

    private readonly record struct CompactSafeFlowStepSpec(
        string Marker,
        string Title,
        string Detail,
        Color Accent
    );

    private static readonly CompactSafeFlowStepSpec[] CompactSafeFlowSteps =
    {
        new("1", "Sign in", "Steam account", LauncherComponentTheme.OrangeAccent),
        new("2", "Get files", "Version on Android", LauncherComponentTheme.CyanAccent),
        new("3", "Get saves", "Steam to Android", LauncherComponentTheme.CyanAccent),
        new("4", "Play", "Ready version", LauncherComponentTheme.OrangeHot),
        new("5", "Upload locked", "Review before uploading", LauncherComponentTheme.TextMuted),
    };

    private static void AddCompactSafeFlowSteps(VBoxContainer body, float scale)
    {
        foreach (var step in CompactSafeFlowSteps)
            body.AddChild(BuildCompactSafeFlowStep(scale, step));
    }
}
