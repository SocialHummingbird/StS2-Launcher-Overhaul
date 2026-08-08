using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ToggleCloudPush()
    {
        _cloudPushExpanded = !_cloudPushExpanded;
        RefreshCloudPushEligibility();
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
            UpdateCloudPushReviewButtonText();
        }

        var canShowPush = cloudVisible && (!_compact || _cloudPushExpanded);
        _cloudPushEligibilityLabel.Visible = canShowPush;
        _pushButton.Visible = canShowPush && showPushButton;
        if (!canShowPush)
        {
            _confirmPushButton.Visible = false;
            _pushConfirmationLabel.Visible = false;
        }
    }

    private void ArmCloudPush()
    {
        var eligibility = ReadAndApplyCloudPushEligibility();
        if (eligibility == null || !eligibility.IsEligible)
            return;

        _pushButton.Visible = false;
        _confirmPushButton.Visible = true;
        _pushConfirmationLabel.Visible = true;
    }

    private void ConfirmCloudPush()
    {
        var eligibility = ReadAndApplyCloudPushEligibility();
        if (eligibility == null || !eligibility.IsEligible)
        {
            ResetCloudPushArm();
            return;
        }

        ResetCloudPushArm();
        CloudPushPressed?.Invoke();
    }

    private void ResetCloudPushArm(bool showPushButton = true)
    {
        _confirmPushButton.Visible = false;
        _pushConfirmationLabel.Visible = false;
        ApplyCloudPushVisibility(showPushButton);
    }

    internal void RefreshCloudPushEligibility()
    {
        if (_cloudPushEligibilityLabel == null)
            return;

        ReadAndApplyCloudPushEligibility();
    }

    private CloudPushEligibilityResult ReadAndApplyCloudPushEligibility()
    {
        var eligibility = CloudPushArmRequested?.Invoke();
        if (eligibility == null)
        {
            ApplyCloudPushEligibilityUnavailable();
            return null;
        }

        ApplyCloudPushEligibility(eligibility);
        return eligibility;
    }

    private void ApplyCloudPushEligibility(
        CloudPushEligibilityResult eligibility
    )
    {
        var presentation = CloudPushEligibilityPresentation.Create(eligibility);
        _cloudPushEligible = presentation.IsEligible;
        _cloudPushReviewDetail = presentation.ReviewButtonDetail;
        _cloudPushEligibilityLabel.Text = presentation.GuidanceText;
        _cloudPushEligibilityLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            presentation.IsEligible
                ? LauncherComponentTheme.CyanAccent
                : LauncherComponentTheme.OrangeHot
        );
        ApplyCloudPushDisabledState();
        UpdateCloudPushReviewButtonText();
        if (!presentation.IsEligible && _confirmPushButton.Visible)
            ResetCloudPushArm();
    }

    internal void ApplyCloudPostOperationSnapshot(
        CloudPostOperationSnapshot snapshot
    )
    {
        ApplyToggle(
            _localBackupToggle,
            _localBackupEnabled,
            LocalBackupText(
                _localBackupEnabled,
                snapshot.CurrentMirrorSaveCount
            )
        );
        ApplyCloudPushEligibility(snapshot.UploadEligibility);
    }

    private void ApplyCloudPushEligibilityUnavailable()
    {
        _cloudPushEligible = false;
        _cloudPushReviewDetail = "Availability pending";
        _cloudPushEligibilityLabel.Text =
            "Upload availability is still being checked. Review Upload again in a moment.";
        _cloudPushEligibilityLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        ApplyCloudPushDisabledState();
        UpdateCloudPushReviewButtonText();
    }

    private void ApplyCloudPushDisabledState()
    {
        _pushButton.Disabled = _pushPullDisabled || !_cloudPushEligible;
        _cloudPushToggle.Disabled = _pushPullDisabled;
        _confirmPushButton.Disabled = _pushPullDisabled || !_cloudPushEligible;
        _pullButton.Disabled = _pushPullDisabled;
    }

    private void UpdateCloudPushReviewButtonText()
        => SetCompactActionButtonText(
            _cloudPushToggle,
            _compact
                ? CompactCloudPushToggleText(
                    _cloudPushExpanded,
                    _cloudPushReviewDetail
                )
                : "Review Upload"
        );
}
