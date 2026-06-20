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
        var initialPhase = LauncherPortalStatusFormatter.PhaseFor("Initializing...");
        var statusPhaseLabel = new StyledLabel(
            initialPhase,
            scale,
            fontSize: profile.Compact ? 13 : 11,
            align: HorizontalAlignment.Center
        );
        statusPhaseLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherPortalStatusFormatter.ColorFor(initialPhase)
        );

        var statusActionLabel = new StyledLabel(
            LauncherPortalStatusFormatter.ActionFor("Initializing..."),
            scale,
            fontSize: profile.Compact ? 13 : 10,
            align: HorizontalAlignment.Center
        );
        statusActionLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );

        var statusLabel = new StyledLabel(
            LauncherPortalStatusFormatter.MessageFor("Initializing..."),
            scale,
            fontSize: profile.Compact ? 15 : 14,
            align: HorizontalAlignment.Left
        );
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );

        var statusAccent = new ColorRect();
        statusAccent.Color = LauncherPortalStatusFormatter.ColorFor(initialPhase);
        var statusCapsule = BuildStatusCapsule(
            statusPhaseLabel,
            statusActionLabel,
            statusLabel,
            statusAccent,
            profile
        );

        return new LauncherViewPrimaryStatus(
            statusPhaseLabel,
            statusActionLabel,
            statusLabel,
            statusAccent,
            statusCapsule.Capsule,
            statusCapsule.CompactDetailButton,
            statusCapsule.CompactDetailCue,
            statusCapsule.CompactHeadline,
            statusCapsule.CompactPhasePanel
        );
    }
}
