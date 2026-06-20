using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly record struct CloudControls(
        VBoxContainer Group,
        VBoxContainer PushPullRow,
        Button PullButton,
        Button CloudPushToggle,
        Button PushButton,
        Button ConfirmPushButton,
        Label PushConfirmationLabel,
        Label CloudSafetyLabel,
        Button CloudSafetyToggle,
        Button CloudOptionsToggle,
        Container CompactCloudOptionsRow,
        Button LocalBackupToggle,
        Button CloudSyncToggle
    );

    private readonly record struct CloudPrimaryActionControls(
        VBoxContainer PushPullRow,
        Button PullButton,
        Button CloudPushToggle,
        Button PushButton,
        Button ConfirmPushButton,
        Label PushConfirmationLabel
    );

    private readonly record struct CloudSafetyControls(
        Label CloudSafetyLabel,
        Button CloudSafetyToggle
    );

    private readonly record struct CloudOptionControls(
        Button CloudOptionsToggle,
        Container CompactCloudOptionsRow,
        Button LocalBackupToggle,
        Button CloudSyncToggle
    );

    private CloudControls BuildCloudControls(float scale, bool compact)
    {
        var cloudGroup = BuildActionGroup(scale);
        cloudGroup.Visible = false;
        AddChild(cloudGroup);

        var primaryActions = BuildCloudPrimaryActionControls(cloudGroup, scale, compact);
        var safetyControls = BuildCloudSafetyControls(cloudGroup, scale, compact);
        var optionControls = BuildCloudOptionControls(cloudGroup, scale, compact);

        return new CloudControls(
            cloudGroup,
            primaryActions.PushPullRow,
            primaryActions.PullButton,
            primaryActions.CloudPushToggle,
            primaryActions.PushButton,
            primaryActions.ConfirmPushButton,
            primaryActions.PushConfirmationLabel,
            safetyControls.CloudSafetyLabel,
            safetyControls.CloudSafetyToggle,
            optionControls.CloudOptionsToggle,
            optionControls.CompactCloudOptionsRow,
            optionControls.LocalBackupToggle,
            optionControls.CloudSyncToggle
        );
    }
}
