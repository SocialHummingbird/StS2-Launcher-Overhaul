using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const string CompactSafeFlowToggleBodyName = "CompactSafeFlowToggleBody";
    private const string CompactSafeFlowToggleTitleName = "CompactSafeFlowToggleTitle";
    private const string CompactSafeFlowToggleDetailName = "CompactSafeFlowToggleDetail";

    private static readonly CompactButtonDetailLabelSpec CompactSafeFlowToggleLabels =
        CompactButtonDetailLabelSpec.Default(
            CompactSafeFlowToggleBodyName,
            CompactSafeFlowToggleTitleName,
            CompactSafeFlowToggleDetailName
        );

    private static Control BuildCollapsedFirstRunGuide(float scale)
    {
        var wrapper = new VBoxContainer();
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        wrapper.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(5, scale)
        );

        var toggle = new StyledButton(
            "",
            scale,
            fontSize: LauncherSectionMetrics.CompactDetailButtonFontSize,
            height: LauncherSectionMetrics.CompactDrawerToggleHeight
        );
        LauncherButtonStyles.ApplySupportAction(toggle, scale);
        SetCompactSafeFlowToggleText(toggle, scale, "Quick Start", "Get saves first");
        wrapper.AddChild(toggle);

        var guide = BuildFirstRunGuidePanel(scale, compact: true);
        guide.Visible = false;
        wrapper.AddChild(guide);

        toggle.Pressed += () =>
        {
            guide.Visible = !guide.Visible;
            if (guide.Visible)
                SetCompactSafeFlowToggleText(toggle, scale, "Hide Guide", "Safe order");
            else
                SetCompactSafeFlowToggleText(toggle, scale, "Quick Start", "Get saves first");
        };

        return wrapper;
    }

}
