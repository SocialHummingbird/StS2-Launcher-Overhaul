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
        Label LastSuccessState,
        Label LocalState,
        Label SteamState
    ) BuildSaveSyncControls(float scale, bool compact)
    {
        var group = BuildActionGroup(scale);
        group.Name = "SaveSyncPresentation";
        AddChild(group);

        var explanation = new StyledLabel(
            "Saves sync automatically before play and after changes.",
            scale,
            fontSize: compact ? 12 : 13,
            align: HorizontalAlignment.Left
        )
        {
            Name = "SaveSyncExplanation",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        explanation.AddThemeColorOverride(
            "font_color",
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(explanation);

        var status = new StyledLabel(
            "Sign in required",
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
        group.AddChild(details);
        var lastSuccessState = AddStateRow(
            details,
            "Last successful sync",
            "Not yet",
            "SaveLastSuccessState",
            scale,
            compact
        );
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
            "Steam",
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
        LauncherButtonStyles.ApplyPrimaryAction(syncNow, scale);

        var pull = AddActionButton(
            actions,
            "Pull from Steam",
            scale,
            () => SavePullPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(pull, scale);

        var push = AddActionButton(
            actions,
            "Push to Steam",
            scale,
            () => SavePushPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(push, scale);

        return (
            group,
            syncNow,
            pull,
            push,
            status,
            lastSuccessState,
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

    internal void SetSaveSyncPresentation(
        string headline,
        string lastSuccess,
        string localState,
        string steamState
    )
    {
        _saveSyncStatus.Text = PresentationText(headline, "Sign in required");
        _saveLastSuccessState.Text = PresentationText(lastSuccess, "Not yet");
        _saveLocalState.Text = PresentationText(localState, "Checking...");
        _saveSteamState.Text = PresentationText(steamState, "Checking...");
        _homeSaveState.Text = _saveSyncStatus.Text;
    }
}
