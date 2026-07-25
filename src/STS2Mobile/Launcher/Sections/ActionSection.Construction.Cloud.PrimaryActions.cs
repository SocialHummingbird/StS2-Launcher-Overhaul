using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private CloudPrimaryActionControls BuildCloudPrimaryActionControls(
        VBoxContainer cloudGroup,
        float scale,
        bool compact
    )
    {
        var pushPullRow = new VBoxContainer();
        pushPullRow.Visible = false;
        pushPullRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                compact
                    ? CompactCloudPrimaryActionSeparation
                    : LauncherSectionMetrics.PushPullRowSeparation,
                scale
            )
        );
        Container cloudPrimaryActionsParent = compact
            ? BuildCompactCloudPrimaryActionsRow(pushPullRow, scale, _compactStackedActionRows)
            : pushPullRow;

        var pullButton = AddPushPullButton(
            cloudPrimaryActionsParent,
            compact ? CompactCloudPullText() : "Pull Saves from Steam Cloud",
            scale,
            () =>
            {
                ResetCloudPushArm();
                CloudPullPressed?.Invoke();
            }
        );
        LauncherButtonStyles.ApplyCloudPullAction(pullButton, scale);
        SetCompactActionButtonText(pullButton, pullButton.Text);

        var cloudPushToggle = AddPushPullButton(
            cloudPrimaryActionsParent,
            compact
                ? CompactCloudPushToggleText(
                    expanded: false,
                    "Check requirements"
                )
                : "Review Upload",
            scale,
            ToggleCloudPush
        );
        LauncherButtonStyles.ApplySupportAction(cloudPushToggle, scale);
        SetCompactActionButtonText(cloudPushToggle, cloudPushToggle.Text);
        cloudPushToggle.Visible = compact;

        var operationProgress = BuildCloudOperationProgressControls(
            pushPullRow,
            scale,
            compact
        );

        var cancelOperationButton = AddPushPullButton(
            pushPullRow,
            "Cancel Cloud Operation",
            scale,
            RequestCloudOperationCancellation
        );
        cancelOperationButton.Visible = false;
        LauncherButtonStyles.ApplySupportAction(
            cancelOperationButton,
            scale
        );
        SetCompactActionButtonText(
            cancelOperationButton,
            cancelOperationButton.Text
        );

        var pushEligibilityLabel = BuildCloudPushEligibilityLabel(scale, compact);
        pushPullRow.AddChild(pushEligibilityLabel);

        var pushButton = AddPushPullButton(
            pushPullRow,
            compact ? CompactCloudPushDangerText() : PushButtonText,
            scale,
            ArmCloudPush
        );
        LauncherButtonStyles.ApplyDangerAction(pushButton, scale);
        SetCompactActionButtonText(pushButton, pushButton.Text);

        var confirmPushButton = AddPushPullButton(
            pushPullRow,
            compact ? CompactCloudPushConfirmText() : PushConfirmButtonText,
            scale,
            ConfirmCloudPush
        );
        confirmPushButton.Visible = false;
        LauncherButtonStyles.ApplyDangerAction(confirmPushButton, scale);
        SetCompactActionButtonText(confirmPushButton, confirmPushButton.Text);

        var pushConfirmationLabel = BuildCloudPushConfirmationLabel(scale, compact);
        pushPullRow.AddChild(pushConfirmationLabel);
        cloudGroup.AddChild(pushPullRow);

        return new CloudPrimaryActionControls(
            pushPullRow,
            pullButton,
            cloudPushToggle,
            pushButton,
            confirmPushButton,
            cancelOperationButton,
            operationProgress.Group,
            operationProgress.PhaseLabel,
            operationProgress.DetailLabel,
            operationProgress.ProgressBar,
            pushEligibilityLabel,
            pushConfirmationLabel
        );
    }
}
