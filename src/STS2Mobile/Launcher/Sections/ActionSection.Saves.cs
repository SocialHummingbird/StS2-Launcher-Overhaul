using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private (
        VBoxContainer Group,
        Button SyncNowButton,
        Button PullButton,
        Button PushButton,
        Label Status,
        GridContainer Details,
        Label LocalState,
        Label SteamState
    ) BuildSaveSyncControls(float scale, bool compact)
    {
        var group = BuildActionGroup(scale);
        group.Name = "SaveSyncPresentation";
        AddChild(group);

        var namespaceExplanation = new StyledLabel(
            "Vanilla and modded saves are separate. Both sync automatically.",
            scale,
            fontSize: compact ? 12 : 13,
            align: HorizontalAlignment.Left
        )
        {
            Name = "SaveNamespaceExplanation",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        namespaceExplanation.AddThemeColorOverride(
            "font_color",
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(namespaceExplanation);

        var status = new StyledLabel(
            "Not synced yet",
            scale,
            fontSize: compact ? 15 : 16,
            align: HorizontalAlignment.Left
        )
        {
            Name = "SaveSyncHeadline",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        status.AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        group.AddChild(status);

        var details = BuildStateRows(scale);
        details.Name = "SaveSyncDetails";
        details.Visible = false;
        group.AddChild(details);
        var localState = AddStateRow(
            details,
            "This device",
            "Checking...",
            "SaveLocalState",
            scale,
            compact
        );
        var steamState = AddStateRow(
            details,
            "Steam Cloud",
            "Checking...",
            "SaveSteamState",
            scale,
            compact
        );

        var actions = BuildActionGroup(scale);
        actions.Name = "SaveSyncActions";
        group.AddChild(actions);

        var syncNow = AddActionButton(
            actions,
            "Sync now",
            scale,
            () => SaveSyncNowPressed?.Invoke()
        );
        syncNow.AccessibilityName = "Sync now";
        LauncherButtonStyles.ApplyPrimaryAction(syncNow, scale);

        var manualActions = BuildCompactActionRow(
            actions,
            scale,
            _compactStackedActionRows
        );
        manualActions.Name = "SaveManualActions";

        var pull = AddActionButton(
            manualActions,
            "Get saves from Steam",
            scale,
            () => SavePullPressed?.Invoke()
        );
        pull.AccessibilityName = "Get saves from Steam";
        LauncherButtonStyles.ApplySupportAction(pull, scale);

        var push = AddActionButton(
            manualActions,
            "Send saves to Steam",
            scale,
            () => SavePushPressed?.Invoke()
        );
        push.AccessibilityName = "Send saves to Steam";
        LauncherButtonStyles.ApplySupportAction(push, scale);

        return (
            group,
            syncNow,
            pull,
            push,
            status,
            details,
            localState,
            steamState
        );
    }

    internal void SetSaveSyncControlsDisabled(bool disabled)
    {
        _saveSyncNowButton.Disabled = disabled;
        _savePullButton.Disabled = disabled;
        _savePushButton.Disabled = disabled;
    }

    internal void SetSaveSyncPresentation(LauncherSaveSyncPresentation presentation)
    {
        _saveSyncStatus.Text = PresentationText(
            presentation.Summary,
            "Not synced yet"
        );
        _saveSyncStatus.SetMeta("save_sync_state", presentation.State.ToString());
        _saveSyncDetails.Visible = presentation.ShowEndpointDetails;
        _saveLocalState.Text = PresentationText(
            presentation.LocalState,
            "Saved locally"
        );
        _saveSteamState.Text = PresentationText(
            presentation.SteamState,
            "Not checked"
        );
        _homeSyncState = PresentationText(
            presentation.HomeState,
            "Not synced yet"
        );
        UpdateHomeStateLine();
    }

    private static GridContainer BuildStateRows(float scale)
    {
        var rows = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        rows.AddThemeConstantOverride(
            "h_separation",
            LauncherViewLayoutMetrics.ScaleInt(12, scale)
        );
        rows.AddThemeConstantOverride(
            "v_separation",
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        return rows;
    }

    private static Label AddStateRow(
        GridContainer rows,
        string title,
        string value,
        string valueName,
        float scale,
        bool compact
    )
    {
        var titleLabel = new StyledLabel(
            title,
            scale,
            fontSize: compact ? 12 : 13,
            align: HorizontalAlignment.Left
        );
        titleLabel.AddThemeColorOverride("font_color", LauncherComponentTheme.TextSecondary);
        rows.AddChild(titleLabel);

        var valueLabel = new StyledLabel(
            value,
            scale,
            fontSize: compact ? 12 : 13,
            align: HorizontalAlignment.Left
        )
        {
            Name = valueName,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        valueLabel.AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        rows.AddChild(valueLabel);
        return valueLabel;
    }
}
