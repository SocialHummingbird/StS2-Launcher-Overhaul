using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static (
        Control Capsule,
        Button CompactDetailButton,
        StyledLabel CompactDetailCue
    ) BuildCompactStatusCapsule(
        StyledLabel statusPhaseLabel,
        StyledLabel statusLabel,
        ColorRect statusAccent,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        var panel = new PanelContainer();
        panel.Name = "GlobalStatusCapsule";
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

        headline.Columns = 1;
        phasePanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddChild(headline);

        var detailButton = BuildCompactStatusDetailButton(scale);
        // Container-managed text contributes its wrapped height to the status panel.
        // A label anchored inside a Button can collapse to zero width on phones.
        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.AddChild(statusLabel);
        var detailCue = BuildCompactStatusDetailCue(scale);
        detailCue.Visible = false;
        body.AddChild(detailCue);
        body.AddChild(detailButton);

        return (panel, detailButton, detailCue);
    }
}
