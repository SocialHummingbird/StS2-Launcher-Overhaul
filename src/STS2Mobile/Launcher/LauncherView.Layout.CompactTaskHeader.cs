using Godot;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactStickyTaskHeaderInlineGap = 6;
    private const int CompactStickyTaskHeaderStackGap = 3;
    private const int CompactStickyTaskButtonMinWidth = 176;
    private const int CompactInlineCurrentTaskHeight = LauncherSectionMetrics.CompactDetailButtonHeight;
    private const int CompactStackedCurrentTaskHeight = CompactWorkflowStepDenseHeight;
    private const int CompactStickyTaskHeaderStackWidth = 560;
    private const int CompactStickyTaskToolbarRadius = 7;
    private const int CompactStickyTaskToolbarHorizontalMargin = 5;
    private const int CompactStickyTaskToolbarVerticalMargin = 4;
    private const string CompactStickyTaskHeaderGridName = "CompactStickyTaskHeaderGrid";
    private const string CompactCurrentTaskButtonBodyName = "CompactCurrentTaskButtonBody";
    private const string CompactCurrentTaskButtonTitleName = "CompactCurrentTaskButtonTitle";
    private const string CompactCurrentTaskButtonDetailName = "CompactCurrentTaskButtonDetail";

    private static (
        Control Toolbar,
        GridContainer Header
    ) BuildCompactStickyTaskHeader(
        LauncherLayoutProfile profile,
        Button compactCurrentTaskButton,
        Control workflowStrip
    )
    {
        var scale = profile.Scale;
        var header = new GridContainer
        {
            Name = CompactStickyTaskHeaderGridName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        header.AddChild(compactCurrentTaskButton);
        header.AddChild(workflowStrip);
        ApplyCompactStickyTaskHeaderLayout(header, compactCurrentTaskButton, workflowStrip, profile);

        return (WrapCompactStickyTaskHeader(scale, header), header);
    }
}
