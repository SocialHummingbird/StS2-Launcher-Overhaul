using Godot;
using STS2Mobile.Launcher;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private void MoveCompactPrimaryInstallControlsBeforeVersionDetails()
    {
        if (!_compact)
            return;

        MoveChild(_compactSelectedVersionPanel, _branchDetailsToggle.GetIndex());
        MoveChild(_downloadButton, _branchDetailsToggle.GetIndex());
    }

    private static Container BuildCompactVersionControlsRow(float scale, bool compactStackedActionRows)
    {
        Container row = compactStackedActionRows ? new VBoxContainer() : new HBoxContainer();
        row.Visible = false;
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );
        return row;
    }
}
