#nullable enable

using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;
using STS2Mobile.Steam.Workshop;

namespace LocalGameplaySaveSafetyProbe;

internal static class Program
{
    private static int _passed;
    private static async Task Main(string[] args)
    {
        if (args.Length != 0)
            throw new ArgumentException("The focused probe does not accept arguments.");
        await RunAsync(
            "local save paths stay contained",
            LocalSavePathsStayContainedAsync
        );
        await RunAsync(
            "local save writes are atomic",
            LocalSaveWritesAreAtomicAsync
        );
        await RunAsync(
            "synchronization makes four deterministic decisions",
            SynchronizationMakesFourDecisionsAsync
        );
        await RunAsync(
            "interrupted Pull leaves local saves intact",
            InterruptedPullLeavesLocalSavesIntactAsync
        );
        await RunAsync(
            "failed Push stays dirty and retryable",
            FailedPushStaysDirtyAndRetryableAsync
        );
        await RunAsync(
            "Pull completes before save loading",
            PullCompletesBeforeSaveLoadingAsync
        );
        await RunAsync(
            "a local save queues Push without the launcher",
            LocalSaveQueuesPushWithoutLauncherAsync
        );
        await RunAsync(
            "manual Push and Pull use one synchronization service",
            ManualPushAndPullUseOneServiceAsync
        );
        await RunAsync(
            "mod selection and discovery survive restart",
            ModSelectionAndDiscoverySurviveRestartAsync
        );

        Console.WriteLine($"Focused local-save, synchronization, and mod probe passed {_passed}/9 scenarios.");
        Console.WriteLine(
            "Scope: FakeSaveRemote validates deterministic synchronization behavior only; it does not prove Steam Cloud or Android transport."
        );
        Console.WriteLine(
            "Scope: the representative mod is a desktop fixture; it does not prove Android mod activation or an in-game effect."
        );
    }

    private static Task LocalSavePathsStayContainedAsync()
    {
        var container = NewTempDirectory();
        var root = Path.Combine(container, "saves");
        try
        {
            ISaveStore store = new AndroidLocalSaveStore(root);
            store.WriteFile("profile1/saves/progress.save", "nested");
            store.WriteFile("user://settings.save", "user-data");

            Expect(
                File.ReadAllText(
                    Path.Combine(root, "profile1", "saves", "progress.save")
                ) == "nested",
                "A valid nested save was not written below the local root."
            );
            Expect(
                File.ReadAllText(Path.Combine(root, "settings.save")) == "user-data",
                "A user:// save was not written below the local root."
            );

            ExpectWriteRejected(
                store,
                "../escape.save",
                Path.Combine(container, "escape.save")
            );
            ExpectWriteRejected(
                store,
                Path.Combine(root, "rooted.save"),
                Path.Combine(root, "rooted.save")
            );
            return Task.CompletedTask;
        }
        finally
        {
            Directory.Delete(container, recursive: true);
        }
    }

    private static async Task LocalSaveWritesAreAtomicAsync()
    {
        var root = NewTempDirectory();
        try
        {
            var callbackSawCommittedBytes = false;
            ISaveStore store = new AndroidLocalSaveStore(
                root,
                path =>
                {
                    if (path == "profile.save")
                    {
                        callbackSawCommittedBytes =
                            File.ReadAllText(Path.Combine(root, path)) == "committed";
                    }
                }
            );
            store.WriteFile("profile.save", "committed");
            Expect(
                callbackSawCommittedBytes,
                "The save notification ran before the local commit."
            );

            var destination = Path.Combine(root, "current_run.save");
            File.WriteAllText(destination, "original");
            using var cancellation = new CancellationTokenSource();
            var canceled = false;
            try
            {
                await CancellableAtomicFile.WriteAsync(
                    destination,
                    overwrite: true,
                    async (stagingPath, token) =>
                    {
                        await File.WriteAllTextAsync(stagingPath, "replacement", token);
                        cancellation.Cancel();
                    },
                    cancellation.Token
                );
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }

            Expect(canceled, "The interrupted atomic replacement did not cancel.");
            Expect(
                File.ReadAllText(destination) == "original",
                "The interrupted replacement changed the committed save."
            );
            Expect(
                !Directory.EnumerateFiles(
                    root,
                    "*.sts2-atomic-*.tmp",
                    SearchOption.AllDirectories
                ).Any(),
                "The interrupted replacement left a staging file behind."
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Task SynchronizationMakesFourDecisionsAsync()
    {
        var baseline = Manifest(('A', "settings.save"));
        var localChange = Manifest(('B', "settings.save"));
        var remoteChange = Manifest(('C', "settings.save"));
        var state = new SaveSyncService.SyncState
        {
            LastLocalManifest = baseline,
            LastRemoteManifest = baseline,
        };

        ExpectDecision(
            state,
            baseline,
            baseline,
            SaveSyncService.SyncOutcome.NoChange,
            SaveSyncService.SyncPrompt.None
        );
        ExpectDecision(
            state,
            localChange,
            baseline,
            SaveSyncService.SyncOutcome.Push,
            SaveSyncService.SyncPrompt.None
        );
        ExpectDecision(
            state,
            baseline,
            remoteChange,
            SaveSyncService.SyncOutcome.Pull,
            SaveSyncService.SyncPrompt.None
        );
        ExpectDecision(
            state,
            localChange,
            remoteChange,
            SaveSyncService.SyncOutcome.Conflict,
            SaveSyncService.SyncPrompt.ChooseSource
        );
        return Task.CompletedTask;
    }

    private static async Task InterruptedPullLeavesLocalSavesIntactAsync()
    {
        var root = NewTempDirectory();
        try
        {
            WriteSave(root, "a.save", "local-a");
            WriteSave(root, "b.save", "local-b");
            var remote = new FakeSaveRemote(
                ("a.save", Bytes("local-a")),
                ("b.save", Bytes("local-b"))
            );
            var service = new SaveSyncService(
                new AndroidLocalSaveStore(root),
                remote
            );
            Expect(
                (await ReconcileAsync(service)).Success,
                "The interrupted-Pull fixture did not establish a baseline."
            );
            remote.SetFile("a.save", Bytes("steam-a"));
            remote.SetFile("b.save", Bytes("steam-b"));
            remote.InterruptNextDownload("b.save");
            var savesBefore = SnapshotLocalFiles(root);
            var statePath = FindSyncStatePath(root);
            var stateBefore = File.ReadAllBytes(statePath);

            var result = await service.SyncAsync(
                SaveSyncService.SyncRequest.Pull,
                overwriteConfirmed: true,
                localWritesAreStopped: true,
                cancellationToken: CancellationToken.None
            );

            Expect(!result.Success, "A Pull interrupted during staging reported success.");
            Expect(
                FileSetsEqual(savesBefore, SnapshotLocalFiles(root)),
                "An interrupted Pull changed the complete local file set."
            );
            Expect(
                File.ReadAllBytes(statePath).SequenceEqual(stateBefore),
                "An interrupted Pull changed sync-state.json."
            );
            Expect(
                !Directory.EnumerateDirectories(
                    root,
                    "pull-staging",
                    SearchOption.AllDirectories
                ).Any(),
                "An interrupted Pull left staging residue."
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task FailedPushStaysDirtyAndRetryableAsync()
    {
        var root = NewTempDirectory();
        try
        {
            const string path = "profile1/saves/current_run.save";
            var original = Bytes("run-old");
            var changed = Bytes("run-new");
            WriteSave(root, path, original);
            var remote = new FakeSaveRemote((path, original));
            var service = new SaveSyncService(
                new AndroidLocalSaveStore(root),
                remote
            );
            var baseline = await ReconcileAsync(service);
            Expect(baseline.Success, "The failed-Push fixture did not establish a baseline.");

            remote.FailNextUpload();
            ISaveStore gameplayStore = new AndroidLocalSaveStore(
                root,
                service.NotifyLocalMutationCommitted
            );
            gameplayStore.WriteFile(path, changed);
            Expect(
                service.FlushAutomaticPush(TimeSpan.FromSeconds(5)),
                "The failed Push worker did not settle."
            );

            var failedState = ReadSyncState(root);
            Expect(
                failedState.LocalChangesWaitingToUpload
                    && failedState.InterruptedTransferMustBeRetried,
                "The failed Push cleared its durable dirty or retry marker."
            );

            var restarted = new SaveSyncService(
                new AndroidLocalSaveStore(root),
                remote
            );
            Expect(
                restarted.HasPendingLocalChanges(),
                "A new service instance did not load the durable dirty marker."
            );
            var retry = await ReconcileAsync(restarted);
            Expect(
                retry.Success && retry.Outcome == SaveSyncService.SyncOutcome.Push,
                "The durable failed Push was not retryable."
            );
            Expect(
                remote.ReadFile(path).SequenceEqual(changed)
                    && !restarted.HasPendingLocalChanges(),
                "The successful retry did not upload the save and clear dirty state."
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task PullCompletesBeforeSaveLoadingAsync()
    {
        var syncStarted = NewSignal();
        var releaseSync = NewSignal();
        var savesLoaded = false;
        var boundary = LauncherStartupFlow.RunPreloadSaveBoundaryAsync(
            async () =>
            {
                syncStarted.TrySetResult(true);
                await releaseSync.Task;
            },
            () => savesLoaded = true
        );

        await syncStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Expect(
            !savesLoaded && !boundary.IsCompleted,
            "Save loading started before Pull completed."
        );
        releaseSync.TrySetResult(true);
        await boundary.WaitAsync(TimeSpan.FromSeconds(2));
        Expect(savesLoaded, "Save loading did not run after Pull completed.");
    }

    private static async Task LocalSaveQueuesPushWithoutLauncherAsync()
    {
        var root = NewTempDirectory();
        try
        {
            const string path = "settings.save";
            var original = Bytes("settings-old");
            var changed = Bytes("settings-new");
            WriteSave(root, path, original);
            var remote = new FakeSaveRemote((path, original));
            var service = new SaveSyncService(
                new AndroidLocalSaveStore(root),
                remote
            );
            var baseline = await ReconcileAsync(service);
            Expect(baseline.Success, "The automatic-Push fixture did not establish a baseline.");

            remote.HoldNextUpload();
            ISaveStore gameplayStore = new AndroidLocalSaveStore(
                root,
                service.NotifyLocalMutationCommitted
            );
            try
            {
                await Task.Run(() => gameplayStore.WriteFile(path, changed))
                    .WaitAsync(TimeSpan.FromSeconds(2));
                await remote.UploadEntered.WaitAsync(TimeSpan.FromSeconds(2));
                Expect(
                    ReadSyncState(root).LocalChangesWaitingToUpload,
                    "The local save returned before its queued state was durable."
                );
            }
            finally
            {
                remote.ReleaseUpload();
            }

            Expect(
                service.FlushAutomaticPush(TimeSpan.FromSeconds(5)),
                "The queued gameplay Push did not finish."
            );
            Expect(
                remote.ReadFile(path).SequenceEqual(changed)
                    && !service.HasPendingLocalChanges(),
                "The gameplay save was not pushed and cleared in the game process."
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task ManualPushAndPullUseOneServiceAsync()
    {
        var root = NewTempDirectory();
        try
        {
            const string path = "profile1/saves/progress.save";
            var original = Bytes("progress-old");
            var localChange = Bytes("progress-device");
            var remoteChange = Bytes("progress-steam");
            WriteSave(root, path, original);
            var remote = new FakeSaveRemote((path, original));
            for (var index = 0; index < 200; index++)
            {
                var historyPath = $"profile1/saves/history/{index:D3}.run";
                var history = Bytes($"history-{index:D3}");
                WriteSave(root, historyPath, history);
                remote.SetFile(historyPath, history);
            }
            var service = new SaveSyncService(
                new AndroidLocalSaveStore(root),
                remote
            );
            Expect(
                (await ReconcileAsync(service)).Success,
                "The manual-sync fixture did not establish a baseline."
            );

            remote.ResetTransferCounts();
            WriteSave(root, path, localChange);
            var push = await service.SyncAsync(
                SaveSyncService.SyncRequest.Push,
                overwriteConfirmed: true,
                localWritesAreStopped: true,
                cancellationToken: CancellationToken.None
            );
            Expect(
                push.Success
                    && push.Outcome == SaveSyncService.SyncOutcome.Push
                    && remote.ReadFile(path).SequenceEqual(localChange)
                    && remote.UploadCount == 1,
                "Manual Push did not use the synchronization service."
            );

            remote.SetFile(path, remoteChange);
            remote.ResetTransferCounts();
            var pull = await service.SyncAsync(
                SaveSyncService.SyncRequest.Pull,
                overwriteConfirmed: true,
                localWritesAreStopped: true,
                cancellationToken: CancellationToken.None
            );
            Expect(
                pull.Success
                    && pull.Outcome == SaveSyncService.SyncOutcome.Pull
                    && File.ReadAllBytes(SavePath(root, path)).SequenceEqual(remoteChange)
                    && remote.DownloadCount == 1,
                "Manual Pull did not use the same synchronization service."
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Task ModSelectionAndDiscoverySurviveRestartAsync()
    {
        const ulong workshopId = 3747503308;
        const string selectionKey = "workshop:3747503308";
        const string manifestId = "ImportVanillaSaves";

        var root = NewTempDirectory();
        try
        {
            var fixtureRoot = Path.Combine(root, "workshop", workshopId.ToString());
            var manifestPath = Path.Combine(fixtureRoot, manifestId + ".json");
            var dllPath = Path.Combine(fixtureRoot, manifestId + ".dll");
            var pckPath = Path.Combine(fixtureRoot, manifestId + ".pck");
            const string validManifest =
                """
                {
                  "id": "ImportVanillaSaves",
                  "name": "Import Vanilla Saves",
                  "version": "v0.2.1",
                  "author": "offline fixture",
                  "description": "One representative desktop fixture.",
                  "affects_gameplay": false,
                  "has_dll": true,
                  "has_pck": true,
                  "dependencies": []
                }
                """;
            WriteFixtureFile(manifestPath, validManifest);
            WriteFixtureFile(dllPath, "representative-dll");
            WriteFixtureFile(pckPath, "representative-pck");

            var workshopManifest = SteamWorkshopSyncManifest.Empty(
                Path.Combine(root, "downloads"),
                Path.Combine(root, "workshop")
            );
            workshopManifest.Items.Add(new SteamWorkshopSyncManifestItem
            {
                PublishedFileId = workshopId,
                Title = "Import Vanilla Saves",
                SourceDirectory = fixtureRoot,
                StagedDirectory = fixtureRoot,
                Status = "staged",
                FileCount = 3,
                HasPck = true,
            });
            workshopManifest.SubscribedItemCount = 1;
            workshopManifest.TotalItemCount = 1;

            var selectionPath = Path.Combine(root, "state", "mod_selection.json");
            var selected = new LauncherModSelectionDocument
            {
                PlayMode = LauncherModSelectionState.ModdedModeName,
                EnabledMods = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    [selectionKey] = true,
                },
            };
            LauncherModSelectionState.Save(selectionPath, selected);

            // Mutate the source object so the next assertions can only pass by
            // reading the atomically persisted document through a fresh call.
            selected.PlayMode = LauncherModSelectionState.VanillaModeName;
            selected.EnabledMods.Clear();
            var reloaded = LauncherModSelectionState.Load(selectionPath);
            var discovered = LauncherModSelectionState.DiscoverKnownMods(
                reloaded,
                workshopManifest,
                Path.Combine(root, "manual")
            );
            var resolution = LauncherModLaunchPlan.Resolve(reloaded, discovered);
            Expect(
                reloaded.PlayMode == LauncherModSelectionState.ModdedModeName
                    && reloaded.EnabledMods.Count == 1
                    && reloaded.EnabledMods.TryGetValue(selectionKey, out var enabled)
                    && enabled,
                "A fresh load lost the persisted Modded mode or enabled importer."
            );
            Expect(
                discovered.Count == 1
                    && discovered[0].Key == selectionKey
                    && discovered[0].Enabled,
                "The representative Workshop fixture did not resolve to one enabled discovery."
            );
            Expect(
                resolution.Success
                    && resolution.Plan.Mode == LauncherModPlayMode.Modded
                    && resolution.Plan.SaveNamespace == LauncherModSaveNamespace.Modded
                    && resolution.Plan.EnabledMods.Length == 1
                    && resolution.Plan.Dependencies.IsEmpty
                    && resolution.Plan.Roots.Length == 1,
                $"The representative launch plan failed: {resolution.Error?.Message}"
            );
            var plannedMod = resolution.Plan.EnabledMods[0];
            Expect(
                plannedMod.ManifestId == manifestId
                    && plannedMod.SelectionKey == selectionKey
                    && string.Equals(
                        plannedMod.RootPath,
                        Path.GetFullPath(fixtureRoot),
                        StringComparison.OrdinalIgnoreCase
                    ),
                "The representative launch plan resolved the wrong identity or root."
            );
            ExpectResolvedPayloads(plannedMod, manifestPath, dllPath, pckPath);

            var afterRestart = LauncherModSelectionState.Load(selectionPath);
            var restarted = LauncherModLaunchPlan.Resolve(
                afterRestart,
                LauncherModSelectionState.DiscoverKnownMods(
                    afterRestart,
                    workshopManifest,
                    Path.Combine(root, "manual")
                )
            );
            Expect(
                restarted.Success
                    && restarted.Plan.Fingerprint == resolution.Plan.Fingerprint
                    && restarted.Plan.Mode == resolution.Plan.Mode
                    && restarted.Plan.EnabledMods.Length == 1
                    && restarted.Plan.EnabledMods[0].ManifestId == manifestId
                    && string.Equals(
                        restarted.Plan.EnabledMods[0].RootPath,
                        plannedMod.RootPath,
                        StringComparison.OrdinalIgnoreCase
                    ),
                "The exact selection was not visible after the simulated restart boundary."
            );

            ExpectTruthfulModsPresentation(afterRestart, discovered, root);

            var disabled = new LauncherModSelectionDocument
            {
                PlayMode = LauncherModSelectionState.ModdedModeName,
                EnabledMods = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    [selectionKey] = false,
                },
            };
            var disabledResolution = LauncherModLaunchPlan.Resolve(disabled, discovered);
            Expect(
                !disabledResolution.Success
                    && disabledResolution.Plan == null
                    && disabledResolution.Error?.Code
                        == LauncherModDiscoveryErrorCode.SelectionRequired,
                "A disabled selection produced a loadable mod plan."
            );

            var vanilla = new LauncherModSelectionDocument
            {
                PlayMode = LauncherModSelectionState.VanillaModeName,
                EnabledMods = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    [selectionKey] = true,
                },
            };
            var vanillaResolution = LauncherModLaunchPlan.Resolve(vanilla, discovered);
            Expect(
                vanillaResolution.Success
                    && vanillaResolution.Plan.Mode == LauncherModPlayMode.Vanilla
                    && vanillaResolution.Plan.SaveNamespace
                        == LauncherModSaveNamespace.Vanilla
                    && vanillaResolution.Plan.EnabledMods.IsEmpty
                    && vanillaResolution.Plan.Roots.IsEmpty,
                "Vanilla mode produced a loadable mod plan."
            );

            WriteFixtureFile(manifestPath, "{ malformed");
            var badManifest = LauncherModLaunchPlan.Resolve(
                afterRestart,
                LauncherModSelectionState.DiscoverKnownMods(
                    afterRestart,
                    workshopManifest,
                    Path.Combine(root, "manual")
                )
            );
            ExpectDiscoveryErrorCode(
                badManifest,
                LauncherModDiscoveryErrorCode.ManifestInvalid,
                selectionKey,
                "A malformed representative manifest did not fail discovery."
            );
            ExpectPlanFailureMarker(
                Path.Combine(root, "results", "bad-manifest"),
                afterRestart,
                badManifest.Error,
                expectedSelectedMods: 1
            );

            WriteFixtureFile(manifestPath, validManifest);
            File.Delete(pckPath);
            var badPayload = LauncherModLaunchPlan.Resolve(
                afterRestart,
                LauncherModSelectionState.DiscoverKnownMods(
                    afterRestart,
                    workshopManifest,
                    Path.Combine(root, "manual")
                )
            );
            ExpectDiscoveryErrorCode(
                badPayload,
                LauncherModDiscoveryErrorCode.PayloadMissing,
                selectionKey,
                "A missing declared payload did not fail discovery."
            );
            ExpectPlanFailureMarker(
                Path.Combine(root, "results", "bad-payload"),
                afterRestart,
                badPayload.Error,
                expectedSelectedMods: 1
            );

            return Task.CompletedTask;
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void ExpectTruthfulModsPresentation(
        LauncherModSelectionDocument selection,
        IReadOnlyList<LauncherKnownMod> discoveredMods,
        string fixtureRoot
    )
    {
        var dataDir = Path.Combine(fixtureRoot, "results", "ui-states");
        WithPreviewDataDirectory(dataDir, () =>
        {
            LauncherModsPresentation WriteAndRead(
                LauncherModSelectionDocument markerSelection,
                string result,
                int loaded
            )
            {
                LauncherModLaunchResultStore.Write(
                    LauncherModSelectionState.ModdedModeName,
                    markerSelection,
                    discovered: 1,
                    loaded,
                    new[]
                    {
                        new LauncherModLaunchResult(
                            "ImportVanillaSaves",
                            result,
                            result switch
                            {
                                "Active" => "Initialization and activation verified.",
                                "Partial" => "Activation was only partially verified.",
                                _ => "Activation failed.",
                            }
                        ),
                    }
                );
                return LauncherModsPresentationState.Build(
                    selection,
                    discoveredMods,
                    AppPaths.AppPrivateLastModLaunchPath
                );
            }

            var current = WriteAndRead(selection, "Active", loaded: 1);
            var currentImporter = current.Mods.Single(mod =>
                mod.Key == "workshop:3747503308"
            );
            Expect(
                current.Mode == LauncherModPlayMode.Modded
                    && current.PrimaryPlayLabel == "Play Modded \u00B7 1 enabled"
                    && currentImporter.Enabled
                    && currentImporter.LastLaunchState
                        == LauncherModLastLaunchState.LoadedLastLaunch,
                "A current persisted result was not surfaced as Loaded last launch."
            );

            var partial = WriteAndRead(selection, "Partial", loaded: 1);
            Expect(
                partial.Mods.Single().LastLaunchState
                    == LauncherModLastLaunchState.Partial,
                "A persisted Partial result was not surfaced as Partial."
            );

            var failed = WriteAndRead(selection, "Failed", loaded: 0);
            Expect(
                failed.Mods.Single().LastLaunchState
                    == LauncherModLastLaunchState.Failed,
                "A persisted Failed result was not surfaced as Failed."
            );

            var differentSelection = new LauncherModSelectionDocument
            {
                PlayMode = LauncherModSelectionState.ModdedModeName,
                EnabledMods = new Dictionary<string, bool>(
                    selection.EnabledMods,
                    StringComparer.OrdinalIgnoreCase
                )
                {
                    ["manual:different-selection"] = true,
                },
            };
            var stale = WriteAndRead(differentSelection, "Active", loaded: 1);
            var staleImporter = stale.Mods.Single();
            Expect(
                stale.HasStaleLastLaunchResult
                    && staleImporter.IsLastLaunchStale
                    && staleImporter.LastLaunchState
                        == LauncherModLastLaunchState.NotTestedYet,
                "A different selection fingerprint was not surfaced as stale and Not tested yet."
            );

            File.Delete(AppPaths.AppPrivateLastModLaunchPath);
            var missing = LauncherModsPresentationState.Build(
                selection,
                discoveredMods,
                AppPaths.AppPrivateLastModLaunchPath
            );
            Expect(
                !missing.HasStaleLastLaunchResult
                    && missing.Mods.Single().LastLaunchState
                        == LauncherModLastLaunchState.NotTestedYet,
                "A missing runtime result did not surface as Not tested yet."
            );
        });
    }

    private static void ExpectPlanFailureMarker(
        string dataDir,
        LauncherModSelectionDocument selection,
        LauncherModDiscoveryError error,
        int expectedSelectedMods
    )
    {
        WithPreviewDataDirectory(dataDir, () =>
        {
            var markerPath = AppPaths.AppPrivateLastModLaunchPath;
            Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
            File.WriteAllText(markerPath, "{\"staleActiveResult\":true}");

            LauncherModLaunchResultStore.WritePlanFailure(selection, error);
            var marker = LauncherModsPresentationState.ReadMarker(markerPath);
            Expect(
                marker != null
                    && marker.LaunchMode == LauncherModSelectionState.ModdedModeName
                    && marker.SelectionFingerprint
                        == LauncherModSelectionState.SelectionFingerprint(selection)
                    && marker.Discovered == 0
                    && marker.Loaded == 0
                    && marker.Active == 0
                    && marker.Partial == 0
                    && marker.Failed == expectedSelectedMods
                    && marker.Mods.Length == expectedSelectedMods
                    && marker.Mods.All(mod => mod.Result == "Failed"),
                $"A {error.Code} plan failure did not overwrite the stale marker with explicit Failed results."
            );
            Expect(
                !File.Exists(markerPath + ".tmp"),
                "The atomic mod-launch result write left its temporary file behind."
            );
        });
    }

    private static void WithPreviewDataDirectory(string dataDir, Action action)
    {
        const string previewModeVariable = "STS2_LAUNCHER_PREVIEW";
        var previousPreviewMode = Environment.GetEnvironmentVariable(previewModeVariable);
        var previousDataDir = Environment.GetEnvironmentVariable(
            AppPaths.LauncherPreviewDataDirEnvironmentVariable
        );
        try
        {
            Environment.SetEnvironmentVariable(previewModeVariable, "1");
            Environment.SetEnvironmentVariable(
                AppPaths.LauncherPreviewDataDirEnvironmentVariable,
                Path.GetFullPath(dataDir)
            );
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(previewModeVariable, previousPreviewMode);
            Environment.SetEnvironmentVariable(
                AppPaths.LauncherPreviewDataDirEnvironmentVariable,
                previousDataDir
            );
        }
    }

    private static void ExpectResolvedPayloads(
        LauncherResolvedMod mod,
        string manifestPath,
        string dllPath,
        string pckPath
    )
    {
        Expect(
            string.Equals(mod.ManifestPath, Path.GetFullPath(manifestPath), StringComparison.OrdinalIgnoreCase)
                && string.Equals(mod.DllPath, Path.GetFullPath(dllPath), StringComparison.OrdinalIgnoreCase)
                && string.Equals(mod.PckPath, Path.GetFullPath(pckPath), StringComparison.OrdinalIgnoreCase),
            $"The launch plan resolved the wrong manifest or payload for {mod.ManifestId}."
        );
    }

    private static void ExpectDiscoveryErrorCode(
        LauncherModLaunchPlanResolution resolution,
        LauncherModDiscoveryErrorCode code,
        string selectionKey,
        string failureMessage
    )
    {
        Expect(
            !resolution.Success
                && resolution.Plan == null
                && resolution.Error != null
                && resolution.Error.Code == code
                && resolution.Error.SelectionKey == selectionKey
                && !string.IsNullOrWhiteSpace(resolution.Error.Message),
            $"{failureMessage} Got {resolution.Error}."
        );
    }

    private static void WriteFixtureFile(string path, string contents)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
        File.WriteAllText(path, contents);
    }

    private static Task<SaveSyncService.SyncResult> ReconcileAsync(
        SaveSyncService service
    )
        => service.SyncAsync(
            SaveSyncService.SyncRequest.Reconcile,
            overwriteConfirmed: false,
            localWritesAreStopped: true,
            cancellationToken: CancellationToken.None
        );

    private static SaveSyncService.SaveManifest Manifest(
        params (char Hash, string Path)[] files
    )
        => SaveSyncService.CreateManifest(
            files.Select(file => new SaveSyncService.ManifestEntry
            {
                Path = file.Path,
                Size = 1,
                ContentHash = new string(file.Hash, 40),
            })
        );

    private static void ExpectDecision(
        SaveSyncService.SyncState state,
        SaveSyncService.SaveManifest local,
        SaveSyncService.SaveManifest remote,
        SaveSyncService.SyncOutcome outcome,
        SaveSyncService.SyncPrompt prompt
    )
    {
        var actual = SaveSyncService.Decide(
            state,
            local,
            remote,
            SaveSyncService.SyncRequest.Reconcile,
            overwriteConfirmed: false
        );
        Expect(
            actual.Outcome == outcome && actual.Prompt == prompt,
            $"Expected {outcome}/{prompt}, got {actual.Outcome}/{actual.Prompt}."
        );
    }

    private static void ExpectWriteRejected(
        ISaveStore store,
        string requestedPath,
        string forbiddenPath
    )
    {
        var rejected = false;
        try
        {
            store.WriteFile(requestedPath, "escape");
        }
        catch (IOException)
        {
            rejected = true;
        }

        Expect(rejected, $"Unsafe save path was accepted: {requestedPath}");
        Expect(
            !File.Exists(forbiddenPath),
            $"Unsafe save path escaped the local root: {requestedPath}"
        );
    }

    private static SaveSyncService.SyncState ReadSyncState(string root)
    {
        var statePath = FindSyncStatePath(root);
        return JsonSerializer.Deserialize<SaveSyncService.SyncState>(
                File.ReadAllText(statePath)
            )
            ?? throw new InvalidDataException("sync-state.json was empty.");
    }

    private static string FindSyncStatePath(string root)
        => Directory.EnumerateFiles(
            root,
            SaveSyncService.StateFileName,
            SearchOption.AllDirectories
        ).Single();

    private static Dictionary<string, byte[]> SnapshotLocalFiles(string root)
        => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path =>
                !string.Equals(
                    Path.GetFileName(path),
                    SaveSyncService.StateFileName,
                    StringComparison.Ordinal
                )
            )
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace('\\', '/'),
                File.ReadAllBytes,
                StringComparer.Ordinal
            );

    private static bool FileSetsEqual(
        IReadOnlyDictionary<string, byte[]> expected,
        IReadOnlyDictionary<string, byte[]> actual
    )
        => expected.Count == actual.Count
            && expected.All(pair =>
                actual.TryGetValue(pair.Key, out var bytes)
                && pair.Value.SequenceEqual(bytes)
            );

    private static void WriteSave(string root, string path, string content)
        => WriteSave(root, path, Bytes(content));

    private static void WriteSave(string root, string path, byte[] bytes)
    {
        var destination = SavePath(root, path);
        var parent = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
        File.WriteAllBytes(destination, bytes);
    }

    private static string SavePath(string root, string path)
        => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));

    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

    private static string NewTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"sts2-save-probe-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(path);
        return path;
    }

    private static TaskCompletionSource<bool> NewSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task RunAsync(string name, Func<Task> scenario)
    {
        try
        {
            await scenario();
            _passed++;
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"FAIL: {name}: {ex.Message}", ex);
        }
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class FakeSaveRemote : ISaveRemote
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
        private string? _interruptedDownloadPath;
        private bool _failNextUpload;
        private bool _holdNextUpload;
        private int _downloadCount;
        private int _uploadCount;
        private TaskCompletionSource<bool> _uploadEntered = NewSignal();
        private TaskCompletionSource<bool> _releaseUpload = NewSignal();

        internal FakeSaveRemote(params (string Path, byte[] Bytes)[] files)
        {
            foreach (var file in files)
                SetFile(file.Path, file.Bytes);
        }

        internal Task UploadEntered
        {
            get
            {
                lock (_gate)
                    return _uploadEntered.Task;
            }
        }

        internal int DownloadCount
        {
            get
            {
                lock (_gate)
                    return _downloadCount;
            }
        }

        internal int UploadCount
        {
            get
            {
                lock (_gate)
                    return _uploadCount;
            }
        }

        internal void SetFile(string path, byte[] bytes)
        {
            lock (_gate)
                _files[Canonical(path)] = bytes.ToArray();
        }

        internal byte[] ReadFile(string path)
        {
            lock (_gate)
                return _files[Canonical(path)].ToArray();
        }

        internal void FailNextUpload()
        {
            lock (_gate)
                _failNextUpload = true;
        }

        internal void InterruptNextDownload(string path)
        {
            lock (_gate)
                _interruptedDownloadPath = Canonical(path);
        }

        internal void HoldNextUpload()
        {
            lock (_gate)
            {
                _holdNextUpload = true;
                _uploadEntered = NewSignal();
                _releaseUpload = NewSignal();
            }
        }

        internal void ReleaseUpload()
        {
            lock (_gate)
                _releaseUpload.TrySetResult(true);
        }

        internal void ResetTransferCounts()
        {
            lock (_gate)
            {
                _downloadCount = 0;
                _uploadCount = 0;
            }
        }

        public Task<IReadOnlyList<SteamCloudTransport.RemoteFile>> EnumerateAsync(
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                IReadOnlyList<SteamCloudTransport.RemoteFile> files = _files
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new SteamCloudTransport.RemoteFile(
                        pair.Key,
                        pair.Value.LongLength,
                        DateTimeOffset.UnixEpoch,
                        Convert.ToHexString(ManagedSha1.Hash(pair.Value))
                    ))
                    .ToArray();
                return Task.FromResult(files);
            }
        }

        public Task<byte[]> DownloadBytesAsync(
            SteamCloudTransport.RemoteFile file,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                _downloadCount++;
                var path = Canonical(file.Path);
                if (
                    string.Equals(
                        _interruptedDownloadPath,
                        path,
                        StringComparison.Ordinal
                    )
                )
                {
                    _interruptedDownloadPath = null;
                    throw new IOException("Fake staged-download interruption");
                }
                return Task.FromResult(_files[path].ToArray());
            }
        }

        public async Task<bool> UploadAsync(
            SteamCloudTransport.UploadFile file,
            CancellationToken cancellationToken
        )
        {
            Task? heldUpload = null;
            lock (_gate)
            {
                _uploadCount++;
                if (_holdNextUpload)
                {
                    _holdNextUpload = false;
                    _uploadEntered.TrySetResult(true);
                    heldUpload = _releaseUpload.Task;
                }
            }

            if (heldUpload != null)
                await heldUpload.WaitAsync(cancellationToken);

            lock (_gate)
            {
                if (_failNextUpload)
                {
                    _failNextUpload = false;
                    throw new IOException("Fake upload failure");
                }

                var path = Canonical(file.Path);
                var changed = !_files.TryGetValue(path, out var existing)
                    || !existing.SequenceEqual(file.Bytes);
                _files[path] = file.Bytes.ToArray();
                return changed;
            }
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
                _files.Remove(Canonical(path));
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        private static string Canonical(string path)
            => SteamCloudTransport.CanonicalizePath(path);
    }
}
