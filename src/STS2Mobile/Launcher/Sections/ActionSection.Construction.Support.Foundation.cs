using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private SupportFoundation BuildSupportFoundation(float scale, bool compact, bool compactStackedActionRows)
    {
        var supportGroup = BuildActionGroup(scale);
        supportGroup.Visible = false;
        var supportToolsGrid = BuildCompactSupportToolsGrid(scale, compact, compactStackedActionRows);
        if (compact)
            supportGroup.AddChild(supportToolsGrid);

        Container supportToolsParent = compact
            ? supportToolsGrid
            : supportGroup;
        return new SupportFoundation(supportGroup, supportToolsGrid, supportToolsParent);
    }
}
