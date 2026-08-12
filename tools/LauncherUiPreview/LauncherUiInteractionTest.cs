using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using STS2Mobile.Launcher;

namespace LauncherUiPreview;

internal static class LauncherUiInteractionTest
{
    internal static void Run(LauncherView view, Control root)
    {
        var events = new EventCounts();
        WireEvents(view, events);

        view.HideActions();
        Press(root, "Mods");
        AssertVisibleLabelText(root, "SelectedModMode", "Selected mode: Vanilla");
        AssertVisibleLabelText(root, "NextSaveNamespace", "Next save set: Vanilla saves");

        _ = FindVisibleButton(root, "Vanilla \u00B7 Selected");
        Press(root, "Modded");
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: false);
        view.SetModsPresentation(DisabledModdedPresentation());
        Press(
            root,
            "Import Vanilla Saves: Workshop \u00B7 Installed \u00B7 Disabled for Modded \u00B7 Not tested yet"
        );
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: true);
        view.SetModsPresentation(CurrentModdedPresentation());
        AssertCurrentModdedPresentation(root);
        Press(
            root,
            "Import Vanilla Saves: Workshop \u00B7 Installed \u00B7 Enabled for Modded \u00B7 Loaded last launch"
        );
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: false);
        view.SetModsPresentation(DisabledModdedPresentation());
        Press(
            root,
            "Import Vanilla Saves: Workshop \u00B7 Installed \u00B7 Disabled for Modded \u00B7 Not tested yet"
        );
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: true);

        Press(root, "Vanilla");
        AssertPersistedSelection(LauncherModPlayMode.Vanilla, importerEnabled: true);
        AssertVisibleLabelText(root, "SelectedModMode", "Selected mode: Vanilla");
        AssertVisibleLabelText(root, "NextSaveNamespace", "Next save set: Vanilla saves");
        Press(root, "Modded");
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: true);
        view.SetModsPresentation(CurrentModdedPresentation());

        view.SetModsPresentation(PartialModdedPresentation());
        AssertVisibleModRows(
            root,
            "Import Vanilla Saves: Workshop \u00B7 Installed \u00B7 Enabled for Modded \u00B7 Partial"
        );
        view.SetModsPresentation(FailedModdedPresentation());
        AssertVisibleModRows(
            root,
            "Import Vanilla Saves: Workshop \u00B7 Installed \u00B7 Enabled for Modded \u00B7 Failed"
        );
        view.SetModsPresentation(StaleModdedPresentation());
        AssertVisibleModRows(
            root,
            "Import Vanilla Saves: Workshop \u00B7 Installed \u00B7 Enabled for Modded \u00B7 Not tested yet"
        );
        Expect(
            !Descendants<Button>(FindUniqueVisibleNamed<Control>(root, "ModsList"))
                .Where(IsLiveAndVisible)
                .Any(button => button.Text.Contains("Loaded last launch", StringComparison.Ordinal)),
            "A stale activation result was presented as loaded."
        );
        view.SetModsPresentation(MissingResultModdedPresentation());
        AssertVisibleModRows(
            root,
            "Import Vanilla Saves: Workshop \u00B7 Installed \u00B7 Enabled for Modded \u00B7 Not tested yet"
        );
        view.SetModsPresentation(CurrentModdedPresentation());

        Press(root, "Saves");
        AssertVisibleLabelText(
            root,
            "SaveNamespaceExplanation",
            "Both save sets sync separately. They are not merged."
        );

        view.ShowLaunchActions("Play", showUpdate: true);
        Press(root, "Home");
        AssertVisibleLabelText(root, "HomeSaveNamespaceState", "Modded saves");
        var play = FindVisibleButton(root, "Play Modded \u00B7 1 enabled");
        Expect(
            !play.Disabled,
            "Play became unavailable because the last modded launch contained a failure."
        );
        play.EmitSignal(Button.SignalName.Pressed);

        Expect(events.Launch == 1, "Play callback was not invoked exactly once.");
        Expect(events.ModsSelectionChanged == 6, "Mode selectors or mod toggles did not persist each change.");
    }

    private static void AssertCurrentModdedPresentation(Node root)
    {
        AssertVisibleLabelText(root, "SelectedModMode", "Selected mode: Modded");
        AssertVisibleLabelText(root, "NextSaveNamespace", "Next save set: Modded saves");
        _ = FindVisibleButton(root, "Modded \u00B7 Selected");
        AssertVisibleModRows(
            root,
            "Import Vanilla Saves: Workshop \u00B7 Installed \u00B7 Enabled for Modded \u00B7 Loaded last launch"
        );
    }

    private static void AssertVisibleModRows(Node root, params string[] expected)
    {
        var list = FindUniqueVisibleNamed<Control>(root, "ModsList");
        AssertVisibleButtonTexts(list, expected);
    }

    private static void AssertPersistedSelection(
        LauncherModPlayMode expectedMode,
        bool importerEnabled
    )
    {
        var reloaded = LauncherModSelectionState.Load();
        var actualMode = LauncherModSelectionState.IsModdedModeFor(reloaded)
            ? LauncherModPlayMode.Modded
            : LauncherModPlayMode.Vanilla;
        var actualEnabled = reloaded.EnabledMods.TryGetValue(
            "workshop:3747503308",
            out var enabled
        ) && enabled;
        Expect(
            actualMode == expectedMode && actualEnabled == importerEnabled,
            $"Fresh mod_selection.json load was {actualMode}/{actualEnabled}; expected {expectedMode}/{importerEnabled}."
        );
    }

    private static LauncherModsPresentation CurrentModdedPresentation()
        => ModsPresentation(
            "preview-current",
            "Play Modded \u00B7 1 enabled",
            new LauncherModPresentationItem(
                "workshop:3747503308",
                "ImportVanillaSaves",
                "Import Vanilla Saves",
                "Workshop",
                Installed: true,
                Enabled: true,
                CanChange: true,
                LastLaunchState: LauncherModLastLaunchState.LoadedLastLaunch,
                IsLastLaunchStale: false,
                Detail: "Initialization and activation verified."
            )
        );

    private static LauncherModsPresentation DisabledModdedPresentation()
        => ImporterPresentation(
            "preview-disabled",
            enabled: false,
            LauncherModLastLaunchState.NotTestedYet,
            stale: false
        );

    private static LauncherModsPresentation PartialModdedPresentation()
        => ImporterPresentation(
            "preview-partial",
            enabled: true,
            LauncherModLastLaunchState.Partial,
            stale: false
        );

    private static LauncherModsPresentation FailedModdedPresentation()
        => ImporterPresentation(
            "preview-failed",
            enabled: true,
            LauncherModLastLaunchState.Failed,
            stale: false
        );

    private static LauncherModsPresentation StaleModdedPresentation()
        => ImporterPresentation(
            "preview-stale",
            enabled: true,
            LauncherModLastLaunchState.LoadedLastLaunch,
            stale: true
        );

    private static LauncherModsPresentation MissingResultModdedPresentation()
        => ImporterPresentation(
            "preview-missing",
            enabled: true,
            LauncherModLastLaunchState.NotTestedYet,
            stale: false
        );

    private static LauncherModsPresentation ImporterPresentation(
        string fingerprint,
        bool enabled,
        LauncherModLastLaunchState state,
        bool stale
    )
        => ModsPresentation(
            fingerprint,
            $"Play Modded \u00B7 {(enabled ? 1 : 0)} enabled",
            new LauncherModPresentationItem(
                "workshop:3747503308",
                "ImportVanillaSaves",
                "Import Vanilla Saves",
                "Workshop",
                Installed: true,
                Enabled: enabled,
                CanChange: true,
                LastLaunchState: state,
                IsLastLaunchStale: stale,
                Detail: stale
                    ? "Last launch used a different mod selection."
                    : state switch
                    {
                        LauncherModLastLaunchState.Partial => "Activation was only partially verified.",
                        LauncherModLastLaunchState.Failed => "Activation failed.",
                        LauncherModLastLaunchState.LoadedLastLaunch => "Initialization and activation verified.",
                        _ => "No matching result from the last launch.",
                    }
            )
        );

    private static LauncherModsPresentation ModsPresentation(
        string fingerprint,
        string playLabel,
        params LauncherModPresentationItem[] mods
    )
        => new(
            LauncherModPlayMode.Modded,
            fingerprint,
            "Modded saves",
            playLabel,
            "Modded selected",
            mods,
            mods.Count(mod => mod.Installed),
            mods.Count(mod => mod.Enabled),
            mods.Any(mod => mod.IsLastLaunchStale)
        );

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

    private static void AssertVisibleLabelText(Node root, string name, string expected)
    {
        var label = FindUniqueVisibleNamed<Label>(root, name);
        Expect(
            string.Equals(label.Text, expected, StringComparison.Ordinal),
            $"Expected {name} to read '{expected}'; found '{label.Text}'."
        );
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
            checkForUpdatesPressed: () => { },
            refreshGameVersionsPressed: () => { },
            redownloadPressed: () => { },
            clearCachedVersionsPressed: () => { },
            diagnosticsPressed: () => { },
            showLastErrorPressed: () => { },
            copyRawLogPressed: () => { },
            safeLaunchPressed: () => { },
            saveSyncNowPressed: () => { },
            savePullPressed: () => { },
            savePushPressed: () => { },
            workshopSyncPressed: () => { },
            workshopClearPressed: () => { },
            modsSelectionChanged: () => events.ModsSelectionChanged++
        );
    }

    private sealed class EventCounts
    {
        internal int Launch;
        internal int ModsSelectionChanged;
    }
}
