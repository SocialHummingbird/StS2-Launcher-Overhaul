using System;
using Godot;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static LauncherViewPrimaryColumn BuildPrimaryColumn(LauncherLayoutProfile profile, VBoxContainer root)
    {
        var scale = profile.Scale;
        Button compactCurrentTaskButton = null;
        GridContainer compactStickyTaskHeader = null;
        Control compactWorkflowStrip = null;
        var workflowStepNumberLabels = Array.Empty<Components.StyledLabel>();
        var workflowStepLabels = Array.Empty<Components.StyledLabel>();
        var workflowStepDetailLabels = Array.Empty<Components.StyledLabel>();
        var workflowStepAccents = Array.Empty<ColorRect>();
        var workflowStepButtons = Array.Empty<Button>();

        var primaryBody = BuildPrimaryColumnBody(profile, root);
        var left = primaryBody.Body;

        var status = BuildPrimaryStatus(profile);
        left.AddChild(status.Capsule);
        var firstRunGuide = BuildFirstRunGuide(scale, profile.Compact);
        var homeSections = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        homeSections.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                profile.Compact
                    ? LauncherViewLayoutMetrics.CompactPrimaryColumnSeparation
                    : LauncherViewLayoutMetrics.PrimaryColumnSeparation,
                scale
            )
        );
        left.AddChild(homeSections);
        homeSections.AddChild(firstRunGuide);

        var login = new LoginSection(scale, profile.Compact);
        homeSections.AddChild(login);

        var code = new CodeSection(scale, profile.Compact, profile.CompactStackedActionRows);
        homeSections.AddChild(code);

        var download = new DownloadSection(scale, profile.Compact, profile.CompactStackedActionRows);
        homeSections.AddChild(download);

        var actions = new ActionSection(scale, profile.Compact, profile.CompactStackedActionRows);
        left.AddChild(actions);

        VBoxContainer compactDiagnosticsHost = null;
        if (profile.Compact)
        {
            compactDiagnosticsHost = new VBoxContainer();
            compactDiagnosticsHost.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            compactDiagnosticsHost.AddThemeConstantOverride(
                LauncherViewLayoutMetrics.ThemeSeparation,
                LauncherViewLayoutMetrics.ScaleInt(
                    LauncherViewLayoutMetrics.CompactPrimaryColumnSeparation,
                    scale
                )
            );
            left.AddChild(compactDiagnosticsHost);
        }

        actions.HelpDiagnosticsHost.AddChild(BuildFmodAttributionSection(scale, profile.Compact));
        return new LauncherViewPrimaryColumn(
            status.Phase,
            status.Action,
            status.Message,
            status.CompactDetailButton,
            status.CompactDetailCue,
            status.Accent,
            workflowStepNumberLabels,
            workflowStepLabels,
            workflowStepDetailLabels,
            workflowStepAccents,
            workflowStepButtons,
            status.CompactHeadline,
            status.CompactPhasePanel,
            compactStickyTaskHeader,
            compactWorkflowStrip,
            compactCurrentTaskButton,
            primaryBody.PrimaryScroll,
            homeSections,
            firstRunGuide,
            login,
            code,
            download,
            actions,
            compactDiagnosticsHost
        );
    }
}
