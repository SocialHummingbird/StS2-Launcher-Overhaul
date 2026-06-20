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
        _supportToggle.Visible = visible;
        if (!visible)
        {
            _supportExpanded = false;
            _supportGroup.Visible = false;
            SetCompactActionButtonText(_supportToggle, SupportToggleText());
        }
        else
        {
            _supportGroup.Visible = _supportExpanded;
        }
        _diagnosticsButton.Visible = visible;
        _refreshVersionsButton.Visible = visible;
        _clearCachedVersionsButton.Visible = visible;
        _showLastErrorButton.Visible = visible;
        _copyRawLogButton.Visible = visible;
    }
}
