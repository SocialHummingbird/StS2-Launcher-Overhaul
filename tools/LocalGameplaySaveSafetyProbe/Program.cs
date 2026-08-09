#nullable enable

using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace LocalGameplaySaveSafetyProbe;

internal static class Program
{
    private static int _passed;

    private static async Task Main()
    {
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

        Console.WriteLine($"Focused local-save and synchronization probe passed {_passed}/8 scenarios.");
        Console.WriteLine(
            "Scope: FakeSaveRemote validates deterministic synchronization behavior only; it does not prove Steam Cloud or Android transport."
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
            var service = new SaveSyncService(
                new AndroidLocalSaveStore(root),
                remote
            );
            Expect(
                (await ReconcileAsync(service)).Success,
                "The manual-sync fixture did not establish a baseline."
            );

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
                    && remote.ReadFile(path).SequenceEqual(localChange),
                "Manual Push did not use the synchronization service."
            );

            remote.SetFile(path, remoteChange);
            var pull = await service.SyncAsync(
                SaveSyncService.SyncRequest.Pull,
                overwriteConfirmed: true,
                localWritesAreStopped: true,
                cancellationToken: CancellationToken.None
            );
            Expect(
                pull.Success
                    && pull.Outcome == SaveSyncService.SyncOutcome.Pull
                    && File.ReadAllBytes(SavePath(root, path)).SequenceEqual(remoteChange),
                "Manual Pull did not use the same synchronization service."
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
