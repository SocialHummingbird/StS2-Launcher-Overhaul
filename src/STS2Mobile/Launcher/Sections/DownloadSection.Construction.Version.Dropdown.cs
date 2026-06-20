using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private OptionButton BuildBranchDropdown(float scale, bool compact)
    {
        var dropdown = new OptionButton();
        dropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        dropdown.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(
                compact ? LauncherSectionMetrics.PrimaryButtonHeight : LauncherSectionMetrics.SecondaryButtonHeight,
                scale
            )
        );
        LauncherButtonStyles.ApplyDropdownAction(
            dropdown,
            scale,
            compact ? LauncherSectionMetrics.PrimaryButtonFontSize : LauncherSectionMetrics.SecondaryButtonFontSize,
            compact
        );
        dropdown.ItemSelected += ApplyGameBranch;
        return dropdown;
    }

    private void AddBranchDropdownToLayout()
    {
        if (_compact)
        {
            _compactVersionControlsRow.AddChild(_branchDropdown);
            AddChild(_compactVersionControlsRow);
            return;
        }

        AddChild(_branchDropdown);
    }
}
