namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ShowUpdateButton(bool visible)
    {
        _updateButton.Visible = visible;
        _updateButton.Disabled = false;
        SetVersionPrimaryAction(LauncherVersionPrimaryAction.CheckForUpdates);
    }

    private void ShowDestinationTools()
    {
        _diagnosticsButton.Visible = true;
        _refreshVersionsButton.Visible = true;
        _redownloadButton.Visible = true;
        _showLastErrorButton.Visible = true;
        _copyRawLogButton.Visible = true;
    }
}
