using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using STS2Mobile.Launcher;

namespace LauncherUiPreview;

internal static class LauncherUiInteractionTest
{
    private static readonly string[] DestinationLabels =
        ["Home", "Saves", "Versions", "Mods", "Help"];

    internal static void Run(LauncherView view, Control root)
    {
        var events = new EventCounts();
        WireEvents(view, events);

        var navigation = FindUniqueVisibleNamed<Control>(
            root,
            "DestinationNavigation"
        );
        var destinations = Descendants<Button>(navigation)
            .Where(IsLiveAndVisible)
            .ToArray();
        Expect(
            destinations.Select(button => button.Text).SequenceEqual(DestinationLabels),
            "Launcher navigation must contain Home, Saves, Versions, Mods, and Help in order."
        );

        SelectDestination(root, "Saves", "Sync now");
        var saveActions = FindUniqueVisibleNamed<Control>(root, "SaveSyncActions");
        AssertVisibleButtonTexts(
            saveActions,
            "Sync now",
            "Pull from Steam",
            "Push to Steam"
        );
        Press(saveActions, "Sync now");
        Press(saveActions, "Pull from Steam");
        Press(saveActions, "Push to Steam");
        AssertConflictChoices(view, root, events);

        SelectDestination(root, "Versions", "Check for Updates");
        Press(root, "Check for Updates");

        SelectDestination(root, "Mods", "Sync Workshop Mods");
        Press(root, "Sync Workshop Mods");

        SelectDestination(root, "Help", "Safe Start");
        Press(root, "Safe Start");

        SelectDestination(root, "Home", "Play");
        Press(root, "Play");

        Expect(events.Launch == 1, "Play callback was not invoked exactly once.");
        Expect(events.SaveSyncNow == 1, "Sync now callback was not invoked exactly once.");
        Expect(events.SavePull == 1, "Pull callback was not invoked exactly once.");
        Expect(events.SavePush == 1, "Push callback was not invoked exactly once.");
        Expect(events.CheckForUpdates == 1, "Version action callback was not invoked exactly once.");
        Expect(events.WorkshopSync == 1, "Mods action callback was not invoked exactly once.");
        Expect(events.SafeLaunch == 1, "Help action callback was not invoked exactly once.");
    }

    private static void SelectDestination(
        Node root,
        string destination,
        string expectedAction
    )
    {
        Press(root, destination);
        _ = FindVisibleButton(root, expectedAction);
    }

    private static void AssertConflictChoices(
        LauncherView view,
        Node root,
        EventCounts events
    )
    {
        view.ShowSaveSyncConflict(
            () => events.UseSteamSaves++,
            () => events.UseDeviceSaves++
        );
        var steamChoice = FindUniqueVisibleNamed<Control>(
            root,
            "ConfirmationActions"
        );
        AssertVisibleButtonTexts(
            steamChoice,
            "Use Steam saves",
            "Use this device"
        );
        Press(steamChoice, "Use Steam saves");

        view.ShowSaveSyncConflict(
            () => events.UseSteamSaves++,
            () => events.UseDeviceSaves++
        );
        var deviceChoice = FindUniqueVisibleNamed<Control>(
            root,
            "ConfirmationActions"
        );
        AssertVisibleButtonTexts(
            deviceChoice,
            "Use Steam saves",
            "Use this device"
        );
        Press(deviceChoice, "Use this device");

        Expect(
            events.UseSteamSaves == 1 && events.UseDeviceSaves == 1,
            "Save-conflict choices did not invoke their matching callbacks."
        );
    }

    private static void Press(Node root, string text)
        => FindVisibleButton(root, text).EmitSignal(Button.SignalName.Pressed);

    private static void AssertVisibleButtonTexts(
        Node root,
        params string[] expected
    )
    {
        var actual = Descendants<Button>(root)
            .Where(IsLiveAndVisible)
            .Select(button => button.Text)
            .ToArray();
        Expect(
            actual.SequenceEqual(expected),
            $"Expected actions [{string.Join(", ", expected)}]; found [{string.Join(", ", actual)}]."
        );
    }

    private static Button FindVisibleButton(Node root, string text)
    {
        var matches = Descendants<Button>(root)
            .Where(button =>
                IsLiveAndVisible(button)
                && string.Equals(button.Text, text, StringComparison.Ordinal)
            )
            .ToArray();
        Expect(
            matches.Length == 1,
            $"Expected one visible button labelled '{text}'; found {matches.Length}."
        );
        return matches[0];
    }

    private static T FindUniqueVisibleNamed<T>(Node root, string name)
        where T : CanvasItem
    {
        var matches = Descendants<T>(root)
            .Where(item =>
                IsLiveAndVisible(item)
                && string.Equals(item.Name.ToString(), name, StringComparison.Ordinal)
            )
            .ToArray();
        Expect(
            matches.Length == 1,
            $"Expected one visible node named {name}; found {matches.Length}."
        );
        return matches[0];
    }

    private static bool IsLiveAndVisible(CanvasItem item)
    {
        for (Node current = item; current is not null; current = current.GetParent())
        {
            if (current.IsQueuedForDeletion())
                return false;
        }
        return item.IsVisibleInTree();
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

    private static void WireEvents(LauncherView view, EventCounts events)
    {
        view.WireEvents(
            loginRequested: (_, _) => { },
            codeSubmitted: _ => { },
            downloadRequested: () => { },
            gameBranchChanged: _ => { },
            rendererModeChanged: _ => { },
            launchPressed: () => events.Launch++,
            retryPressed: () => { },
            checkForUpdatesPressed: () => events.CheckForUpdates++,
            refreshGameVersionsPressed: () => { },
            redownloadPressed: () => { },
            clearCachedVersionsPressed: () => { },
            diagnosticsPressed: () => { },
            showLastErrorPressed: () => { },
            copyRawLogPressed: () => { },
            safeLaunchPressed: () => events.SafeLaunch++,
            saveSyncNowPressed: () => events.SaveSyncNow++,
            savePullPressed: () => events.SavePull++,
            savePushPressed: () => events.SavePush++,
            workshopSyncPressed: () => events.WorkshopSync++,
            workshopClearPressed: () => { }
        );
    }

    private sealed class EventCounts
    {
        internal int Launch;
        internal int SaveSyncNow;
        internal int SavePull;
        internal int SavePush;
        internal int CheckForUpdates;
        internal int WorkshopSync;
        internal int SafeLaunch;
        internal int UseSteamSaves;
        internal int UseDeviceSaves;
    }
}
