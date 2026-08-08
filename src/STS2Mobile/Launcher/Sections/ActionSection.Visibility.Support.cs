namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ShowUpdateButton(bool visible)
    {
        _updateButton.Visible = visible;
        _updateButton.Disabled = false;
        SetCompactActionButtonText(_updateButton, _compact
            ? CompactSupportToolText("Check Files", "Updates")
            : "Check for Updates");
    }

    private void SetSupportButtonsVisible(bool visible)
    {
        _supportToggle.Visible = false;
        _supportExpanded = false;
        _supportGroup.Visible = false;
        _diagnosticsButton.Visible = true;
        _refreshVersionsButton.Visible = visible;
        _clearCachedVersionsButton.Visible = visible;
        _showLastErrorButton.Visible = true;
        _copyRawLogButton.Visible = true;
    }
}
