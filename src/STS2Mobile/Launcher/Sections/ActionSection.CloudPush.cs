namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ToggleCloudPush()
    {
        _cloudPushExpanded = !_cloudPushExpanded;
        ResetCloudPushArm();
    }

    private void ApplyCloudPushVisibility(bool showPushButton)
    {
        var cloudVisible = _pushPullRow.Visible;
        if (_compact && !cloudVisible)
        {
            _cloudPushExpanded = false;
        }

        if (_cloudPushToggle != null)
        {
            _cloudPushToggle.Visible = cloudVisible && _compact;
            SetCompactActionButtonText(_cloudPushToggle, _compact
                ? CompactCloudPushToggleText(_cloudPushExpanded)
                : "Push Locked");
        }

        var canShowPush = cloudVisible && (!_compact || _cloudPushExpanded);
        _pushButton.Visible = canShowPush && showPushButton;
        if (!canShowPush)
        {
            _confirmPushButton.Visible = false;
            _pushConfirmationLabel.Visible = false;
        }
    }

    private void ArmCloudPush()
    {
        if (CloudPushArmRequested?.Invoke() == false)
            return;

        _pushButton.Visible = false;
        _confirmPushButton.Visible = true;
        _pushConfirmationLabel.Visible = true;
    }

    private void ConfirmCloudPush()
    {
        ResetCloudPushArm();
        CloudPushPressed?.Invoke();
    }

    private void ResetCloudPushArm(bool showPushButton = true)
    {
        _confirmPushButton.Visible = false;
        _pushConfirmationLabel.Visible = false;
        ApplyCloudPushVisibility(showPushButton);
    }
}
