using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private SupportFoundation BuildSupportFoundation(float scale, bool compact, bool compactStackedActionRows)
    {
        var supportGroup = BuildActionGroup(scale);
        supportGroup.Visible = false;
        Container supportToolsParent = supportGroup;
        if (compact)
        {
            var supportToolsGrid = BuildCompactSupportToolsGrid(scale, compact, compactStackedActionRows);
            supportGroup.AddChild(supportToolsGrid);
            supportToolsParent = supportToolsGrid;
        }

        return new SupportFoundation(supportGroup, supportToolsParent);
    }
}
