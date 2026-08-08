namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ToggleCloudOptions()
    {
        _cloudOptionsExpanded = !_cloudOptionsExpanded;
        ApplyCloudOptionVisibility(_cloudGroup.Visible);
    }

    private void ApplyCloudOptionVisibility(bool cloudControlsVisible)
    {
        if (_compact && !cloudControlsVisible)
        {
            _cloudOptionsExpanded = false;
        }

        if (_cloudOptionsToggle != null)
        {
            _cloudOptionsToggle.Visible = cloudControlsVisible && _compact;
            UpdateCloudOptionsToggleText();
        }

        var showOptions = cloudControlsVisible && (!_compact || _cloudOptionsExpanded);
        if (_compactCloudOptionsRow != null)
            _compactCloudOptionsRow.Visible = showOptions;
        _localBackupToggle.Visible = showOptions;
        _cloudSyncToggle.Visible = showOptions;
    }

    private void UpdateCloudOptionsToggleText()
    {
        if (_cloudOptionsToggle == null)
            return;

        SetCompactActionButtonText(_cloudOptionsToggle, _cloudOptionsExpanded
            ? CompactPlaySyncDrawerText("Hide Save Settings", "Backup and cloud")
            : CompactPlaySyncDrawerText(
                $"Backup {OnOff(_localBackupEnabled)} / Cloud {OnOff(_cloudSyncEnabled)}",
                "Save settings"
            ));
    }

    private static string OnOff(bool value)
        => value ? "On" : "Off";
}
