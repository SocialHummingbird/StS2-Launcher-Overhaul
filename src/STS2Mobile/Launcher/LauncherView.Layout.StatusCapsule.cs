using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactStatusBodySeparation = 5;
    private const int CompactStatusAccentHeight = 3;
    private const int CompactStatusHeadlineSeparation = 3;
    private const int CompactStatusHeadlineInlineSeparation = 6;
    private const int CompactStatusPhaseInlineWidth = 112;
    private const int CompactStatusPhaseHorizontalMargin = 7;
    private const int CompactStatusPhaseVerticalMargin = 3;
    private const int CompactStatusActionMinHeight = 24;
    private const int CompactStatusDetailHeight = 44;
    private const int CompactStatusDetailCueWidth = 62;
    private const int CompactStatusDetailCueFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactStatusDetailHorizontalMargin = 8;
    private const int CompactStatusDetailVerticalMargin = 5;
    private const int CompactStatusDetailRowGap = 6;
    private const int CompactStatusDetailRadius = 7;

    private static (
        Control Capsule,
        GridContainer CompactHeadline,
        PanelContainer CompactPhasePanel,
        Button CompactDetailButton,
        StyledLabel CompactDetailCue
    ) BuildStatusCapsule(
        StyledLabel statusPhaseLabel,
        StyledLabel statusActionLabel,
        StyledLabel statusLabel,
        ColorRect statusAccent,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        if (profile.Compact)
            return BuildCompactStatusCapsule(statusPhaseLabel, statusActionLabel, statusLabel, statusAccent, profile);

        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusStyle(scale, compact: false)
        );

        var body = new HBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        panel.AddChild(body);

        statusAccent.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(4, scale),
            LauncherViewLayoutMetrics.ScaleInt(30, scale)
        );
        body.AddChild(statusAccent);

        var phasePanel = new PanelContainer();
        phasePanel.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(96, scale),
            0
        );
        phasePanel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusPhaseStyle(scale, compact: false)
        );
        var phaseBody = new VBoxContainer();
        phaseBody.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        phaseBody.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(1, scale)
        );
        statusPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
        phaseBody.AddChild(statusPhaseLabel);
        phaseBody.AddChild(statusActionLabel);
        phasePanel.AddChild(phaseBody);
        body.AddChild(phasePanel);

        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddChild(statusLabel);
        return (panel, null, null, null, null);
    }
}
