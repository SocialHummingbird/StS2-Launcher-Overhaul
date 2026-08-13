using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void UpdateBranchHelpText()
    {
        _selectedVersionInstalled = LauncherBranchCatalog.SelectedVersionInstalled(
            _gameBranch,
            _availableBranches
        );
        var state = LauncherBranchCatalog.SelectedVersionState(
            _gameBranch,
            _availableBranches
        );
        _branchHelpLabel.Text = _versionCheckOutcome switch
        {
            LauncherVersionCheckOutcome.UpToDate => state + " · Up to date",
            LauncherVersionCheckOutcome.UpdateAvailable => state + " · Update available",
            _ => state,
        };
        _branchHelpLabel.TooltipText = LauncherBranchCatalog.SelectedOptionStatus(
            _gameBranch,
            _availableBranches
        );
        _branchHelpLabel.AccessibilityDescription = _branchHelpLabel.TooltipText;
        UpdateVersionActionAvailability();
    }
}
