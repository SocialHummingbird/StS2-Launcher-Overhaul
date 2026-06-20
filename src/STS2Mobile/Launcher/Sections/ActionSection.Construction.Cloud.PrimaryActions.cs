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
            compact ? CompactCloudPushToggleText(expanded: false) : "Push Locked",
            scale,
            ToggleCloudPush
        );
        LauncherButtonStyles.ApplyDangerAction(cloudPushToggle, scale);
        SetCompactActionButtonText(cloudPushToggle, cloudPushToggle.Text);
        cloudPushToggle.Visible = compact;

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
            pushConfirmationLabel
        );
    }

    private static StyledLabel BuildCloudPushConfirmationLabel(float scale, bool compact)
    {
        var pushConfirmationLabel = new StyledLabel(
            compact
                ? CompactCloudPushWarningText()
                : "Confirming Push uploads Android saves to Steam Cloud for the selected version and can overwrite remote Steam Cloud saves. Continue only after Pull and local save evidence are verified.",
            scale,
            fontSize: compact
                ? CompactCloudPushWarningFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        pushConfirmationLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        pushConfirmationLabel.ClipText = compact;
        pushConfirmationLabel.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            pushConfirmationLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            pushConfirmationLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactCloudPushWarningHeight, scale)
            );
        }
        pushConfirmationLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        pushConfirmationLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.OrangeHot
        );
        pushConfirmationLabel.Visible = false;
        return pushConfirmationLabel;
    }
}
