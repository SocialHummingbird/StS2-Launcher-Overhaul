using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static LauncherViewPrimaryStatus BuildPrimaryStatus(
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        const LauncherStatusSeverity initialSeverity = LauncherStatusSeverity.Working;
        var initialPhase = LauncherPortalStatusFormatter.LabelFor(initialSeverity);
        var statusPhaseLabel = new StyledLabel(
            initialPhase,
            scale,
            fontSize: profile.Compact ? 13 : 11,
            align: HorizontalAlignment.Center
        );
        statusPhaseLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherPortalStatusFormatter.ColorFor(initialSeverity)
        );
        statusPhaseLabel.Name = "GlobalStatusSeverity";

        var statusLabel = new StyledLabel(
            LauncherPortalStatusFormatter.MessageFor("Starting launcher..."),
            scale,
            fontSize: profile.Compact ? 15 : 14,
            align: HorizontalAlignment.Left
        );
        statusLabel.Name = "GlobalStatusMessage";
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );

        var statusAccent = new ColorRect();
        statusAccent.Color = LauncherPortalStatusFormatter.ColorFor(initialSeverity);
        var statusCapsule = BuildStatusCapsule(
            statusPhaseLabel,
            statusLabel,
            statusAccent,
            profile
        );

        return new LauncherViewPrimaryStatus(
            statusPhaseLabel,
            statusLabel,
            statusAccent,
            statusCapsule.Capsule,
            statusCapsule.CompactDetailButton,
            statusCapsule.CompactDetailCue
        );
    }
}
