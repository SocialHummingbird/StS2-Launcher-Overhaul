using System;
using Godot;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static LauncherViewPrimaryColumn BuildPrimaryColumn(LauncherLayoutProfile profile, VBoxContainer root)
    {
        var scale = profile.Scale;
        var compactCurrentTaskButton = BuildCompactCurrentTaskButton(scale, profile.Compact);
        var workflowStrip = BuildCompactWorkflowStrip(scale, profile.Compact, profile.CompactStackedActionRows);
        GridContainer compactStickyTaskHeader = null;
        if (profile.Compact)
        {
            var stickyHeader = BuildCompactStickyTaskHeader(profile, compactCurrentTaskButton, workflowStrip.Strip);
            compactStickyTaskHeader = stickyHeader.Header;
            root.AddChild(stickyHeader.Toolbar);
        }

        var primaryBody = BuildPrimaryColumnBody(profile, root);
        var left = primaryBody.Body;

        var status = BuildPrimaryStatus(profile);
        left.AddChild(status.Capsule);
        if (!profile.Compact)
            left.AddChild(workflowStrip.Strip);
        var firstRunGuide = BuildFirstRunGuide(scale, profile.Compact);
        left.AddChild(firstRunGuide);

        var login = new LoginSection(scale, profile.Compact);
        left.AddChild(login);

        var code = new CodeSection(scale, profile.Compact, profile.CompactStackedActionRows);
        left.AddChild(code);

        var download = new DownloadSection(scale, profile.Compact, profile.CompactStackedActionRows);
        left.AddChild(download);

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

        left.AddChild(BuildFmodAttributionSection(scale, profile.Compact));
        if (profile.Compact)
            left.AddChild(BuildCompactBottomScrollSpacer(scale));

        return new LauncherViewPrimaryColumn(
            status.Phase,
            status.Action,
            status.Message,
            status.CompactDetailButton,
            status.CompactDetailCue,
            status.Accent,
            workflowStrip.StepNumberLabels,
            workflowStrip.StepLabels,
            workflowStrip.StepDetailLabels,
            workflowStrip.StepAccents,
            workflowStrip.StepButtons,
            status.CompactHeadline,
            status.CompactPhasePanel,
            compactStickyTaskHeader,
            workflowStrip.Strip,
            compactCurrentTaskButton,
            primaryBody.PrimaryScroll,
            firstRunGuide,
            login,
            code,
            download,
            actions,
            compactDiagnosticsHost
        );
    }
}
