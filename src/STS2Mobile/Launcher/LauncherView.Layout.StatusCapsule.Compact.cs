using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static (
        Control Capsule,
        GridContainer CompactHeadline,
        PanelContainer CompactPhasePanel,
        Button CompactDetailButton,
        StyledLabel CompactDetailCue
    ) BuildCompactStatusCapsule(
        StyledLabel statusPhaseLabel,
        StyledLabel statusActionLabel,
        StyledLabel statusLabel,
        ColorRect statusAccent,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusStyle(scale, compact: true)
        );

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusBodySeparation, scale)
        );
        panel.AddChild(body);

        statusAccent.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusAccentHeight, scale)
        );
        body.AddChild(statusAccent);

        var headline = new GridContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };

        var phasePanel = new PanelContainer();
        phasePanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        phasePanel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusPhaseStyle(scale, compact: true)
        );
        statusPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
        phasePanel.AddChild(statusPhaseLabel);
        headline.AddChild(phasePanel);

        statusActionLabel.HorizontalAlignment = HorizontalAlignment.Left;
        statusActionLabel.VerticalAlignment = VerticalAlignment.Center;
        statusActionLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        statusActionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusActionLabel.ClipText = false;
        headline.AddChild(statusActionLabel);
        ApplyCompactStatusHeadlineLayout(headline, phasePanel, statusActionLabel, profile);
        body.AddChild(headline);

        var detailButton = BuildCompactStatusDetailButton(scale);
        var detailRow = BuildCompactStatusDetailRow(statusLabel, scale);
        var detailCue = BuildCompactStatusDetailCue(scale);
        detailRow.AddChild(detailCue);

        detailButton.AddChild(detailRow);
        body.AddChild(detailButton);

        return (panel, headline, phasePanel, detailButton, detailCue);
    }

    private static void ApplyCompactStatusHeadlineLayout(
        GridContainer headline,
        PanelContainer phasePanel,
        StyledLabel statusActionLabel,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        var stacked = profile.CompactStackedActionRows;
        headline.Columns = stacked ? 1 : 2;
        headline.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                stacked ? CompactStatusHeadlineSeparation : CompactStatusHeadlineInlineSeparation,
                scale
            )
        );
        phasePanel.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(stacked ? 0 : CompactStatusPhaseInlineWidth, scale),
            0
        );
        phasePanel.SizeFlagsHorizontal = stacked
            ? Control.SizeFlags.ExpandFill
            : Control.SizeFlags.ShrinkBegin;
        statusActionLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusActionLabel.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusActionMinHeight, scale)
        );
    }
}
