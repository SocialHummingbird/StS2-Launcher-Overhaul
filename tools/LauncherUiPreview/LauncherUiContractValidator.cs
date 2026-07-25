using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Launcher;

namespace LauncherUiPreview;

internal static class LauncherUiContractValidator
{
    internal static void Validate(LauncherView view, Control root)
    {
        var events = new EventCounts();
        WireEvents(view, events);

        view.SelectDestination(LauncherDestination.Home);
        Press(root, "Vulkan");
        Press(root, "Start Game");
        Press(root, "Safe Start");
        Expect(events.RendererMode == 1, "Renderer selection event was not preserved.");
        Expect(events.Launch == 1, "Start Game event was not preserved.");
        Expect(events.SafeLaunch == 1, "Safe Start event was not preserved.");

        view.SelectDestination(LauncherDestination.Saves);
        Press(root, "Pull Saves from Steam Cloud");
        ToggleStartingWith(root, "Local Backup:");
        ToggleStartingWith(root, "Game Cloud Sync:");
        view.RefreshCloudPushEligibility();
        Press(root, "Upload Saves to Steam Cloud");
        Expect(events.CloudPull == 1, "Steam Cloud Pull event was not preserved.");
        Expect(events.LocalBackup == 1, "Local backup toggle event was not preserved.");
        Expect(events.CloudSync == 1, "Cloud sync toggle event was not preserved.");
        Expect(
            events.CloudPushArm == 3,
            "Steam Cloud Push eligibility was not checked after the backup toggle, refresh, and arm."
        );
        Expect(events.CloudPush == 0, "Steam Cloud Push fired before explicit confirmation.");
        FindVisibleButton(root, "Confirm: Overwrite Steam Cloud");
        view.SetPushPullDisabled(true);
        Press(root, "Cancel Cloud Operation");
        Expect(
            events.CloudOperationCancel == 1,
            "Cloud operation cancellation event was not preserved."
        );

        view.SelectDestination(LauncherDestination.Versions);
        Press(root, "Check for Updates");
        Press(root, "Refresh Game Versions");
        Press(root, "Redownload Selected Version");
        Press(root, "Clear Cached Versions");
        var branch = FindVisible<OptionButton>(root);
        branch.EmitSignal(OptionButton.SignalName.ItemSelected, branch.Selected);
        Expect(events.CheckForUpdates == 1, "Update event was not preserved.");
        Expect(events.RefreshVersions == 1, "Refresh versions event was not preserved.");
        Expect(events.Redownload == 1, "Redownload event was not preserved.");
        Expect(events.ClearVersions == 1, "Clear cached versions event was not preserved.");
        Expect(events.BranchChanged == 1, "Branch selection event was not preserved.");

        view.SelectDestination(LauncherDestination.Mods);
        Press(root, "Sync Workshop Mods");
        Press(root, "Clear Staged Mods");
        Expect(events.WorkshopSync == 1, "Workshop sync event was not preserved.");
        Expect(events.WorkshopClear == 1, "Workshop clear event was not preserved.");

        view.SelectDestination(LauncherDestination.Help);
        Press(root, "Create Help Report");
        Press(root, "Show Last Problem");
        Press(root, "Copy Launcher Log (Review First)");
        Expect(events.Diagnostics == 1, "Help report event was not preserved.");
        Expect(events.ShowLastError == 1, "Last problem event was not preserved.");
        Expect(events.CopyLog == 1, "Copy log event was not preserved.");

        view.ShowRetry();
        Press(root, "Retry");
        Expect(events.Retry == 1, "Retry event was not preserved.");

        view.ShowDownloadAction("Download Game");
        Press(root, "Download Game");
        Expect(events.Download == 1, "Download event was not preserved.");

        view.ShowCodePrompt(wasIncorrect: false);
        FindLineEdit(root, "Steam Guard code").Text = "ABCDE";
        Press(root, "Verify Code");
        Expect(events.Code == 1, "Steam Guard submission event was not preserved.");

        view.SetLoginFormVisible(visible: true, disabled: false);
        FindLineEdit(root, "Steam Username").Text = "preview-user";
        FindLineEdit(root, "Steam Password").Text = "preview-password";
        Press(root, "Sign in");
        Expect(events.Login == 1, "Steam login event was not preserved.");
    }

    private static void WireEvents(LauncherView view, EventCounts events)
    {
        view.WireEvents(
            loginRequested: (_, _) => events.Login++,
            codeSubmitted: _ => events.Code++,
            downloadRequested: () => events.Download++,
            gameBranchChanged: _ => events.BranchChanged++,
            rendererModeChanged: _ => events.RendererMode++,
            launchPressed: () => events.Launch++,
            retryPressed: () => events.Retry++,
            localBackupToggled: _ => events.LocalBackup++,
            cloudSyncToggled: _ => events.CloudSync++,
            cloudPushArmRequested: () =>
            {
                events.CloudPushArm++;
                return new CloudPushEligibilityResult(
                    Array.Empty<CloudPushEligibilityBlock>()
                );
            },
            cloudPushPressed: () => events.CloudPush++,
            cloudPullPressed: () => events.CloudPull++,
            cloudOperationCancelPressed: () =>
                events.CloudOperationCancel++,
            checkForUpdatesPressed: () => events.CheckForUpdates++,
            refreshGameVersionsPressed: () => events.RefreshVersions++,
            redownloadPressed: () => events.Redownload++,
            clearCachedVersionsPressed: () => events.ClearVersions++,
            diagnosticsPressed: () => events.Diagnostics++,
            showLastErrorPressed: () => events.ShowLastError++,
            copyRawLogPressed: () => events.CopyLog++,
            safeLaunchPressed: () => events.SafeLaunch++,
            workshopSyncPressed: () => events.WorkshopSync++,
            workshopClearPressed: () => events.WorkshopClear++
        );
    }

    private static void Press(Node root, string text)
        => FindVisibleButton(root, text).EmitSignal(Button.SignalName.Pressed);

    private static void ToggleStartingWith(Node root, string text)
    {
        var button = FindVisibleButtonStartingWith(root, text);
        Expect(button.ToggleMode, $"Expected a toggle-mode button: {text}");
        button.ButtonPressed = !button.ButtonPressed;
    }

    private static Button FindVisibleButton(Node root, string text)
    {
        foreach (var button in Descendants<Button>(root))
        {
            if (button.IsVisibleInTree()
                && string.Equals(button.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        throw new InvalidOperationException($"Visible button not found: {text}");
    }

    private static Button FindVisibleButtonStartingWith(Node root, string text)
    {
        foreach (var button in Descendants<Button>(root))
        {
            if (button.IsVisibleInTree()
                && button.Text.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        throw new InvalidOperationException($"Visible button not found with prefix: {text}");
    }

    private static LineEdit FindLineEdit(Node root, string placeholder)
    {
        foreach (var input in Descendants<LineEdit>(root))
        {
            if (string.Equals(input.PlaceholderText, placeholder, StringComparison.OrdinalIgnoreCase))
                return input;
        }

        throw new InvalidOperationException($"Input not found: {placeholder}");
    }

    private static T FindVisible<T>(Node root) where T : CanvasItem
    {
        foreach (var item in Descendants<T>(root))
        {
            if (item.IsVisibleInTree())
                return item;
        }

        throw new InvalidOperationException($"Visible {typeof(T).Name} not found.");
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match)
                yield return match;
            foreach (var descendant in Descendants<T>(child))
                yield return descendant;
        }
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class EventCounts
    {
        internal int Login;
        internal int Code;
        internal int Download;
        internal int BranchChanged;
        internal int RendererMode;
        internal int Launch;
        internal int Retry;
        internal int LocalBackup;
        internal int CloudSync;
        internal int CloudPushArm;
        internal int CloudPush;
        internal int CloudPull;
        internal int CloudOperationCancel;
        internal int CheckForUpdates;
        internal int RefreshVersions;
        internal int Redownload;
        internal int ClearVersions;
        internal int Diagnostics;
        internal int ShowLastError;
        internal int CopyLog;
        internal int SafeLaunch;
        internal int WorkshopSync;
        internal int WorkshopClear;
    }
}
