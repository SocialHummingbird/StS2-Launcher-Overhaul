using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using STS2Mobile;
using STS2Mobile.Launcher;

namespace LauncherUiPreview;

internal static class LauncherUiInteractionTest
{
    internal static void Run(LauncherView view, Control root)
    {
        var events = new EventCounts();
        LauncherPreferences.SaveGameBranch("public");
        var model = new LauncherModel(AppPaths.AppPrivateDataDir);
        var diagnostics = new LauncherDiagnosticsCoordinator(model, view);
        var launch = new LauncherLaunchCoordinator(model, view, diagnostics);
        var versions = new LauncherVersionCoordinator(model, view);
        var downloads = new LauncherDownloadCoordinator(
            model,
            view,
            launch,
            versions.RefreshGameBranchOptions
        );
        var branchSwitch = new LauncherBranchSwitchCoordinator(
            model,
            view,
            versions,
            launch,
            downloads
        );
        WireEvents(view, events, branchSwitch.GameBranchChanged);

        AssertNavigationAndStatusShell(view, root);
        AssertVersionsJourney(view, root, events);
        AssertHomeInterventionJourney(view, root, events);
        AssertSavesJourney(view, root, events);
        AssertHelpJourney(view, root, events);
        view.HideActions();
        Press(root, "Mods");
        view.SetModsPresentation(VanillaDisabledPresentation());
        AssertModsPresentation(
            root,
            "Uses Vanilla saves",
            LauncherModPlayMode.Vanilla,
            enabled: false,
            "Not run with this setup"
        );
        Press(root, "Modded");
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: false);
        view.SetModsPresentation(DisabledModdedPresentation());
        AssertModsPresentation(
            root,
            "Uses Modded saves \u00B7 0 mods enabled",
            LauncherModPlayMode.Modded,
            enabled: false,
            "Not run with this setup"
        );
        PressImporterToggle(root);
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: true);
        view.SetModsPresentation(CurrentModdedPresentation());
        AssertModsPresentation(
            root,
            "Uses Modded saves \u00B7 1 mod enabled",
            LauncherModPlayMode.Modded,
            enabled: true,
            "Active last launch"
        );
        view.ShowLaunchActions("Play", showUpdate: true);
        Press(root, "Home");
        AssertReadyHome(
            root,
            "Public \u00B7 Modded saves \u00B7 Synced",
            "Play Modded \u00B7 1 mod"
        );
        Press(root, "Mods");
        PressImporterToggle(root);
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: false);
        view.SetModsPresentation(DisabledModdedPresentation());
        PressImporterToggle(root);
        AssertPersistedSelection(LauncherModPlayMode.Modded, importerEnabled: true);

        view.SetModsPresentation(PartialModdedPresentation());
        AssertModsPresentation(
            root,
            "Uses Modded saves \u00B7 1 mod enabled",
            LauncherModPlayMode.Modded,
            enabled: true,
            "Partly loaded"
        );
        view.SetModsPresentation(FailedModdedPresentation());
        AssertModsPresentation(
            root,
            "Uses Modded saves \u00B7 1 mod enabled",
            LauncherModPlayMode.Modded,
            enabled: true,
            "Failed last launch"
        );
        view.SetModsPresentation(StaleModdedPresentation());
        AssertModsPresentation(
            root,
            "Uses Modded saves \u00B7 1 mod enabled",
            LauncherModPlayMode.Modded,
            enabled: true,
            "Not run with this setup"
        );
        view.SetModsPresentation(MissingResultModdedPresentation());
        AssertModsPresentation(
            root,
            "Uses Modded saves \u00B7 1 mod enabled",
            LauncherModPlayMode.Modded,
            enabled: true,
            "Not run with this setup"
        );

        AssertPrimaryHierarchy(root, LauncherDestination.Mods);
        Press(root, "Remove downloaded Workshop mods\u2026");
        AssertConfirmation(
            root,
            "Remove the launcher's local Workshop mod copies? Your selections and cached downloads remain, but these mods cannot load until Workshop mods are updated again.",
            "Cancel",
            "Remove downloaded Workshop mods"
        );
        PressConfirmation(root, "Cancel");
        Expect(events.WorkshopRemove == 0, "Cancel removed downloaded Workshop mods.");
        Press(root, "Remove downloaded Workshop mods\u2026");
        PressConfirmation(root, "Remove downloaded Workshop mods");
        Expect(events.WorkshopRemove == 1, "Confirmed Workshop removal did not route once.");

        view.SetModsPresentation(FailedModdedPresentation());
        Press(root, "Vanilla");
        AssertPersistedSelection(LauncherModPlayMode.Vanilla, importerEnabled: true);
        view.SetModsPresentation(VanillaFailedPresentation());
        AssertModsPresentation(
            root,
            "Uses Vanilla saves",
            LauncherModPlayMode.Vanilla,
            enabled: true,
            "Failed last launch"
        );

        view.ShowLaunchActions("Play", showUpdate: true);
        Press(root, "Home");
        AssertReadyHome(root, "Public · Vanilla saves · Synced", "Play Vanilla");
        var play = FindVisibleButton(root, "Play Vanilla");
        Expect(
            !play.Disabled,
            "Vanilla Play became unavailable because mod loading failed."
        );
        play.EmitSignal(Button.SignalName.Pressed);

        Expect(events.Launch == 1, "Play callback was not invoked exactly once.");
        Expect(events.Retry == 1, "Retry callback was not invoked exactly once.");
        Expect(events.SaveSyncNow == 1, "Sync now callback was not invoked exactly once.");
        Expect(events.SaveGet == 1, "Get saves callback was not invoked exactly once.");
        Expect(events.SaveSend == 1, "Send saves callback was not invoked exactly once.");
        Expect(events.CheckForUpdates == 1, "Check for updates callback was not invoked exactly once.");
        Expect(events.RefreshVersions == 1, "Refresh list callback was not invoked exactly once.");
        Expect(events.UpdateSelectedVersion == 1, "Update selected version did not route exactly once.");
        Expect(events.SafeLaunch == 1, "Safe Start callback was not invoked exactly once.");
        Expect(events.Diagnostics == 1, "Create support report did not route exactly once.");
        Expect(events.ShowLastError == 1, "View last error did not route exactly once.");
        Expect(events.CopyRawLog == 1, "Copy launcher log did not route exactly once.");
        Expect(events.ModsSelectionChanged == 5, "Mode selectors or the importer toggle did not persist each change.");
    }

    private static void AssertHelpJourney(
        LauncherView view,
        Node root,
        EventCounts events
    )
    {
        view.SelectDestination(LauncherDestination.Help);
        var help = FindUniqueVisibleNamed<Control>(root, "HelpDestination");
        var guidance = FindUniqueVisibleNamed<Label>(root, "HelpRecoveryGuidance");
        Expect(
            guidance.Text == "If the game freezes or shows a black screen, try Safe Start.",
            "Help did not show the exact recovery guidance."
        );

        var safeStart = FindVisibleButton(help, "Safe Start");
        Expect(!safeStart.Disabled, "Safe Start was visible but unavailable.");
        Expect(
            guidance.GetIndex() < safeStart.GetIndex(),
            "Safe Start appeared before its concise recovery guidance."
        );
        Expect(
            RenderedButtonText(safeStart) == "Safe Start",
            "Safe Start retained a vague secondary label."
        );
        Expect(
            safeStart.AccessibilityDescription
                == "Uses local saves and skips shader warmup for one run. Uses OpenGL on PowerVR; otherwise uses the game's default renderer.",
            "Safe Start did not state its one-run save, warmup, and renderer behavior truthfully."
        );
        safeStart.EmitSignal(Button.SignalName.Pressed);

        var graphics = FindUniqueVisibleNamed<Control>(root, "GraphicsSection");
        AssertVisibleLabelText(graphics, "GraphicsSectionLabel", "Graphics");
        var auto = FindUniqueVisibleNamed<Button>(graphics, "RendererAuto");
        var vulkan = FindUniqueVisibleNamed<Button>(graphics, "RendererVulkan");
        var openGl = FindUniqueVisibleNamed<Button>(graphics, "RendererOpenGL");
        Expect(
            auto.ButtonPressed && !vulkan.ButtonPressed && !openGl.ButtonPressed,
            "Auto was not the normal renderer choice."
        );
        Expect(
            !auto.Disabled && !vulkan.Disabled && !openGl.Disabled,
            "The normal fixture unexpectedly disabled a Graphics choice."
        );
        openGl.EmitSignal(Button.SignalName.Pressed);
        Expect(
            openGl.ButtonPressed && !auto.ButtonPressed && !vulkan.ButtonPressed,
            "OpenGL did not become the visibly selected renderer."
        );
        auto.EmitSignal(Button.SignalName.Pressed);
        Expect(
            events.RendererModes.SequenceEqual(
                new[] { LauncherRendererMode.OpenGl, LauncherRendererMode.Auto }
            ) && auto.ButtonPressed,
            "Graphics selection did not route OpenGL then restore Auto."
        );

        var diagnostics = FindUniqueVisibleNamed<Control>(root, "HelpDiagnosticsGroup");
        AssertVisibleLabelText(diagnostics, "HelpDiagnosticsLabel", "Diagnostics");
        foreach (var action in new[]
        {
            "Create support report",
            "View last error",
            "Copy launcher log",
        })
        {
            var button = FindVisibleButton(diagnostics, action);
            Expect(!button.Disabled, $"Diagnostic action '{action}' was unavailable.");
            button.EmitSignal(Button.SignalName.Pressed);
        }

        var helpButtonNames = Descendants<Button>(help)
            .Select(AccessibleButtonText)
            .ToArray();
        foreach (var versionsOnlyAction in new[]
        {
            "Check for updates",
            "Refresh list",
            "Repair current version",
            "Remove old versions",
        })
        {
            Expect(
                helpButtonNames.All(name =>
                    !name.StartsWith(versionsOnlyAction, StringComparison.OrdinalIgnoreCase)
                ),
                $"Versions action '{versionsOnlyAction}' leaked into Help."
            );
        }

        var toggle = FindUniqueVisibleNamed<Button>(root, "TechnicalDetailsToggle");
        Expect(
            AccessibleButtonText(toggle) == "Show technical details",
            "The closed technical drawer had the wrong action label."
        );
        toggle.EmitSignal(Button.SignalName.Pressed);
        var drawer = FindUniqueVisibleNamed<Control>(root, "TechnicalDetailsDrawer");
        AssertVisibleLabelText(drawer, "TechnicalDetailsTitle", "Technical details");
        Expect(
            AccessibleButtonText(toggle) == "Hide technical details",
            "The open technical drawer did not expose a direct close action."
        );
        AssertNoFillerCopy(root);

        view.SelectDestination(LauncherDestination.Home);
        view.SelectDestination(LauncherDestination.Help);
        Expect(
            VisibleNamedCount<Control>(root, "TechnicalDetailsDrawer") == 0,
            "Technical details stayed open after leaving Help."
        );
        toggle = FindUniqueVisibleNamed<Button>(root, "TechnicalDetailsToggle");
        Expect(
            AccessibleButtonText(toggle) == "Show technical details",
            "Help returned with stale Hide technical details copy."
        );
        toggle.EmitSignal(Button.SignalName.Pressed);
        FindVisibleButton(root, "Hide technical details").EmitSignal(Button.SignalName.Pressed);
        Expect(
            VisibleNamedCount<Control>(root, "TechnicalDetailsDrawer") == 0,
            "Technical details did not close."
        );

        help = FindUniqueVisibleNamed<Control>(root, "HelpDestination");
        var footer = FindUniqueVisibleNamed<Control>(root, "HelpAttributionFooter");
        var drawerNode = FindUniqueNamed<Control>(root, "TechnicalDetailsDrawer");
        toggle = FindUniqueVisibleNamed<Button>(root, "TechnicalDetailsToggle");
        var orderedHelpChildren = new Node[]
        {
            guidance,
            safeStart,
            graphics,
            diagnostics,
            toggle,
            drawerNode,
            footer,
        };
        Expect(
            orderedHelpChildren.All(node => ReferenceEquals(node.GetParent(), help)),
            "Help hierarchy order was compared across different containers."
        );
        Expect(
            guidance.GetIndex() < safeStart.GetIndex()
                && safeStart.GetIndex() < graphics.GetIndex()
                && graphics.GetIndex() < diagnostics.GetIndex()
                && diagnostics.GetIndex() < toggle.GetIndex()
                && toggle.GetIndex() < drawerNode.GetIndex()
                && drawerNode.GetIndex() < footer.GetIndex()
                && footer.GetIndex() == help.GetChildCount() - 1,
            "Help did not keep recovery first, diagnostics second, and attribution last."
        );
        var footerLabels = Descendants<Label>(footer)
            .Where(IsLiveAndVisible)
            .Select(label => label.Text)
            .ToArray();
        Expect(
            footerLabels.Contains("Made by SocialHummingbird", StringComparer.Ordinal)
                && footerLabels.Contains(
                    "Made using FMOD Studio by Firelight Technologies Pty Ltd.",
                    StringComparer.Ordinal
                )
                && Descendants<LinkButton>(footer).Any(link =>
                    IsLiveAndVisible(link)
                    && link.Text == "Open StS2 Launcher on GitHub"
                ),
            "The low-priority attribution footer lost its retained credits."
        );
        Expect(
            Descendants<BaseButton>(footer).All(button =>
                !button.HasMeta("launcher_primary_action")
            ),
            "Attribution competed with Safe Start."
        );
        AssertPrimaryHierarchy(root, LauncherDestination.Help);
        AssertNoFillerCopy(root);
    }

    private static void AssertVersionsJourney(
        LauncherView view,
        Node root,
        EventCounts events
    )
    {
        view.SelectDestination(LauncherDestination.Versions);
        AssertVisibleLabelText(root, "GameVersionLabel", "Game version");
        AssertVisibleLabelText(
            root,
            "SelectedGameVersionState",
            "Selected for Play \u00B7 Installed on this device"
        );
        var selector = Descendants<OptionButton>(
                FindUniqueVisibleNamed<Control>(root, "GameVersionSelection")
            )
            .Single(IsLiveAndVisible);
        Expect(
            selector.AccessibilityName == "Game version",
            "The game-version selector was not labelled."
        );
        Expect(
            selector.AccessibilityDescription == "Select the game version Play will use.",
            "The selector did not explain its launch effect."
        );
        Expect(
            string.Equals(
                selector.GetItemText(selector.Selected),
                "Default / public (installed)",
                StringComparison.OrdinalIgnoreCase
            ),
            "The visible selector did not identify the installed public version."
        );

        AssertPrimaryHierarchy(root, LauncherDestination.Versions);
        _ = FindVisibleButton(root, "Refresh list");
        _ = FindVisibleButton(root, "Repair current version");
        _ = FindVisibleButton(root, "Remove old versions...");
        AssertVisibleLabelText(root, "RepairAndStorageLabel", "Repair and storage");
        Expect(
            Descendants<Button>(FindUniqueVisibleNamed<Control>(root, "RepairAndStorageActions"))
                .Where(IsLiveAndVisible)
                .All(button => button.CustomMinimumSize.Y < FindVisibleButton(root, "Check for updates").CustomMinimumSize.Y),
            "Repair and storage actions competed with the update action."
        );
        foreach (var repeated in new[]
        {
            "Version target",
            "Ready version",
            "Play uses this version",
        })
        {
            Expect(
                VisibleUiStrings(root).All(text =>
                    !text.Contains(repeated, StringComparison.OrdinalIgnoreCase)
                ),
                $"Versions still repeated '{repeated}'."
            );
        }

        Press(root, "Check for updates");
        Press(root, "Refresh list");
        UpdateCheckViewUpdate.Completed(hasUpdate: true, "Public").Apply(view);
        AssertPrimaryHierarchy(
            root,
            LauncherDestination.Versions,
            "Update selected version"
        );
        _ = FindVisibleButton(root, "Update selected version");
        _ = FindUniqueVisibleNamed<Control>(root, "VersionsDestination");
        Press(root, "Update selected version");
        UpdateCheckViewUpdate.Completed(hasUpdate: false, "Public").Apply(view);
        _ = FindVisibleButton(root, "Check for updates");
        AssertVisibleLabelText(
            root,
            "SelectedGameVersionState",
            "Selected for Play · Installed on this device · Up to date"
        );

        var betaIndex = Enumerable.Range(0, selector.ItemCount)
            .Single(index => string.Equals(
                selector.GetItemMetadata(index).AsString(),
                "beta",
                StringComparison.OrdinalIgnoreCase
            ));
        selector.Select(betaIndex);
        selector.EmitSignal(OptionButton.SignalName.ItemSelected, betaIndex);
        Expect(
            LauncherPreferences.ReadGameBranch() == "public",
            "Changing the selector persisted before confirmation."
        );
        PressConfirmation(root, "Switch Version");
        Expect(
            LauncherPreferences.ReadActionPreferences().GameBranch == "beta",
            "The confirmed version did not survive a fresh preference load."
        );
        _ = FindUniqueVisibleNamed<Control>(root, "HomeDestination");
        var download = FindUniqueVisibleNamed<Button>(root, "DownloadGameFilesAction");
        Expect(
            download.HasMeta("launcher_primary_action")
                && download.GetMeta("launcher_primary_action").AsBool(),
            "The uninstalled selected version did not reach the primary download path."
        );
        Expect(
            AccessibleButtonText(download).StartsWith(
                "Download",
                StringComparison.OrdinalIgnoreCase
            ),
            "The selected-version download action was not labelled directly."
        );
        Expect(
            Descendants<Button>(root).Count(button =>
                IsLiveAndVisible(button)
                    && button.HasMeta("launcher_primary_action")
                    && button.GetMeta("launcher_primary_action").AsBool()
            ) == 1,
            "The download journey competed with another primary action."
        );

        view.SelectDestination(LauncherDestination.Versions);
        AssertVisibleLabelText(
            root,
            "SelectedGameVersionState",
            "Selected for Play \u00B7 Not installed"
        );
        Expect(
            Descendants<Button>(root).Count(button =>
                IsLiveAndVisible(button)
                    && string.Equals(
                        AccessibleButtonText(button),
                        "Repair current version",
                        StringComparison.Ordinal
                    )
            ) == 0,
            "Repair was offered for a version that is not installed."
        );
        selector = Descendants<OptionButton>(
                FindUniqueVisibleNamed<Control>(root, "GameVersionSelection")
            )
            .Single(IsLiveAndVisible);
        Expect(
            string.Equals(
                selector.GetItemMetadata(selector.Selected).AsString(),
                "beta",
                StringComparison.OrdinalIgnoreCase
            ),
            "The selector did not show the freshly persisted beta version."
        );
        Expect(
            string.Equals(
                selector.GetItemText(selector.Selected),
                "beta",
                StringComparison.OrdinalIgnoreCase
            ),
            "The visible selector label did not identify the persisted beta version."
        );
        var publicIndex = Enumerable.Range(0, selector.ItemCount)
            .Single(index => string.Equals(
                selector.GetItemMetadata(index).AsString(),
                "public",
                StringComparison.OrdinalIgnoreCase
            ));
        selector.Select(publicIndex);
        selector.EmitSignal(OptionButton.SignalName.ItemSelected, publicIndex);
        PressConfirmation(root, "Switch Version");
        Expect(
            LauncherPreferences.ReadGameBranch() == "public",
            "The test did not restore the persisted public version."
        );
    }

    private static void AssertNavigationAndStatusShell(LauncherView view, Node root)
    {
        var navigation = FindUniqueNamed<Control>(root, "DestinationNavigation");
        var navigationButtons = Descendants<Button>(navigation).ToArray();
        Expect(
            navigationButtons.Select(AccessibleButtonText).SequenceEqual(
                new[] { "Home", "Saves", "Versions", "Mods", "Help" }
            ),
            "The five launcher destinations changed or were reordered."
        );

        view.SetStatus("Ready to play.", LauncherStatusSeverity.Ready);
        AssertReadyHome(root, "Public · Vanilla saves · Synced", "Play Vanilla");
        view.SetGameBranch("beta");
        AssertReadyHome(root, "Beta · Vanilla saves · Synced", "Play Vanilla");
        view.SetSaveSyncPresentation(
            new LauncherSaveSyncPresentation(
                LauncherSaveSyncState.ChangesQueued,
                "Changes queued",
                "Changes queued",
                "Changes queued",
                "Waiting to sync"
            )
        );
        AssertReadyHome(root, "Beta · Vanilla saves · Changes queued", "Play Vanilla");
        view.SetGameBranch("public");
        view.SetSaveSyncPresentation(
            UpToDateSavePresentation()
        );
        AssertReadyHome(root, "Public · Vanilla saves · Synced", "Play Vanilla");
        foreach (var destination in Enum.GetValues<LauncherDestination>())
        {
            navigationButtons[(int)destination].EmitSignal(Button.SignalName.Pressed);
            var expectedTitle = $"{destination}DestinationTitle";
            _ = FindUniqueVisibleNamed<Label>(root, expectedTitle);
            var header = FindUniqueVisibleNamed<Control>(
                root,
                $"{destination}DestinationHeader"
            );
            Expect(
                Descendants<Label>(header)
                    .Where(IsLiveAndVisible)
                    .Select(label => label.Text)
                    .SequenceEqual(new[] { destination.ToString() }),
                $"{destination} header still contains generic subtitle copy."
            );
            Expect(
                Enum.GetValues<LauncherDestination>().Count(candidate =>
                    VisibleNamedCount<Control>(root, $"{candidate}Destination") == 1
                ) == 1,
                $"Navigation did not display only {destination}."
            );

            AssertDestinationContentAnchor(root, destination);
            AssertPrimaryHierarchy(root, destination);
            var capsuleVisible = VisibleNamedCount<Control>(root, "GlobalStatusCapsule") == 1;
            var bannerVisible = VisibleNamedCount<Control>(root, "SecondaryStatusBanner") == 1;
            Expect(!capsuleVisible && !bannerVisible, $"Healthy status chrome was visible on {destination}.");
            AssertNoFillerCopy(root);
        }

        view.SelectDestination(LauncherDestination.Saves);
        view.SetStatus("Previous error is resolved.", LauncherStatusSeverity.Information);
        Expect(
            VisibleNamedCount<Control>(root, "SecondaryStatusBanner") == 0,
            "Information status was promoted because its prose contained 'error'."
        );

        AssertExceptionalBanner(
            view,
            root,
            "Background reconciliation is running.",
            LauncherStatusSeverity.Working,
            "Working"
        );
        AssertExceptionalBanner(
            view,
            root,
            "Please review this launcher state.",
            LauncherStatusSeverity.Warning,
            "Warning"
        );
        AssertExceptionalBanner(
            view,
            root,
            "This launcher state needs attention.",
            LauncherStatusSeverity.Error,
            "Error"
        );

        foreach (var destination in new[]
        {
            LauncherDestination.Saves,
            LauncherDestination.Versions,
            LauncherDestination.Mods,
            LauncherDestination.Help,
        })
        {
            view.SelectDestination(destination);
            view.SetStatus(
                $"Background work for {destination} is running.",
                LauncherStatusSeverity.Working
            );
            _ = FindUniqueVisibleNamed<Control>(root, "SecondaryStatusBanner");
            AssertVisibleLabelText(root, "SecondaryStatusSeverity", "Working");
        }

        view.SetStatus("Ready to play.", LauncherStatusSeverity.Ready);
        view.SelectDestination(LauncherDestination.Home);
        Expect(
            VisibleTextOccurrences(root, "Ready to play.") == 0,
            "Healthy ready-state prose was repeated on Home."
        );
    }

    private static void AssertSavesJourney(
        LauncherView view,
        Node root,
        EventCounts events
    )
    {
        view.SelectDestination(LauncherDestination.Saves);
        AssertSaveState(
            view,
            root,
            UpToDateSavePresentation(),
            "Up to date · Last synced 9 Aug 2026, 10:30",
            detailsVisible: false
        );
        AssertSaveState(
            view,
            root,
            LauncherSaveSyncPresentation.NotSyncedYet(),
            "Not synced yet",
            detailsVisible: false
        );
        AssertSaveState(
            view,
            root,
            LauncherSaveSyncPresentation.SyncFailed(
                new STS2Mobile.Steam.SaveSyncService.StatusSnapshot(
                    HasCredentials: true,
                    HasSuccessfulSync: true,
                    LastSuccessfulSyncUtc: null,
                    ChangesQueued: false,
                    RetryRequired: false
                )
            ),
            "Sync failed",
            detailsVisible: true
        );
        AssertSaveState(
            view,
            root,
            LauncherSaveSyncPresentation.Offline(
                new STS2Mobile.Steam.SaveSyncService.StatusSnapshot(
                    HasCredentials: true,
                    HasSuccessfulSync: true,
                    LastSuccessfulSyncUtc: null,
                    ChangesQueued: false,
                    RetryRequired: false,
                    Availability: STS2Mobile.Steam.SaveSyncService.SyncAvailability.Unavailable
                )
            ),
            "Offline",
            detailsVisible: true
        );

        Press(root, "Sync now");
        Press(root, "Get saves from Steam");
        Press(root, "Send saves to Steam");
        AssertSaveConfirmations(view, root);
        view.SetSaveSyncPresentation(UpToDateSavePresentation());
    }

    private static void AssertSaveState(
        LauncherView view,
        Node root,
        LauncherSaveSyncPresentation presentation,
        string expectedSummary,
        bool detailsVisible
    )
    {
        view.SetSaveSyncPresentation(presentation);
        AssertVisibleLabelText(
            root,
            "SaveNamespaceExplanation",
            "Vanilla and modded saves are separate. Both sync automatically."
        );
        var headline = FindUniqueVisibleNamed<Label>(root, "SaveSyncHeadline");
        Expect(headline.Text == expectedSummary, $"Expected save summary '{expectedSummary}'; found '{headline.Text}'.");
        Expect(
            headline.HasMeta("save_sync_state")
                && headline.GetMeta("save_sync_state").AsString()
                    == presentation.State.ToString(),
            "The Saves page lost its typed status identity."
        );
        Expect(
            VisibleNamedCount<Control>(root, "SaveSyncDetails")
                == (detailsVisible ? 1 : 0),
            $"{presentation.State} displayed the wrong endpoint-detail visibility."
        );

        var saves = FindUniqueVisibleNamed<Control>(root, "SavesDestination");
        var presentationGroup = FindUniqueVisibleNamed<Control>(
            root,
            "SaveSyncPresentation"
        );
        var topLevelCopy = Descendants<Label>(presentationGroup)
            .Where(label =>
                IsLiveAndVisible(label)
                && !IsDescendantOf(label, FindUniqueNamed<Control>(root, "SaveSyncDetails"))
            )
            .Select(label => label.Text)
            .ToArray();
        Expect(
            topLevelCopy.SequenceEqual(
                new[]
                {
                    "Vanilla and modded saves are separate. Both sync automatically.",
                    expectedSummary,
                }
            ),
            $"{presentation.State} repeated or obscured the Saves truth."
        );
        var buttons = Descendants<Button>(saves)
            .Where(button => IsLiveAndVisible(button) && button is not OptionButton)
            .ToArray();
        Expect(buttons.Length == 3, $"{presentation.State} did not expose exactly three save actions.");
        Expect(
            buttons.Select(AccessibleButtonText).SequenceEqual(
                new[]
                {
                    "Sync now",
                    "Get saves from Steam",
                    "Send saves to Steam",
                }
            ),
            $"{presentation.State} exposed unclear save actions."
        );
        var primary = buttons.Where(button =>
            button.HasMeta("launcher_primary_action")
            && button.GetMeta("launcher_primary_action").AsBool()
        ).ToArray();
        Expect(
            primary.Length == 1 && AccessibleButtonText(primary[0]) == "Sync now",
            $"{presentation.State} changed the Saves primary action."
        );
        Expect(
            buttons.Where(button => !primary.Contains(button)).All(button =>
                button.CustomMinimumSize.Y <= primary[0].CustomMinimumSize.Y * 0.9f
            ),
            $"{presentation.State} manual save directions were not secondary controls."
        );
    }

    private static void AssertSaveConfirmations(LauncherView view, Node root)
    {
        var getConfirmed = 0;
        var getCancelled = 0;
        view.ShowGetSavesOverwriteConfirmation(
            () => getConfirmed++,
            () => getCancelled++
        );
        AssertConfirmation(
            root,
            "Steam saves differ from this device. Getting them will replace or delete vanilla and modded saves on this device. Continue?",
            "Cancel",
            "Get saves from Steam"
        );
        PressConfirmation(root, "Cancel");
        Expect(getConfirmed == 0 && getCancelled == 1, "Cancel started a Get operation.");
        view.ShowGetSavesOverwriteConfirmation(
            () => getConfirmed++,
            () => getCancelled++
        );
        PressConfirmation(root, "Get saves from Steam");
        Expect(getConfirmed == 1 && getCancelled == 1, "Get confirmation routed incorrectly.");

        var sendConfirmed = 0;
        var sendCancelled = 0;
        view.ShowSendSavesOverwriteConfirmation(
            () => sendConfirmed++,
            () => sendCancelled++
        );
        AssertConfirmation(
            root,
            "This device differs from Steam. Sending these saves will replace or delete vanilla and modded saves in Steam Cloud. Continue?",
            "Cancel",
            "Send saves to Steam"
        );
        PressConfirmation(root, "Cancel");
        Expect(sendConfirmed == 0 && sendCancelled == 1, "Cancel started a Send operation.");
        view.ShowSendSavesOverwriteConfirmation(
            () => sendConfirmed++,
            () => sendCancelled++
        );
        PressConfirmation(root, "Send saves to Steam");
        Expect(sendConfirmed == 1 && sendCancelled == 1, "Send confirmation routed incorrectly.");

        var steamChoice = 0;
        var deviceChoice = 0;
        view.ShowSaveSyncConflict(() => steamChoice++, () => deviceChoice++);
        AssertConfirmation(
            root,
            "Saves changed on this device and in Steam Cloud. Choose which copy to keep; the other copy will be replaced. Vanilla and modded saves remain separate.",
            "Use Steam saves",
            "Use this device"
        );
        PressConfirmation(root, "Use Steam saves");
        Expect(steamChoice == 1 && deviceChoice == 0, "Steam conflict choice routed incorrectly.");
        view.ShowSaveSyncConflict(() => steamChoice++, () => deviceChoice++);
        PressConfirmation(root, "Use this device");
        Expect(steamChoice == 1 && deviceChoice == 1, "Device conflict choice routed incorrectly.");
    }

    private static void AssertConfirmation(
        Node root,
        string expectedMessage,
        string expectedSecondary,
        string expectedPrimary
    )
    {
        var actions = FindUniqueVisibleNamed<Control>(root, "ConfirmationActions");
        var dialog = actions.GetParent();
        Expect(
            Descendants<Label>(dialog)
                .Where(IsLiveAndVisible)
                .Any(label => label.Text == expectedMessage),
            "Confirmation did not explain the replacement consequence."
        );
        var buttons = Descendants<Button>(actions)
            .Where(IsLiveAndVisible)
            .ToArray();
        Expect(
            buttons.Select(AccessibleButtonText).SequenceEqual(
                new[] { expectedSecondary, expectedPrimary }
            ),
            "Confirmation actions were unclear or reordered."
        );
        Expect(
            buttons.Count(button => button.HasMeta("launcher_primary_action")
                && button.GetMeta("launcher_primary_action").AsBool()) == 1,
            "Confirmation did not expose one clear committing action."
        );
    }

    private static void PressConfirmation(Node root, string text)
    {
        var actions = FindUniqueVisibleNamed<Control>(root, "ConfirmationActions");
        FindVisibleButton(actions, text).EmitSignal(Button.SignalName.Pressed);
    }

    private static LauncherSaveSyncPresentation UpToDateSavePresentation()
        => new(
            LauncherSaveSyncState.UpToDate,
            "Up to date · Last synced 9 Aug 2026, 10:30",
            "Synced",
            "Up to date",
            "Up to date"
        );

    private static void AssertHomeInterventionJourney(
        LauncherView view,
        Node root,
        EventCounts events
    )
    {
        view.SelectDestination(LauncherDestination.Home);
        view.SetStatus("Game files need repair.", LauncherStatusSeverity.Warning);
        view.ShowRetry();
        AssertVisibleLabelText(root, "GlobalStatusSeverity", "Warning");
        AssertVisibleLabelText(root, "GlobalStatusMessage", "Game files need repair.");
        Expect(
            VisibleTextOccurrences(root, "Game files need repair.") == 1,
            "Home intervention warning was repeated."
        );
        var home = FindUniqueVisibleNamed<Control>(root, "HomeDestination");
        var buttons = Descendants<Button>(home)
            .Where(button => IsLiveAndVisible(button) && button is not OptionButton)
            .ToArray();
        Expect(buttons.Length == 1, "Home intervention exposed more than one action.");
        Expect(RenderedButtonText(buttons[0]) == "Try Again", "Home retry was not concise.");
        Expect(!buttons[0].Disabled, "Home retry was disabled.");
        Expect(
            buttons[0].HasMeta("launcher_primary_action")
                && buttons[0].GetMeta("launcher_primary_action").AsBool(),
            "Home retry was not the primary action."
        );
        buttons[0].EmitSignal(Button.SignalName.Pressed);

        view.SetStatus("Game startup failed last time.", LauncherStatusSeverity.Warning);
        view.ShowLaunchActions("Play", showUpdate: true);
        view.ShowHomeHelpAction();
        home = FindUniqueVisibleNamed<Control>(root, "HomeDestination");
        buttons = Descendants<Button>(home)
            .Where(button => IsLiveAndVisible(button) && button is not OptionButton)
            .ToArray();
        Expect(buttons.Length == 2, "Launch failure did not expose Play and one direct help action.");
        Expect(
            buttons.Count(button => button.HasMeta("launcher_primary_action")
                && button.GetMeta("launcher_primary_action").AsBool()) == 1,
            "Launch failure changed the single primary action hierarchy."
        );
        Expect(
            buttons.Any(button => RenderedButtonText(button) == "Open Help"),
            "Launch failure did not expose Open Help."
        );
        Expect(
            buttons.All(button => !RenderedButtonText(button).StartsWith("Safe Start", StringComparison.Ordinal)),
            "Safe Start leaked into Home."
        );
        Press(root, "Open Help");
        _ = FindUniqueVisibleNamed<Control>(root, "HelpDestination");

        view.SetStatus("Ready to play.", LauncherStatusSeverity.Ready);
        view.ShowLaunchActions("Play", showUpdate: true);
        AssertReadyHome(root, "Public · Vanilla saves · Synced", "Play Vanilla");
    }

    private static void AssertReadyHome(
        Node root,
        string expectedStateLine,
        string expectedPlay
    )
    {
        var home = FindUniqueVisibleNamed<Control>(root, "HomeDestination");
        AssertVisibleLabelText(root, "HomeStateLine", expectedStateLine);
        var journey = FindUniqueVisibleNamed<Control>(root, "HomeJourney");
        Expect(
            Descendants<Label>(journey)
                .Where(IsLiveAndVisible)
                .Select(label => label.Text)
                .SequenceEqual(new[] { expectedStateLine }),
            "Healthy Home contained more than its compact state line."
        );
        foreach (var removed in new[]
        {
            "HomeJourneyRows",
            "HomeAccountState",
            "HomeGameState",
            "HomeSaveState",
            "HomeSaveNamespaceState",
        })
        {
            Expect(VisibleNamedCount<CanvasItem>(root, removed) == 0, $"Legacy Home node {removed} remained visible.");
        }

        var buttons = Descendants<Button>(home)
            .Where(button => IsLiveAndVisible(button) && button is not OptionButton)
            .ToArray();
        Expect(buttons.Length == 1, "Ready Home exposed more than one action.");
        Expect(RenderedButtonText(buttons[0]) == expectedPlay, $"Ready Home action was '{RenderedButtonText(buttons[0])}'.");
        Expect(!buttons[0].Disabled, "Ready Home Play was disabled.");
        Expect(
            buttons[0].HasMeta("launcher_primary_action")
                && buttons[0].GetMeta("launcher_primary_action").AsBool(),
            "Ready Home Play was not the primary action."
        );
    }

    private static void AssertPrimaryHierarchy(
        Node root,
        LauncherDestination destination,
        string expectedOverride = null
    )
    {
        var page = FindUniqueVisibleNamed<Control>(root, $"{destination}Destination");
        var buttons = Descendants<Button>(page)
            .Where(button => IsLiveAndVisible(button) && button is not OptionButton)
            .ToArray();
        var primary = buttons.Where(button =>
            button.HasMeta("launcher_primary_action")
            && button.GetMeta("launcher_primary_action").AsBool()
        ).ToArray();
        Expect(primary.Length == 1, $"{destination} did not have exactly one primary action.");

        var expected = expectedOverride ?? destination switch
        {
            LauncherDestination.Home => "Play Vanilla",
            LauncherDestination.Saves => "Sync now",
            LauncherDestination.Versions => "Check for updates",
            LauncherDestination.Mods => "Update Workshop mods",
            LauncherDestination.Help => "Safe Start",
            _ => throw new ArgumentOutOfRangeException(nameof(destination)),
        };
        Expect(
            string.Equals(
                AccessibleButtonText(primary[0]),
                expected,
                StringComparison.OrdinalIgnoreCase
            ),
            $"{destination} primary action was '{AccessibleButtonText(primary[0])}', expected '{expected}'."
        );

        var secondary = buttons.Where(button => !primary.Contains(button)).ToArray();
        Expect(
            secondary.All(button =>
                button.CustomMinimumSize.Y <= primary[0].CustomMinimumSize.Y * 0.9f
            ),
            $"{destination} secondary actions were not visually smaller than its primary action."
        );
    }

    private static void AssertDestinationContentAnchor(Node root, LauncherDestination destination)
    {
        switch (destination)
        {
            case LauncherDestination.Home:
                _ = FindUniqueVisibleNamed<Control>(root, "HomeJourney");
                break;
            case LauncherDestination.Saves:
                _ = FindUniqueVisibleNamed<Control>(root, "SaveSyncPresentation");
                break;
            case LauncherDestination.Versions:
                Expect(
                    Descendants<OptionButton>(root).Count(IsLiveAndVisible) == 1,
                    "Versions did not begin with its game-version control."
                );
                break;
            case LauncherDestination.Mods:
                _ = FindUniqueVisibleNamed<Control>(root, "ModsControls");
                break;
            case LauncherDestination.Help:
                Expect(
                    Descendants<Button>(FindUniqueVisibleNamed<Control>(root, "HelpDestination"))
                        .Any(button => IsLiveAndVisible(button)
                            && string.Equals(button.AccessibilityName, "Safe Start", StringComparison.Ordinal)),
                    "Help did not begin with Safe Start."
                );
                break;
        }
    }

    private static void AssertExceptionalBanner(
        LauncherView view,
        Node root,
        string message,
        LauncherStatusSeverity severity,
        string expectedSeverity
    )
    {
        view.SetStatus(message, severity);
        _ = FindUniqueVisibleNamed<Control>(root, "SecondaryStatusBanner");
        AssertVisibleLabelText(root, "SecondaryStatusSeverity", expectedSeverity);
        AssertVisibleLabelText(root, "SecondaryStatusMessage", message);
    }

    private static void AssertNoFillerCopy(Node root)
    {
        var forbidden = new[]
        {
            "Selector",
            "Confirmation",
            "Review first",
            "Private until opened",
            "Share details",
            "Open details",
            "Help & Reports",
            "Compatibility mode",
            "Check status, then play.",
            "Automatic sync or choose a direction.",
            "Choose or repair game files.",
            "Choose vanilla or mods.",
            "Compatibility, repair, and reports.",
        };
        var visible = VisibleUiStrings(root).ToArray();
        foreach (var phrase in forbidden)
        {
            Expect(
                visible.All(text => !text.Contains(phrase, StringComparison.OrdinalIgnoreCase)),
                $"Visible launcher copy still contains '{phrase}'."
            );
        }
    }

    private static int VisibleTextOccurrences(Node root, string expected)
        => VisibleUiStrings(root).Count(text => string.Equals(text, expected, StringComparison.Ordinal));

    private static IEnumerable<string> VisibleUiStrings(Node root)
    {
        foreach (var label in Descendants<Label>(root).Where(IsLiveAndVisible))
            if (!string.IsNullOrWhiteSpace(label.Text))
                yield return label.Text;
        foreach (var richText in Descendants<RichTextLabel>(root).Where(IsLiveAndVisible))
            if (!string.IsNullOrWhiteSpace(richText.Text))
                yield return richText.Text;
        foreach (var button in Descendants<Button>(root).Where(IsLiveAndVisible))
        {
            if (!string.IsNullOrWhiteSpace(button.Text))
                yield return button.Text;
            if (!string.IsNullOrWhiteSpace(button.AccessibilityName))
                yield return button.AccessibilityName;
            if (!string.IsNullOrWhiteSpace(button.AccessibilityDescription))
                yield return button.AccessibilityDescription;
        }
    }

    private static string AccessibleButtonText(Button button)
        => string.IsNullOrWhiteSpace(button.AccessibilityName)
            ? button.Text
            : button.AccessibilityName;

    private static string RenderedButtonText(Button button)
    {
        if (!string.IsNullOrWhiteSpace(button.Text))
            return button.Text;

        var labels = Descendants<Label>(button)
            .Where(IsLiveAndVisible)
            .Select(label => label.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();
        if (labels.Length >= 2)
            return $"{labels[0]}: {labels[1]}";
        if (labels.Length == 1)
            return labels[0];
        return button.AccessibilityName ?? "";
    }

    private static void AssertModsPresentation(
        Node root,
        string expectedSummary,
        LauncherModPlayMode expectedMode,
        bool enabled,
        string expectedResult
    )
    {
        AssertVisibleLabelText(root, "ModsLaunchSummary", expectedSummary);
        Expect(
            VisibleNamedCount<CanvasItem>(root, "SelectedModMode") == 0
                && VisibleNamedCount<CanvasItem>(root, "NextSaveNamespace") == 0,
            "Mods still exposed duplicate selected-mode or save-set labels."
        );

        var vanilla = FindVisibleButton(root, "Vanilla");
        var modded = FindVisibleButton(root, "Modded");
        Expect(
            (expectedMode == LauncherModPlayMode.Vanilla
                ? vanilla.AccessibilityDescription
                : modded.AccessibilityDescription) == "Selected mode",
            "The selected mod mode was not stated by its selector."
        );
        Expect(
            (expectedMode == LauncherModPlayMode.Vanilla
                ? modded.AccessibilityDescription
                : vanilla.AccessibilityDescription) == "",
            "Both mod mode selectors appeared selected."
        );

        var list = FindUniqueVisibleNamed<Control>(root, "ModsList");
        var rows = Descendants<PanelContainer>(list)
            .Where(row => IsLiveAndVisible(row) && row.Name == "ModRow")
            .ToArray();
        Expect(rows.Length == 1, $"Expected one representative mod row; found {rows.Length}.");
        var row = rows[0];
        AssertVisibleLabelText(row, "ModName", "Import Vanilla Saves");
        AssertVisibleLabelText(row, "ModSource", "Workshop");
        AssertVisibleLabelText(row, "ModRuntimeResult", expectedResult);

        var toggles = Descendants<CheckButton>(row)
            .Where(toggle => IsLiveAndVisible(toggle) && toggle.Name == "ModEnabledToggle")
            .ToArray();
        Expect(toggles.Length == 1, "The representative mod did not expose one Enabled toggle.");
        Expect(toggles[0].Text == "Enabled", "The mod toggle was not labelled directly.");
        Expect(toggles[0].ButtonPressed == enabled, "The Enabled toggle did not reflect persisted selection.");

        var renderedLabels = Descendants<Label>(row)
            .Where(IsLiveAndVisible)
            .Select(label => label.Text)
            .ToArray();
        foreach (var obsolete in new[]
        {
            "Installed",
            "Enabled for Modded",
            "Loaded last launch",
            "Not tested yet",
        })
        {
            Expect(
                renderedLabels.All(text =>
                    !text.Contains(obsolete, StringComparison.OrdinalIgnoreCase)
                ),
                $"The simplified mod row still rendered '{obsolete}'."
            );
        }
    }

    private static void PressImporterToggle(Node root)
    {
        var list = FindUniqueVisibleNamed<Control>(root, "ModsList");
        var toggle = Descendants<CheckButton>(list)
            .Single(item => IsLiveAndVisible(item) && item.Name == "ModEnabledToggle");
        toggle.EmitSignal(Button.SignalName.Pressed);
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

    private static LauncherModsPresentation VanillaFailedPresentation()
    {
        var failed = FailedModdedPresentation();
        return new LauncherModsPresentation(
            LauncherModPlayMode.Vanilla,
            "preview-vanilla-failed",
            "Vanilla saves",
            failed.Mods,
            failed.InstalledCount,
            failed.EnabledCount,
            hasStaleLastLaunchResult: false
        );
    }

    private static LauncherModsPresentation VanillaDisabledPresentation()
    {
        var disabled = DisabledModdedPresentation();
        return new LauncherModsPresentation(
            LauncherModPlayMode.Vanilla,
            "preview-vanilla-disabled",
            "Vanilla saves",
            disabled.Mods,
            disabled.InstalledCount,
            disabled.EnabledCount,
            hasStaleLastLaunchResult: false
        );
    }

    private static LauncherModsPresentation ImporterPresentation(
        string fingerprint,
        bool enabled,
        LauncherModLastLaunchState state,
        bool stale
    )
        => ModsPresentation(
            fingerprint,
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
        params LauncherModPresentationItem[] mods
    )
    {
        var enabledCount = mods.Count(mod => mod.Enabled);
        return new LauncherModsPresentation(
            LauncherModPlayMode.Modded,
            fingerprint,
            "Modded saves",
            mods,
            mods.Count(mod => mod.Installed),
            enabledCount,
            mods.Any(mod => mod.IsLastLaunchStale)
        );
    }

    private static void Press(Node root, string text)
        => FindVisibleButton(root, text).EmitSignal(Button.SignalName.Pressed);

    private static Button FindVisibleButton(Node root, string text)
    {
        var matches = Descendants<Button>(root)
            .Where(button =>
                IsLiveAndVisible(button)
                && (string.Equals(RenderedButtonText(button), text, StringComparison.Ordinal)
                    || string.Equals(AccessibleButtonText(button), text, StringComparison.Ordinal))
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

    private static T FindUniqueNamed<T>(Node root, string name)
        where T : Node
    {
        var matches = Descendants<T>(root)
            .Where(item => string.Equals(item.Name.ToString(), name, StringComparison.Ordinal))
            .ToArray();
        Expect(matches.Length == 1, $"Expected one node named {name}; found {matches.Length}.");
        return matches[0];
    }

    private static int VisibleNamedCount<T>(Node root, string name)
        where T : CanvasItem
        => Descendants<T>(root).Count(item =>
            IsLiveAndVisible(item)
            && string.Equals(item.Name.ToString(), name, StringComparison.Ordinal)
        );

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

    private static bool IsDescendantOf(Node item, Node ancestor)
    {
        for (var current = item.GetParent(); current != null; current = current.GetParent())
            if (ReferenceEquals(current, ancestor))
                return true;
        return false;
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

    private static void WireEvents(
        LauncherView view,
        EventCounts events,
        Action<string> gameBranchChanged
    )
    {
        view.WireEvents(
            loginRequested: (_, _) => { },
            codeSubmitted: _ => { },
            downloadRequested: () => { },
            gameBranchChanged: gameBranchChanged,
            rendererModeChanged: mode => events.RendererModes.Add(mode),
            launchPressed: () => events.Launch++,
            retryPressed: () => events.Retry++,
            checkForUpdatesPressed: () => events.CheckForUpdates++,
            updateSelectedVersionPressed: () => events.UpdateSelectedVersion++,
            refreshGameVersionsPressed: () => events.RefreshVersions++,
            redownloadPressed: () => { },
            clearCachedVersionsPressed: () => { },
            diagnosticsPressed: () => events.Diagnostics++,
            showLastErrorPressed: () => events.ShowLastError++,
            copyRawLogPressed: () => events.CopyRawLog++,
            safeLaunchPressed: () => events.SafeLaunch++,
            saveSyncNowPressed: () => events.SaveSyncNow++,
            savePullPressed: () => events.SaveGet++,
            savePushPressed: () => events.SaveSend++,
            workshopSyncPressed: () => { },
            workshopClearPressed: () =>
                LauncherWorkshopCoordinator.ShowRemoveDownloadedModsConfirmation(
                    view,
                    () => events.WorkshopRemove++
                ),
            modsSelectionChanged: () => events.ModsSelectionChanged++
        );
    }

    private sealed class EventCounts
    {
        internal int Launch;
        internal int Retry;
        internal int SaveSyncNow;
        internal int SaveGet;
        internal int SaveSend;
        internal int CheckForUpdates;
        internal int RefreshVersions;
        internal int UpdateSelectedVersion;
        internal int SafeLaunch;
        internal int Diagnostics;
        internal int ShowLastError;
        internal int CopyRawLog;
        internal readonly List<string> RendererModes = new();
        internal int WorkshopRemove;
        internal int ModsSelectionChanged;
    }
}
