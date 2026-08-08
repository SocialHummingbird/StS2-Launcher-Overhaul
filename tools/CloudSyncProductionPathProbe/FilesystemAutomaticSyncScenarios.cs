#nullable enable

using System.Text;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal static class FilesystemAutomaticSyncScenarios
{
    private const ulong SteamId64 = InMemoryCloudSaveStore.DefaultSteamId64;
    private const string PublicBeta = "public-beta";
    private const string ModFingerprintA = "sha256:filesystem-mods-a";
    private const string ModFingerprintB = "sha256:filesystem-mods-b";
    private const string VanillaProgressPath = "profile1/saves/progress.save";
    private const string ModdedProgressPath =
        "modded/profile1/saves/progress.save";
    private const string CurrentRunPath = "profile1/saves/current_run.save";
    private const string HistoryA = "profile1/saves/history/20260806.run";
    private const string HistoryB = "profile1/saves/history/20260805.run";
    private const string SettingsPath = "settings.save";
    private static readonly byte[] A = Bytes("fixture-a\r\n\0");
    private static readonly byte[] B = Bytes("fixture-b\r\n\0");
    private static readonly byte[] C = Bytes("fixture-c\r\n\0");
    private static int _passed;

    internal static async Task<int> RunAllAsync()
    {
        _passed = 0;
        CloudSyncCoordinator.SetLocalBackupEnabled(false);

        await RunAsync(
            "filesystem exact-context uploads isolate vanilla/modded and public/beta",
            ExactContextUploadMatrixAsync
        ).ConfigureAwait(false);
        await RunAsync(
            "filesystem public/beta switch stops before cross-branch activation",
            PublicBetaSwitchStopsBeforeMutationAsync
        ).ConfigureAwait(false);
        await RunAsync(
            "filesystem changed mod set blocks before mutation",
            ChangedModSetBlocksAsync
        ).ConfigureAwait(false);
        await RunAsync(
            "filesystem remote change downloads safely before Play",
            RemoteChangeDownloadsBeforePlayAsync
        ).ConfigureAwait(false);
        await RunAsync(
            "filesystem divergence preserves local and Steam bytes",
            DivergencePreservesBothSidesAsync
        ).ConfigureAwait(false);
        await RunAsync(
            "filesystem offline upload remains pending and retries",
            OfflineUploadRetriesAsync
        ).ConfigureAwait(false);
        await RunAsync(
            "filesystem automatic commit failure never reports synchronized",
            AutomaticCommitFailureStaysPendingAsync
        ).ConfigureAwait(false);
        await RunAsync(
            "filesystem automatic read-back mismatch never reports synchronized",
            AutomaticReadBackFailureStaysPendingAsync
        ).ConfigureAwait(false);
        await RunAsync(
            "filesystem Restore and Undo round-trip exact bytes",
            RestoreAndUndoRoundTripExactBytesAsync
        ).ConfigureAwait(false);

        Console.WriteLine(
            $"Filesystem automatic-sync production-path probe passed {_passed}/9 scenarios."
        );
        return _passed;
    }

    private static async Task ExactContextUploadMatrixAsync()
    {
        var cases = new[]
        {
            new ContextCase(
                "vanilla-public",
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                ""
            ),
            new ContextCase(
                "vanilla-public-beta",
                SaveNamespace.Vanilla,
                PublicBeta,
                ""
            ),
            new ContextCase(
                "modded-public",
                SaveNamespace.Modded,
                SteamGameBranch.Public,
                ModFingerprintA
            ),
            new ContextCase(
                "modded-public-beta",
                SaveNamespace.Modded,
                PublicBeta,
                ModFingerprintA
            ),
        };
        var baselinePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in cases)
        {
            await WithFilesystemAsync(item.Name, async (root, local) =>
            {
                var cloud = Store($"filesystem-{item.Name}-steam");
                var context = Context(item);
                var selectedPath = item.Namespace == SaveNamespace.Vanilla
                    ? VanillaProgressPath
                    : ModdedProgressPath;
                var unselectedPath = item.Namespace == SaveNamespace.Vanilla
                    ? ModdedProgressPath
                    : VanillaProgressPath;
                var otherContext = item.Namespace == SaveNamespace.Vanilla
                    ? SaveContext.Create(
                        SteamId64,
                        SaveNamespace.Modded,
                        item.Runtime,
                        ModFingerprintA
                    )
                    : SaveContext.Create(
                        SteamId64,
                        SaveNamespace.Vanilla,
                        item.Runtime,
                        ""
                    );
                var initial = Bytes($"{item.Name}-initial\r\n\0");
                var afterGame = Bytes($"{item.Name}-after-game\r\n\0");
                var localUnselected = Bytes($"{item.Name}-local-unselected");
                var remoteUnselected = Bytes($"{item.Name}-remote-unselected");
                var remoteSettings = Bytes($"{item.Name}-remote-settings");

                WriteLocal(local, selectedPath, initial);
                WriteLocal(local, unselectedPath, localUnselected);
                WriteLocal(local, SettingsPath, Bytes("device-local-only"));
                cloud.SeedBytes(selectedPath, Bytes("remote-old"));
                cloud.SeedBytes(unselectedPath, remoteUnselected);
                cloud.SeedBytes(SettingsPath, remoteSettings);
                SeedMarker(cloud, context);
                SeedMarker(cloud, otherContext);

                var established = await ReconcileAsync(
                    local,
                    cloud,
                    context,
                    AutomaticSyncSourceChoice.Local
                ).ConfigureAwait(false);
                ExpectOutcome(established, AutomaticSyncOutcome.Synchronized);
                ExpectRemoteBytes(cloud, selectedPath, initial);
                ExpectRemoteBytes(cloud, unselectedPath, remoteUnselected);
                ExpectRemoteBytes(cloud, SettingsPath, remoteSettings);
                ExpectRemoteText(
                    cloud,
                    context.MarkerPath,
                    context.SerializeMarker()
                );
                ExpectRemoteText(
                    cloud,
                    otherContext.MarkerPath,
                    otherContext.SerializeMarker()
                );
                Expect(cloud.WriteCountFor(unselectedPath) == 0,
                    $"{item.Name} wrote the unselected namespace.");
                Expect(cloud.WriteCountFor(SettingsPath) == 0,
                    $"{item.Name} wrote device settings to Steam.");

                var baselinePath = CloudSyncCoordinator
                    .AutomaticSyncBaselinePath(context);
                Expect(LocalExists(root, baselinePath),
                    $"{item.Name} did not persist its context baseline.");
                Expect(baselinePaths.Add(baselinePath),
                    $"{item.Name} collided with another context baseline.");

                var prepared = await BeginAsync(local, cloud, context)
                    .ConfigureAwait(false);
                ExpectOutcome(
                    prepared,
                    AutomaticSyncOutcome.GameSessionPrepared
                );
                Expect(prepared.CanStartGame,
                    $"{item.Name} did not permit the prepared game session.");
                WriteLocal(local, selectedPath, afterGame);
                var operationCount = cloud.Operations.Count;

                var recovered = await RecoverAsync(local, cloud)
                    .ConfigureAwait(false);
                ExpectOutcome(recovered, AutomaticSyncOutcome.Synchronized);
                ExpectRemoteBytes(cloud, selectedPath, afterGame);
                ExpectRemoteBytes(cloud, unselectedPath, remoteUnselected);
                ExpectRemoteBytes(cloud, SettingsPath, remoteSettings);
                ExpectLocalBytes(root, selectedPath, afterGame);
                ExpectLocalBytes(root, unselectedPath, localUnselected);
                Expect(!LocalExists(
                    root,
                    CloudSyncCoordinator.AutomaticSyncPendingPath
                ), $"{item.Name} left pending sync state after success.");
                ExpectVerifiedAfterWrite(
                    cloud.Operations.Skip(operationCount),
                    selectedPath,
                    item.Name
                );
                Expect(cloud.WriteCountFor(unselectedPath) == 0,
                    $"{item.Name} post-game upload crossed namespaces.");
                Expect(cloud.WriteCountFor(SettingsPath) == 0,
                    $"{item.Name} post-game upload transferred settings.");
            }).ConfigureAwait(false);
        }

        Expect(baselinePaths.Count == cases.Length,
            "Filesystem context baselines were not unique.");
    }

    private static Task PublicBetaSwitchStopsBeforeMutationAsync()
        => WithFilesystemAsync("public-beta-switch", async (root, local) =>
        {
            var cloud = Store("filesystem-public-beta-switch-steam");
            var publicContext = VanillaContext();
            var betaContext = SaveContext.Create(
                SteamId64,
                SaveNamespace.Vanilla,
                PublicBeta,
                ""
            );
            WriteLocal(local, VanillaProgressPath, A);
            cloud.SeedBytes(VanillaProgressPath, A);
            SeedMarker(cloud, publicContext);
            await EstablishAsync(local, cloud, publicContext)
                .ConfigureAwait(false);
            var localBefore = SnapshotAllFiles(root);
            var remoteWrites = cloud.WriteCount;
            var remoteDeletes = cloud.DeleteCount;

            var blocked = await ReconcileAsync(
                local,
                cloud,
                betaContext,
                AutomaticSyncSourceChoice.Local
            ).ConfigureAwait(false);

            ExpectOutcome(blocked, AutomaticSyncOutcome.Conflict);
            Expect(blocked.HasConflict && !blocked.CanStartGame,
                "A public-to-beta context switch silently activated saves.");
            ExpectAllFiles(root, localBefore);
            ExpectRemoteBytes(cloud, VanillaProgressPath, A);
            ExpectRemoteText(
                cloud,
                publicContext.MarkerPath,
                publicContext.SerializeMarker()
            );
            Expect(
                cloud.WriteCount == remoteWrites
                    && cloud.DeleteCount == remoteDeletes,
                "A public-to-beta mismatch mutated fake Steam."
            );
            Expect(!LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncBaselinePath(betaContext)
            ), "A blocked beta context wrote a baseline.");
            Expect(!LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "A blocked beta context wrote pending sync state.");
        });

    private static Task ChangedModSetBlocksAsync()
        => WithFilesystemAsync("changed-mod-set", async (root, local) =>
        {
            var cloud = Store("filesystem-changed-mod-set-steam");
            var contextA = SaveContext.Create(
                SteamId64,
                SaveNamespace.Modded,
                PublicBeta,
                ModFingerprintA
            );
            var contextB = SaveContext.Create(
                SteamId64,
                SaveNamespace.Modded,
                PublicBeta,
                ModFingerprintB
            );
            WriteLocal(local, ModdedProgressPath, B);
            cloud.SeedBytes(ModdedProgressPath, A);
            SeedMarker(cloud, contextA);
            var localBefore = SnapshotAllFiles(root);
            var remoteWrites = cloud.WriteCount;
            var remoteDeletes = cloud.DeleteCount;

            var blocked = await ReconcileAsync(
                local,
                cloud,
                contextB,
                AutomaticSyncSourceChoice.Local
            ).ConfigureAwait(false);

            ExpectOutcome(blocked, AutomaticSyncOutcome.Conflict);
            Expect(blocked.HasConflict && !blocked.CanStartGame,
                "A changed mod set was allowed to start the game.");
            ExpectAllFiles(root, localBefore);
            ExpectRemoteBytes(cloud, ModdedProgressPath, A);
            ExpectRemoteText(
                cloud,
                contextA.MarkerPath,
                contextA.SerializeMarker()
            );
            Expect(
                cloud.WriteCount == remoteWrites
                    && cloud.DeleteCount == remoteDeletes,
                "Changed-mod-set reconciliation mutated Steam."
            );
            Expect(!LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Changed-mod-set reconciliation persisted a pending transfer.");
        });

    private static Task RemoteChangeDownloadsBeforePlayAsync()
        => WithFilesystemAsync("remote-before-play", async (root, local) =>
        {
            var cloud = Store("filesystem-remote-before-play-steam");
            var context = VanillaContext();
            WriteLocal(local, VanillaProgressPath, A);
            cloud.SeedBytes(VanillaProgressPath, A);
            SeedMarker(cloud, context);
            await EstablishAsync(local, cloud, context).ConfigureAwait(false);
            var backupsBefore = TransferBackupFiles(root, VanillaProgressPath);

            cloud.SeedBytes(VanillaProgressPath, B);
            var prepared = await BeginAsync(local, cloud, context)
                .ConfigureAwait(false);

            ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
            Expect(prepared.CanStartGame,
                "Remote pre-Play download did not produce a prepared session.");
            ExpectLocalBytes(root, VanillaProgressPath, B);
            ExpectRemoteBytes(cloud, VanillaProgressPath, B);
            Expect(LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Pre-Play reconciliation did not persist game-running state.");
            var newBackups = TransferBackupFiles(root, VanillaProgressPath)
                .Except(backupsBefore, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Expect(newBackups.Any(path => File.ReadAllBytes(path)
                    .AsSpan().SequenceEqual(A)),
                "Remote pre-Play overwrite did not back up the previous local bytes.");

            var settled = await RecoverAsync(local, cloud).ConfigureAwait(false);
            ExpectOutcome(settled, AutomaticSyncOutcome.Synchronized);
            Expect(!LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Unchanged game session did not settle after safe download.");
        });

    private static Task DivergencePreservesBothSidesAsync()
        => WithFilesystemAsync("divergence", async (root, local) =>
        {
            var cloud = Store("filesystem-divergence-steam");
            var context = VanillaContext();
            WriteLocal(local, VanillaProgressPath, A);
            cloud.SeedBytes(VanillaProgressPath, A);
            SeedMarker(cloud, context);
            await EstablishAsync(local, cloud, context).ConfigureAwait(false);

            WriteLocal(local, VanillaProgressPath, B);
            cloud.SeedBytes(VanillaProgressPath, C);
            var localBefore = SnapshotLiveFiles(root);
            var remoteWrites = cloud.WriteCount;
            var remoteDeletes = cloud.DeleteCount;
            var blocked = await BeginAsync(local, cloud, context)
                .ConfigureAwait(false);

            ExpectOutcome(blocked, AutomaticSyncOutcome.Conflict);
            Expect(blocked.HasConflict && !blocked.CanStartGame,
                "Divergent saves allowed Play.");
            ExpectLiveFiles(root, localBefore);
            ExpectLocalBytes(root, VanillaProgressPath, B);
            ExpectRemoteBytes(cloud, VanillaProgressPath, C);
            Expect(
                cloud.WriteCount == remoteWrites
                    && cloud.DeleteCount == remoteDeletes,
                "Divergence mutated Steam instead of stopping."
            );
            Expect(!LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Divergence persisted an overwrite intent.");
        });

    private static Task OfflineUploadRetriesAsync()
        => WithFilesystemAsync("offline-retry", async (root, local) =>
        {
            var cloud = Store("filesystem-offline-retry-steam");
            var context = VanillaContext();
            WriteLocal(local, VanillaProgressPath, A);
            cloud.SeedBytes(VanillaProgressPath, A);
            SeedMarker(cloud, context);
            await EstablishAsync(local, cloud, context).ConfigureAwait(false);
            var prepared = await BeginAsync(local, cloud, context)
                .ConfigureAwait(false);
            ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
            WriteLocal(local, VanillaProgressPath, B);
            cloud.AuthenticationFailure = new IOException(
                "Injected deterministic offline Steam connection."
            );
            var remoteWrites = cloud.WriteCount;

            var offline = await AttemptAsync(() => RecoverAsync(local, cloud))
                .ConfigureAwait(false);
            ExpectNotSynchronized(offline, "offline automatic upload");
            Expect(LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Offline work did not remain pending.");
            ExpectLocalBytes(root, VanillaProgressPath, B);
            ExpectRemoteBytes(cloud, VanillaProgressPath, A);
            Expect(cloud.WriteCount == remoteWrites,
                "Offline automatic sync wrote Steam data.");

            cloud.AuthenticationFailure = null;
            var operationCount = cloud.Operations.Count;
            var retry = await RecoverAsync(local, cloud).ConfigureAwait(false);
            ExpectOutcome(retry, AutomaticSyncOutcome.Synchronized);
            ExpectRemoteBytes(cloud, VanillaProgressPath, B);
            ExpectLocalBytes(root, VanillaProgressPath, B);
            Expect(!LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Successful online retry left work pending.");
            ExpectVerifiedAfterWrite(
                cloud.Operations.Skip(operationCount),
                VanillaProgressPath,
                "online retry"
            );
        });

    private static Task AutomaticCommitFailureStaysPendingAsync()
        => WithFilesystemAsync("commit-failure", async (root, local) =>
        {
            var cloud = Store("filesystem-commit-failure-steam");
            var context = VanillaContext();
            WriteLocal(local, VanillaProgressPath, A);
            cloud.SeedBytes(VanillaProgressPath, A);
            SeedMarker(cloud, context);
            await EstablishAsync(local, cloud, context).ConfigureAwait(false);
            await ExpectPreparedAsync(local, cloud, context).ConfigureAwait(false);
            WriteLocal(local, VanillaProgressPath, B);
            cloud.WriteFailure = path => path.Equals(
                VanillaProgressPath,
                StringComparison.OrdinalIgnoreCase
            )
                ? new InvalidOperationException(
                    "Commit returned file_committed=false for filesystem fixture"
                )
                : null;

            var failed = await AttemptAsync(() => RecoverAsync(local, cloud))
                .ConfigureAwait(false);
            ExpectNotSynchronized(failed, "automatic commit failure");
            Expect(failed.Error is not null
                    && failed.Error.Message.Contains(
                        "file_committed=false",
                        StringComparison.OrdinalIgnoreCase
                    ),
                "Commit failure did not propagate its failure detail.");
            Expect(LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Commit failure cleared pending recovery state.");
            ExpectLocalBytes(root, VanillaProgressPath, B);

            cloud.WriteFailure = null;
            var retry = await RecoverAsync(local, cloud).ConfigureAwait(false);
            ExpectOutcome(retry, AutomaticSyncOutcome.Synchronized);
            ExpectRemoteBytes(cloud, VanillaProgressPath, B);
            Expect(!LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Commit-failure retry did not clear pending state.");
        });

    private static Task AutomaticReadBackFailureStaysPendingAsync()
        => WithFilesystemAsync("read-back-failure", async (root, local) =>
        {
            var cloud = Store("filesystem-read-back-failure-steam");
            var context = VanillaContext();
            WriteLocal(local, VanillaProgressPath, A);
            cloud.SeedBytes(VanillaProgressPath, A);
            SeedMarker(cloud, context);
            await EstablishAsync(local, cloud, context).ConfigureAwait(false);
            await ExpectPreparedAsync(local, cloud, context).ConfigureAwait(false);
            WriteLocal(local, VanillaProgressPath, B);
            var writesBeforeFailure = cloud.WriteCountFor(VanillaProgressPath);
            cloud.VerificationRawReadTransform = (path, content) =>
                path.Equals(
                    VanillaProgressPath,
                    StringComparison.OrdinalIgnoreCase
                ) && cloud.WriteCountFor(path) > writesBeforeFailure
                    ? content.Concat(new byte[] { 0xFF }).ToArray()
                    : content;

            var failed = await AttemptAsync(() => RecoverAsync(local, cloud))
                .ConfigureAwait(false);
            ExpectNotSynchronized(failed, "automatic read-back mismatch");
            Expect(failed.Error is not null
                    && failed.Error.Message.Contains(
                        "hash mismatch",
                        StringComparison.OrdinalIgnoreCase
                    ),
                "Remote read-back mismatch did not propagate.");
            Expect(LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Read-back mismatch cleared pending recovery state.");
            ExpectLocalBytes(root, VanillaProgressPath, B);
            ExpectRemoteBytes(cloud, VanillaProgressPath, B);

            cloud.VerificationRawReadTransform = null;
            var retry = await RecoverAsync(local, cloud).ConfigureAwait(false);
            ExpectOutcome(retry, AutomaticSyncOutcome.Synchronized);
            ExpectRemoteBytes(cloud, VanillaProgressPath, B);
            Expect(!LocalExists(
                root,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ), "Read-back retry did not clear pending state.");
        });

    private static Task RestoreAndUndoRoundTripExactBytesAsync()
        => WithFilesystemAsync("restore-undo", async (root, local) =>
        {
            var context = VanillaContext();
            var advanced = SpecialBytes(
                "{\"ironclad\":10,\"name\":\"RÃ©gent\"}\r\n\0"
            );
            var regressed = SpecialBytes("{\"ironclad\":7}\r\n\0");
            var run = SpecialBytes("{\"floor\":12}\r\n\0");
            var extraHistory = SpecialBytes("{\"run\":\"new-local\"}\r\n\0");
            WriteLocal(local, "profile.save", Bytes("{\"profile\":1}"));
            WriteLocal(local, VanillaProgressPath, advanced);
            WriteLocal(local, HistoryA, Bytes("{\"run\":\"advanced\"}"));
            var source = await CloudSyncCoordinator
                .CaptureRetainedLocalSnapshotAsync(
                    local,
                    context,
                    "filesystem-recovery-test",
                    CancellationToken.None
                ).ConfigureAwait(false);

            WriteLocal(local, VanillaProgressPath, regressed);
            WriteLocal(local, CurrentRunPath, run);
            WriteLocal(local, HistoryB, extraHistory);
            var beforeRestore = SnapshotLiveFiles(root);

            var restored = await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                source.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                SteamId64,
                CancellationToken.None
            ).ConfigureAwait(false);
            Expect(restored.Status.SyncHeld && restored.Status.ValidationRequired,
                "Filesystem Restore did not enter validation hold.");
            ExpectLocalBytes(root, VanillaProgressPath, advanced);
            Expect(!LocalExists(root, CurrentRunPath),
                "Full filesystem Restore did not apply the current-run tombstone.");
            Expect(!LocalExists(root, HistoryB),
                "Full filesystem Restore did not apply the history tombstone.");

            var undone = await CloudSyncCoordinator.UndoSaveRecoveryAsync(
                local,
                CancellationToken.None
            ).ConfigureAwait(false);
            Expect(!undone.Status.SyncHeld && !undone.Status.CanUndo,
                "Filesystem Undo did not release recovery hold.");
            ExpectLiveFiles(root, beforeRestore);
            ExpectLocalBytes(root, VanillaProgressPath, regressed);
            ExpectLocalBytes(root, CurrentRunPath, run);
            ExpectLocalBytes(root, HistoryB, extraHistory);
            _ = await CloudSyncCoordinator.ReadVerifiedSnapshotAsync(
                local,
                source.Path,
                context,
                CancellationToken.None
            ).ConfigureAwait(false);
        });

    private static async Task EstablishAsync(
        AndroidLocalSaveStore local,
        InMemoryCloudSaveStore cloud,
        SaveContext context
    )
    {
        var result = await ReconcileAsync(
            local,
            cloud,
            context,
            AutomaticSyncSourceChoice.Local
        ).ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.Synchronized);
        ExpectRemoteText(
            cloud,
            context.MarkerPath,
            context.SerializeMarker()
        );
    }

    private static async Task ExpectPreparedAsync(
        AndroidLocalSaveStore local,
        InMemoryCloudSaveStore cloud,
        SaveContext context
    )
    {
        var result = await BeginAsync(local, cloud, context)
            .ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.GameSessionPrepared);
        Expect(result.CanStartGame, "Prepared session did not permit Play.");
    }

    private static Task<AutomaticSyncResult> ReconcileAsync(
        AndroidLocalSaveStore local,
        InMemoryCloudSaveStore cloud,
        SaveContext context,
        AutomaticSyncSourceChoice? sourceChoice = null
    )
        => CloudSyncCoordinator.ReconcileAutomaticSyncAsync(
            local,
            cloud,
            context.Namespace,
            context.RuntimeIdentity,
            context.ModSetFingerprint,
            sourceChoice,
            Progress(),
            CancellationToken.None
        );

    private static Task<AutomaticSyncResult> BeginAsync(
        AndroidLocalSaveStore local,
        InMemoryCloudSaveStore cloud,
        SaveContext context
    )
        => CloudSyncCoordinator.BeginAutomaticGameSessionAsync(
            local,
            cloud,
            context.Namespace,
            context.RuntimeIdentity,
            context.ModSetFingerprint,
            Progress(),
            CancellationToken.None
        );

    private static Task<AutomaticSyncResult> RecoverAsync(
        AndroidLocalSaveStore local,
        InMemoryCloudSaveStore cloud
    )
        => CloudSyncCoordinator.RecoverAutomaticSyncAsync(
            local,
            cloud,
            Progress(),
            CancellationToken.None
        );

    private static CloudOperationProgressTracker Progress()
        => new(CloudOperationKind.Push);

    private static SaveContext Context(ContextCase item)
        => SaveContext.Create(
            SteamId64,
            item.Namespace,
            item.Runtime,
            item.ModFingerprint
        );

    private static SaveContext VanillaContext()
        => SaveContext.Create(
            SteamId64,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            ""
        );

    private static InMemoryCloudSaveStore Store(string name)
        => new(name, SteamId64);

    private static void SeedMarker(
        InMemoryCloudSaveStore cloud,
        SaveContext context
    )
        => cloud.Seed(context.MarkerPath, context.SerializeMarker());

    private static void WriteLocal(
        AndroidLocalSaveStore local,
        string path,
        byte[] content
    )
        => ((ISaveStore)local).WriteFile(path, content);

    private static void ExpectLocalBytes(
        string root,
        string path,
        byte[] expected
    )
    {
        var actual = File.ReadAllBytes(FullPath(root, path));
        Expect(actual.AsSpan().SequenceEqual(expected),
            $"Filesystem bytes differ for {path}.");
    }

    private static void ExpectRemoteBytes(
        InMemoryCloudSaveStore cloud,
        string path,
        byte[] expected
    )
    {
        Expect(cloud.TryReadSeededBytes(path, out var actual),
            $"Fake Steam file is missing: {path}");
        Expect(actual.AsSpan().SequenceEqual(expected),
            $"Fake Steam bytes differ for {path}.");
    }

    private static void ExpectRemoteText(
        InMemoryCloudSaveStore cloud,
        string path,
        string expected
    )
    {
        Expect(cloud.TryReadSeeded(path, out var actual),
            $"Fake Steam text file is missing: {path}");
        Expect(actual == expected, $"Fake Steam text differs for {path}.");
    }

    private static string[] TransferBackupFiles(string root, string savePath)
    {
        var suffix = $"/files/{Canonical(savePath)}";
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path =>
            {
                var relative = Canonical(Path.GetRelativePath(root, path));
                return relative.StartsWith(
                        ".sts2-launcher/transfer-backups/",
                        StringComparison.OrdinalIgnoreCase
                    ) && relative.EndsWith(
                        suffix,
                        StringComparison.OrdinalIgnoreCase
                    );
            })
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, byte[]> SnapshotAllFiles(string root)
        => SnapshotFiles(root, includeLauncherState: true);

    private static Dictionary<string, byte[]> SnapshotLiveFiles(string root)
        => SnapshotFiles(root, includeLauncherState: false);

    private static Dictionary<string, byte[]> SnapshotFiles(
        string root,
        bool includeLauncherState
    )
        => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                Relative = Canonical(Path.GetRelativePath(root, path)),
            })
            .Where(file => includeLauncherState
                || !file.Relative.StartsWith(
                    ".sts2-launcher/",
                    StringComparison.OrdinalIgnoreCase
                ))
            .ToDictionary(
                file => file.Relative,
                file => File.ReadAllBytes(file.Path),
                StringComparer.OrdinalIgnoreCase
            );

    private static void ExpectAllFiles(
        string root,
        IReadOnlyDictionary<string, byte[]> expected
    )
        => ExpectFiles(root, expected, includeLauncherState: true);

    private static void ExpectLiveFiles(
        string root,
        IReadOnlyDictionary<string, byte[]> expected
    )
        => ExpectFiles(root, expected, includeLauncherState: false);

    private static void ExpectFiles(
        string root,
        IReadOnlyDictionary<string, byte[]> expected,
        bool includeLauncherState
    )
    {
        var actual = SnapshotFiles(root, includeLauncherState);
        Expect(actual.Count == expected.Count,
            "Filesystem live-file set changed unexpectedly.");
        foreach (var pair in expected)
        {
            Expect(actual.TryGetValue(pair.Key, out var bytes),
                $"Filesystem live file is missing: {pair.Key}");
            Expect(bytes.AsSpan().SequenceEqual(pair.Value),
                $"Filesystem live bytes changed: {pair.Key}");
        }
    }

    private static void ExpectVerifiedAfterWrite(
        IEnumerable<string> operations,
        string path,
        string scenario
    )
    {
        var canonical = Canonical(path);
        var trace = operations.ToArray();
        var writeIndex = Array.FindIndex(trace, operation =>
            operation.Equals(
                $"write-async:complete:{canonical}",
                StringComparison.OrdinalIgnoreCase
            ) || operation.Equals(
                $"raw-write:complete:{canonical}",
                StringComparison.OrdinalIgnoreCase
            ) || operation.Equals(
                $"write:{canonical}",
                StringComparison.OrdinalIgnoreCase
            ));
        var verificationIndex = writeIndex < 0
            ? -1
            : Array.FindIndex(
                trace,
                writeIndex + 1,
                operation => operation.Equals(
                    $"verify-read:{canonical}",
                    StringComparison.OrdinalIgnoreCase
                ) || operation.Equals(
                    $"verify-raw-read:{canonical}",
                    StringComparison.OrdinalIgnoreCase
                )
            );
        Expect(
            writeIndex >= 0 && verificationIndex > writeIndex,
            $"{scenario} reported synchronized without post-write remote verification."
        );
    }

    private static bool LocalExists(string root, string path)
        => File.Exists(FullPath(root, path));

    private static string FullPath(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root);
        var candidate = Path.GetFullPath(Path.Combine(
            fullRoot,
            Canonical(path).Replace('/', Path.DirectorySeparatorChar)
        ));
        var prefix = fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Fixture path escaped its root: {path}");
        return candidate;
    }

    private static string Canonical(string path)
        => path.Replace('\\', '/').Trim('/');

    private static byte[] Bytes(string content)
        => Encoding.UTF8.GetBytes(content);

    private static byte[] SpecialBytes(string content)
        => Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(content))
            .ToArray();

    private static async Task<AutomaticAttempt> AttemptAsync(
        Func<Task<AutomaticSyncResult>> operation
    )
    {
        try
        {
            return new AutomaticAttempt(
                await operation().ConfigureAwait(false),
                null
            );
        }
        catch (Exception ex)
        {
            return new AutomaticAttempt(null, ex);
        }
    }

    private static void ExpectNotSynchronized(
        AutomaticAttempt attempt,
        string scenario
    )
    {
        Expect(attempt.Error is not null || attempt.Result.HasValue,
            $"{scenario} produced neither a result nor a failure.");
        Expect(
            !attempt.Result.HasValue
                || attempt.Result.Value.Outcome
                    != AutomaticSyncOutcome.Synchronized,
            $"{scenario} falsely reported Synchronized."
        );
    }

    private static void ExpectOutcome(
        AutomaticSyncResult result,
        AutomaticSyncOutcome expected
    )
        => Expect(result.Outcome == expected,
            $"Expected {expected}, got {result.Outcome}: {result.Message}");

    private static async Task WithFilesystemAsync(
        string name,
        Func<string, AndroidLocalSaveStore, Task> scenario
    )
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"sts2-stage5-{name}-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(root);
        try
        {
            await scenario(root, new AndroidLocalSaveStore(root))
                .ConfigureAwait(false);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static async Task RunAsync(string name, Func<Task> scenario)
    {
        try
        {
            await scenario().ConfigureAwait(false);
            _passed++;
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"FAIL filesystem scenario '{name}': {ex.Message}",
                ex
            );
        }
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record ContextCase(
        string Name,
        SaveNamespace Namespace,
        string Runtime,
        string ModFingerprint
    );

    private readonly record struct AutomaticAttempt(
        AutomaticSyncResult? Result,
        Exception? Error
    );
}
