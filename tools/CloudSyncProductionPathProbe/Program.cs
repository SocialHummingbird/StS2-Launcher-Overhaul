#nullable enable

using System.Collections.Concurrent;
using System.Text;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal static class Program
{
    private const ulong SteamId64 = InMemoryCloudSaveStore.DefaultSteamId64;
    private const ulong OtherSteamId64 = SteamId64 + 1;
    private const string ProgressPath = "profile1/saves/progress.save";
    private const string LegacyPrefsPath = "profile1/saves/prefs";
    private const string PrefsPath = "profile1/saves/prefs.save";
    private const string CurrentRunPath = "profile1/saves/current_run.save";
    private const string Profile2ProgressPath = "profile2/saves/progress.save";
    private const string Profile2LegacyPrefsPath = "profile2/saves/prefs";
    private const string Profile2PrefsPath = "profile2/saves/prefs.save";
    private const string Profile2CurrentRunPath = "profile2/saves/current_run.save";
    private const string Profile2MultiplayerRunPath = "profile2/saves/current_run_mp.save";
    private const string HistoryPath = "profile1/saves/history/20260806.run";
    private const string Profile2HistoryPath = "profile2/saves/history/20260805.run";
    private const string OrphanHistoryBackupPath =
        "profile2/saves/history/20260804.run.backup";
    private const string RejectedHistoryBakPath =
        "profile2/saves/history/20260803.run.bak";
    private const string SharedProfilePath = "profile.save";
    private const string RejectedArbitraryPath = "profile2/saves/notes.txt";
    private const string RejectedBackupPath = "profile2/saves/notes.txt.backup";
    private const string RejectedModdedProfilePath = "modded/profile.save";
    private const string ModdedProgressPath = "modded/profile1/saves/progress.save";
    private const string ModdedPrefsPath = "modded/profile1/saves/prefs.save";
    private const string SettingsPath = "settings.save";
    private const string PublicBeta = "public-beta";
    private const string ModFingerprintA = "sha256:mods-a";
    private const string ModFingerprintB = "sha256:mods-b";
    private const string NewProgress = "{\"progress\":\"new\"}";
    private const string OldProgress = "{\"progress\":\"old\"}";
    private const string VanillaIncompletePullPath =
        ".sts2-launcher/pull-incomplete/vanilla.json";
    private const string ModdedIncompletePullPath =
        ".sts2-launcher/pull-incomplete/modded.json";
    private const string IncompletePullMarkerContent =
        "{\"version\":1,\"state\":\"pull-incomplete\"}";
    private static int _passed;

    private static async Task Main(string[] args)
    {
        Environment.SetEnvironmentVariable(
            "STS2_BOOTSTRAP_TRACE_FILE",
            Path.Combine(
                Path.GetTempPath(),
                "sts2-cloud-sync-production-path-probe.log"
            )
        );
        CloudSyncCoordinator.SetLocalBackupEnabled(false);
        if (Stage5ProcessDeathProbe.IsWorkerInvocation(args))
        {
            await Stage5ProcessDeathProbe.RunWorkerAsync(args)
                .ConfigureAwait(false);
            return;
        }

        await RunAsync(
            "successful Push is verified, allowlisted, and backed up",
            SuccessfulPushAsync
        );
        await RunAsync(
            "successful Pull is verified and backed up",
            SuccessfulPullAsync
        );
        await RunAsync(
            "Push transfers an immutable source snapshot",
            ImmutablePushSnapshotAsync
        );
        await RunAsync(
            "modded allowlist cannot cross into vanilla or settings",
            ModdedAllowlistIsolationAsync
        );
        await RunAsync(
            "commit-style upload failure propagates",
            CommitStyleFailureAsync
        );
        await RunAsync(
            "file_committed=false is rejected",
            FileCommittedFalseIsRejectedAsync
        );
        await RunAsync(
            "authentication failure propagates",
            AuthenticationFailureAsync
        );
        await RunAsync(
            "connection loss propagates",
            ConnectionLossAsync
        );
        await RunAsync(
            "download failure propagates",
            DownloadFailureAsync
        );
        await RunAsync(
            "remote read-back hash mismatch fails Push",
            RemoteReadBackMismatchAsync
        );
        await RunAsync(
            "unreadable final marker recovers only through confirmed Push",
            UnreadableFinalMarkerRecoveryAsync
        );
        await RunAsync(
            "Push current-run tombstone backs up then deletes",
            PushCurrentRunTombstoneAsync
        );
        await RunAsync(
            "Pull current-run tombstone backs up then deletes",
            PullCurrentRunTombstoneAsync
        );
        await RunAsync(
            "tombstone deletion failure propagates",
            TombstoneDeleteFailureAsync
        );
        await RunAsync(
            "history listing failures propagate before mutation",
            HistoryListingFailureAsync
        );
        await RunAsync(
            "destination creation and change drift abort before mutation",
            DestinationDriftBeforeMutationAsync
        );
        await RunAsync(
            "save-context drift aborts before invalidation",
            ContextDriftBeforeInvalidationAsync
        );
        await RunAsync(
            "concurrent replacement context survives failed marker cleanup",
            ConcurrentMarkerReplacementSurvivesCleanupAsync
        );
        await RunAsync(
            "late Push and Pull drift cannot report verified success",
            LateFinalDriftAsync
        );
        await RunAsync(
            "Steam account mismatch blocks before mutation",
            AccountMismatchAsync
        );
        await RunAsync(
            "save namespace mismatch blocks before mutation",
            NamespaceMismatchAsync
        );
        await RunAsync(
            "runtime branch mismatch blocks before mutation",
            RuntimeMismatchAsync
        );
        await RunAsync(
            "mod-set mismatch blocks before mutation",
            ModSetMismatchAsync
        );
        await RunAsync(
            "failed Pull leaves an incomplete marker that blocks Push",
            FailedPullLeavesIncompleteMarkerAsync
        );
        await RunAsync(
            "shared root profile alone is ineligible and rejected",
            SharedRootProfileAloneIsRejectedAsync
        );
        await RunAsync(
            "empty allowlisted sources fail before mutation",
            EmptyAllowlistedSourcesFailBeforeMutationAsync
        );

        var automaticPassed = await AutomaticSyncScenarios.RunAllAsync()
            .ConfigureAwait(false);
        var recoveryPassed = await SaveRecoveryScenarios.RunAllAsync()
            .ConfigureAwait(false);
        var filesystemPassed = await FilesystemAutomaticSyncScenarios
            .RunAllAsync()
            .ConfigureAwait(false);

        Console.WriteLine(
            $"Cloud sync production-path probe passed "
                + $"{_passed + automaticPassed + recoveryPassed + filesystemPassed}"
                + "/69 scenarios."
        );
        Console.WriteLine(
            "The 69-scenario audit above used filesystem/in-memory local stores and the deterministic fake Steam store only; no credentials, network, hardware, or real Steam Cloud operation was used."
        );
        await Stage5ProcessDeathProbe.RunAllAsync().ConfigureAwait(false);
    }

    private static async Task SuccessfulPushAsync()
    {
        var trace = new ConcurrentQueue<string>();
        var local = Store("local", trace);
        var cloud = Store("cloud", trace);
        var context = VanillaContext();
        var newProgressBytes = RawTransferFixture(NewProgress);
        var oldProgressBytes = RawTransferFixture(OldProgress);
        local.Seed("profile.save", "{\"profile\":1}");
        local.SeedBytes(ProgressPath, newProgressBytes);
        local.Seed(LegacyPrefsPath, "{\"legacyPrefs\":\"new\"}");
        local.Seed(PrefsPath, "{\"prefs\":\"new\"}");
        local.Seed(HistoryPath, "{\"run\":\"allowed\"}");
        local.Seed(SettingsPath, "{\"device\":\"android-only\"}");
        local.Seed($"{ProgressPath}.backup", "not-authoritative");
        local.Seed($"{LegacyPrefsPath}.backup", "not-authoritative");
        local.Seed($"{HistoryPath}.bak", "not-authoritative");
        local.Seed(ModdedProgressPath, "{\"modded\":true}");
        cloud.SeedBytes(ProgressPath, oldProgressBytes);
        cloud.Seed($"{SharedProfilePath}.backup", "stale-root-sidecar");
        cloud.Seed($"{ProgressPath}.backup", "stale-progress-sidecar");
        cloud.Seed($"{HistoryPath}.backup", "stale-history-sidecar");
        SeedMarker(cloud, context);

        var progress = Progress(CloudOperationKind.Push);
        var result = await PushAsync(local, cloud, progress).ConfigureAwait(false);

        ExpectSuccessfulResult(result, progress, CloudOperationKind.Push);
        ExpectBytes(cloud, ProgressPath, newProgressBytes);
        ExpectContent(cloud, LegacyPrefsPath, "{\"legacyPrefs\":\"new\"}");
        ExpectContent(cloud, PrefsPath, "{\"prefs\":\"new\"}");
        ExpectContent(cloud, HistoryPath, "{\"run\":\"allowed\"}");
        ExpectContent(cloud, context.MarkerPath, context.SerializeMarker());
        ExpectMissing(local, VanillaIncompletePullPath);
        ExpectMissing(local, ModdedIncompletePullPath);
        ExpectMissing(cloud, SettingsPath);
        ExpectMissing(cloud, $"{ProgressPath}.backup");
        ExpectMissing(cloud, $"{LegacyPrefsPath}.backup");
        ExpectMissing(cloud, $"{HistoryPath}.bak");
        ExpectMissing(cloud, $"{SharedProfilePath}.backup");
        ExpectMissing(cloud, $"{HistoryPath}.backup");
        ExpectMissing(cloud, ModdedProgressPath);

        var progressBackup = FindBackupPath(local, ProgressPath);
        ExpectBytes(local, progressBackup, oldProgressBytes);
        var markerBackup = FindBackupPath(local, context.MarkerPath);
        ExpectContent(local, markerBackup, context.SerializeMarker());
        var rootSidecarBackup = FindBackupPath(
            local,
            $"{SharedProfilePath}.backup"
        );
        ExpectContent(local, rootSidecarBackup, "stale-root-sidecar");
        var progressSidecarBackup = FindBackupPath(
            local,
            $"{ProgressPath}.backup"
        );
        ExpectContent(local, progressSidecarBackup, "stale-progress-sidecar");
        var historySidecarBackup = FindBackupPath(
            local,
            $"{HistoryPath}.backup"
        );
        ExpectContent(local, historySidecarBackup, "stale-history-sidecar");
        ExpectBefore(
            trace,
            $"local:write:{progressBackup}",
            $"cloud:write:{ProgressPath}",
            "Push destination backup must finish before overwrite"
        );
        ExpectBefore(
            trace,
            $"local:write:{markerBackup}",
            $"cloud:delete-async:start:{context.MarkerPath}",
            "Push context-marker backup must finish before invalidation"
        );
        ExpectBefore(
            trace,
            $"cloud:delete-async:complete:{context.MarkerPath}",
            $"cloud:raw-write:start:{ProgressPath}",
            "Push must invalidate the context marker before writing data"
        );
        ExpectBefore(
            trace,
            $"cloud:verify-raw-read:{ProgressPath}",
            $"cloud:write-async:start:{context.MarkerPath}",
            "Push must not recreate the context marker before data verification"
        );
        ExpectBefore(
            trace,
            $"cloud:raw-write:complete:{ProgressPath}",
            $"cloud:delete-async:start:{ProgressPath}.backup",
            "Push must apply paired sidecar cleanup after its primary"
        );
        ExpectBefore(
            trace,
            "cloud:metadata:prepared",
            "local:directory-exists:profile1/saves/history",
            "Push must prepare strict cloud metadata before source history enumeration"
        );
        ExpectBefore(
            trace,
            "cloud:metadata:prepared",
            "cloud:directory-exists:profile1/saves/history",
            "Push must prepare strict cloud metadata before destination history enumeration"
        );
        Expect(
            local.Operations
                .Where(operation => operation.StartsWith("list-files:", StringComparison.Ordinal))
                .All(operation => operation.Contains("/history", StringComparison.Ordinal)),
            "Push enumerated a directory outside an explicit history allowlist."
        );
    }

    private static async Task SuccessfulPullAsync()
    {
        var trace = new ConcurrentQueue<string>();
        var local = Store("local", trace);
        var cloud = Store("cloud", trace);
        var context = VanillaContext();
        var newProgressBytes = RawTransferFixture(NewProgress);
        var oldProgressBytes = RawTransferFixture(OldProgress);
        local.SeedBytes(ProgressPath, oldProgressBytes);
        local.Seed($"{ProgressPath}.backup", "stale-local-progress-sidecar");
        local.Seed($"{LegacyPrefsPath}.backup", "stale-local-prefs-sidecar");
        local.Seed(HistoryPath, "{\"run\":\"old-local\"}");
        local.Seed($"{HistoryPath}.backup", "stale-local-history-sidecar");
        local.Seed(SettingsPath, "{\"device\":\"keep-local\"}");
        cloud.SeedBytes(ProgressPath, newProgressBytes);
        cloud.Seed(LegacyPrefsPath, "{\"legacyPrefs\":\"cloud\"}");
        cloud.Seed(PrefsPath, "{\"prefs\":\"cloud\"}");
        cloud.Seed(HistoryPath, "{\"run\":\"cloud\"}");
        cloud.Seed($"{LegacyPrefsPath}.backup", "not-authoritative");
        cloud.Seed($"{HistoryPath}.backup", "not-authoritative");
        cloud.Seed(SettingsPath, "{\"device\":\"must-not-pull\"}");
        cloud.Seed(ModdedProgressPath, "{\"modded\":true}");
        SeedMarker(cloud, context);
        var sawIncompleteContextBeforeData = false;
        local.BeforeWriteAsync = (path, _) =>
        {
            if (path.Equals(ProgressPath, StringComparison.OrdinalIgnoreCase))
            {
                ExpectContent(
                    local,
                    VanillaIncompletePullPath,
                    IncompletePullMarkerContent
                );
                sawIncompleteContextBeforeData = true;
            }
            return Task.CompletedTask;
        };

        var progress = Progress(CloudOperationKind.Pull);
        var result = await PullAsync(local, cloud, progress).ConfigureAwait(false);

        ExpectSuccessfulResult(result, progress, CloudOperationKind.Pull);
        ExpectBytes(local, ProgressPath, newProgressBytes);
        ExpectContent(local, LegacyPrefsPath, "{\"legacyPrefs\":\"cloud\"}");
        ExpectContent(local, PrefsPath, "{\"prefs\":\"cloud\"}");
        ExpectContent(local, HistoryPath, "{\"run\":\"cloud\"}");
        ExpectContent(local, SettingsPath, "{\"device\":\"keep-local\"}");
        ExpectMissing(local, $"{ProgressPath}.backup");
        ExpectMissing(local, $"{LegacyPrefsPath}.backup");
        ExpectMissing(local, $"{HistoryPath}.backup");
        ExpectMissing(local, ModdedProgressPath);
        ExpectMissing(local, VanillaIncompletePullPath);
        Expect(
            sawIncompleteContextBeforeData,
            "Pull did not persist an incomplete context before data writes."
        );
        var backup = FindBackupPath(local, ProgressPath);
        ExpectBytes(local, backup, oldProgressBytes);
        ExpectContent(
            local,
            FindBackupPath(local, $"{ProgressPath}.backup"),
            "stale-local-progress-sidecar"
        );
        ExpectContent(
            local,
            FindBackupPath(local, $"{LegacyPrefsPath}.backup"),
            "stale-local-prefs-sidecar"
        );
        ExpectContent(
            local,
            FindBackupPath(local, $"{HistoryPath}.backup"),
            "stale-local-history-sidecar"
        );
        ExpectBefore(
            trace,
            $"local:write:{backup}",
            $"local:write:{ProgressPath}",
            "Pull destination backup must finish before overwrite"
        );
        ExpectBefore(
            trace,
            $"local:write:{backup}",
            $"local:write-async:start:{VanillaIncompletePullPath}",
            "Pull data backup must finish before marking the Pull incomplete"
        );
        ExpectBefore(
            trace,
            $"local:write-async:complete:{VanillaIncompletePullPath}",
            $"local:read-async:{VanillaIncompletePullPath}",
            "Pull must verify its incomplete marker"
        );
        ExpectBefore(
            trace,
            $"local:read-async:{VanillaIncompletePullPath}",
            $"local:raw-write:start:{ProgressPath}",
            "Pull must verify its incomplete marker before writing save data"
        );
        ExpectBefore(
            trace,
            $"local:raw-read:{PrefsPath}",
            $"local:delete-async:start:{VanillaIncompletePullPath}",
            "Pull must retain its incomplete marker through data verification"
        );
        ExpectBefore(
            trace,
            $"local:delete-async:start:{VanillaIncompletePullPath}",
            $"local:delete-async:complete:{VanillaIncompletePullPath}",
            "Pull must await deletion of its incomplete marker"
        );
        ExpectBefore(
            trace,
            $"local:raw-write:complete:{HistoryPath}",
            $"local:delete-async:start:{HistoryPath}.backup",
            "Pull must apply paired history sidecar cleanup after its primary"
        );
        ExpectBefore(
            trace,
            "cloud:metadata:prepared",
            "cloud:directory-exists:profile1/saves/history",
            "Pull must prepare strict cloud metadata before source history enumeration"
        );
        ExpectBefore(
            trace,
            "cloud:metadata:prepared",
            "local:directory-exists:profile1/saves/history",
            "Pull must prepare strict cloud metadata before destination history enumeration"
        );
        Expect(
            trace
                .Where(operation => operation.StartsWith(
                    "local:write:",
                    StringComparison.OrdinalIgnoreCase
                ) || operation.StartsWith(
                    "local:delete-async:complete:",
                    StringComparison.OrdinalIgnoreCase
                ))
                .Last()
                .Equals(
                    $"local:delete-async:complete:{VanillaIncompletePullPath}",
                    StringComparison.OrdinalIgnoreCase
                ),
            "Pull incomplete-marker deletion was not the final local mutation."
        );
        Expect(
            cloud.WriteCount == 0 && cloud.DeleteCount == 0,
            "Pull mutated a cloud source whose context marker already existed."
        );
    }

    private static async Task ImmutablePushSnapshotAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        const string captured = "{\"snapshot\":\"captured\"}";
        const string changedLater = "{\"snapshot\":\"changed-after-capture\"}";
        local.Seed(ProgressPath, captured);
        var mutated = false;
        local.BeforeWriteAsync = (path, _) =>
        {
            if (!mutated && IsTransferBackupPath(path))
            {
                mutated = true;
                local.Seed(ProgressPath, changedLater);
            }
            return Task.CompletedTask;
        };

        var result = await PushAsync(
            local,
            cloud,
            Progress(CloudOperationKind.Push)
        ).ConfigureAwait(false);

        Expect(result.TransferredPathCount > 0, "Immutable Push did not succeed.");
        Expect(mutated, "The source mutation hook did not execute after snapshot capture.");
        ExpectContent(local, ProgressPath, changedLater);
        ExpectContent(cloud, ProgressPath, captured);
    }

    private static async Task ModdedAllowlistIsolationAsync()
    {
        Expect(
            SaveTransferAllowlist.IsAllowedPath(
                SaveNamespace.Vanilla,
                "profile.save"
            ),
            "Vanilla context rejected shared root profile.save."
        );
        Expect(
            SaveTransferAllowlist.IsAllowedPath(
                SaveNamespace.Modded,
                "profile.save"
            ),
            "Modded context rejected shared root profile.save."
        );
        Expect(
            !SaveTransferAllowlist.IsAllowedPath(
                SaveNamespace.Vanilla,
                RejectedModdedProfilePath
            )
                && !SaveTransferAllowlist.IsAllowedPath(
                    SaveNamespace.Modded,
                    RejectedModdedProfilePath
                ),
            "A context accepted non-existent modded/profile.save as transferable."
        );

        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        local.Seed("profile.save", "{\"sharedProfile\":true}");
        local.Seed(RejectedModdedProfilePath, "{\"mustNotTransfer\":true}");
        local.Seed(ProgressPath, "{\"vanillaProgress\":true}");
        local.Seed(ModdedProgressPath, "{\"moddedProgress\":true}");
        local.Seed(ModdedPrefsPath, "{\"moddedPrefs\":true}");
        local.Seed(SettingsPath, "{\"device\":true}");

        var progress = Progress(CloudOperationKind.Push);
        var result = await PushAsync(
            local,
            cloud,
            progress,
            SaveNamespace.Modded,
            PublicBeta,
            ModFingerprintA
        ).ConfigureAwait(false);

        ExpectSuccessfulResult(result, progress, CloudOperationKind.Push);
        ExpectContent(cloud, "profile.save", "{\"sharedProfile\":true}");
        ExpectContent(cloud, ModdedProgressPath, "{\"moddedProgress\":true}");
        ExpectContent(cloud, ModdedPrefsPath, "{\"moddedPrefs\":true}");
        ExpectMissing(cloud, RejectedModdedProfilePath);
        ExpectMissing(cloud, ProgressPath);
        ExpectMissing(cloud, SettingsPath);
    }

    private static async Task CommitStyleFailureAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        var context = VanillaContext();
        local.Seed(ProgressPath, NewProgress);
        SeedMarker(cloud, context);
        cloud.WriteFailure = path => path.Equals(
            ProgressPath,
            StringComparison.OrdinalIgnoreCase
        )
            ? new InvalidOperationException(
                "Commit returned file_committed=false for injected upload"
            )
            : null;
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "file_committed=false");
        ExpectMissing(cloud, ProgressPath);
        ExpectMissing(cloud, context.MarkerPath);
        ExpectContent(
            local,
            FindBackupPath(local, context.MarkerPath),
            context.SerializeMarker()
        );

        var failedPullProgress = Progress(CloudOperationKind.Pull);
        var failedPull = await ExpectTransferFailureAsync(
            CloudOperationKind.Pull,
            failedPullProgress,
            () => PullAsync(local, cloud, failedPullProgress)
        ).ConfigureAwait(false);
        ExpectContains(failedPull.Message, "trusted launcher context");
        ExpectMissing(cloud, context.MarkerPath);
    }

    private static Task FileCommittedFalseIsRejectedAsync()
    {
        var rejected = false;
        try
        {
            SteamKit2CloudSaveStore.RequireCommitted(
                ProgressPath,
                uploadSucceeded: true,
                fileCommitted: false
            );
        }
        catch (InvalidOperationException ex)
        {
            rejected = ex.Message.Contains(
                "file_committed=false",
                StringComparison.OrdinalIgnoreCase
            );
        }

        Expect(rejected, "Production commit guard accepted file_committed=false.");
        SteamKit2CloudSaveStore.RequireCommitted(
            ProgressPath,
            uploadSucceeded: true,
            fileCommitted: true
        );
        return Task.CompletedTask;
    }

    private static async Task AuthenticationFailureAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud")
        {
            AuthenticationFailure = new UnauthorizedAccessException(
                "Injected Steam authentication failure."
            ),
        };
        local.Seed(ProgressPath, NewProgress);
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "authentication failure");
        ExpectNoDestinationMutation(local, cloud, "authentication failure");
    }

    private static async Task ConnectionLossAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        var context = VanillaContext();
        cloud.Seed(ProgressPath, NewProgress);
        SeedMarker(cloud, context);
        cloud.BeforeMetadataAsync = _ => throw new IOException(
            "Injected Steam connection loss during metadata download."
        );
        var progress = Progress(CloudOperationKind.Pull);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Pull,
            progress,
            () => PullAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "connection loss");
        ExpectMissing(local, ProgressPath);
    }

    private static async Task DownloadFailureAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        var context = VanillaContext();
        cloud.Seed(ProgressPath, NewProgress);
        SeedMarker(cloud, context);
        cloud.ReadFailure = path => path.Equals(
            ProgressPath,
            StringComparison.OrdinalIgnoreCase
        )
            ? new IOException("Injected Steam Cloud download failure.")
            : null;
        var progress = Progress(CloudOperationKind.Pull);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Pull,
            progress,
            () => PullAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "download failure");
        ExpectMissing(local, ProgressPath);
    }

    private static async Task RemoteReadBackMismatchAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        local.Seed(ProgressPath, NewProgress);
        cloud.VerificationRawReadTransform = (path, content) =>
            path.Equals(ProgressPath, StringComparison.OrdinalIgnoreCase)
                && cloud.WriteCountFor(path) > 0
                ? content.Concat(new byte[] { 0xA5 }).ToArray()
                : content;
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "hash mismatch");
        ExpectContent(cloud, ProgressPath, NewProgress);
        ExpectMissing(cloud, VanillaContext().MarkerPath);
    }

    private static async Task UnreadableFinalMarkerRecoveryAsync()
    {
        const string unreadableMarker = "{not-valid-json";
        var trace = new ConcurrentQueue<string>();
        var local = Store("local", trace);
        var cloud = Store("cloud", trace);
        var context = VanillaContext();
        local.Seed(ProgressPath, NewProgress);
        var corruptNextMarkerRead = true;
        cloud.VerificationReadTransform = (path, content) =>
        {
            if (
                corruptNextMarkerRead
                && path.Equals(
                    context.MarkerPath,
                    StringComparison.OrdinalIgnoreCase
                )
                && cloud.WriteCountFor(path) > 0
            )
            {
                corruptNextMarkerRead = false;
                return content + "-corrupted-read-back";
            }

            return content;
        };

        var failedFinalizeProgress = Progress(CloudOperationKind.Push);
        var failedFinalize = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            failedFinalizeProgress,
            () => PushAsync(local, cloud, failedFinalizeProgress)
        ).ConfigureAwait(false);

        ExpectContains(failedFinalize.Message, "hash mismatch");
        ExpectMissing(cloud, context.MarkerPath);

        cloud.VerificationReadTransform = null;
        cloud.Seed(context.MarkerPath, unreadableMarker);
        var localWritesBeforePull = local.WriteCount;
        var localDeletesBeforePull = local.DeleteCount;
        var cloudWritesBeforePull = cloud.WriteCount;
        var cloudDeletesBeforePull = cloud.DeleteCount;
        var failedPullProgress = Progress(CloudOperationKind.Pull);
        var failedPull = await ExpectTransferFailureAsync(
            CloudOperationKind.Pull,
            failedPullProgress,
            () => PullAsync(local, cloud, failedPullProgress)
        ).ConfigureAwait(false);

        Expect(
            failedPull is InvalidDataException,
            "Pull did not fail closed on an unreadable save-context marker."
        );
        Expect(
            local.WriteCount == localWritesBeforePull
                && local.DeleteCount == localDeletesBeforePull
                && cloud.WriteCount == cloudWritesBeforePull
                && cloud.DeleteCount == cloudDeletesBeforePull,
            "Pull mutated state after reading an unreadable context marker."
        );

        trace.Clear();
        var deletesBeforeRecovery = cloud.DeleteCount;
        var confirmedPushProgress = Progress(CloudOperationKind.Push);
        var confirmedPush = await PushAsync(
            local,
            cloud,
            confirmedPushProgress
        ).ConfigureAwait(false);

        ExpectSuccessfulResult(
            confirmedPush,
            confirmedPushProgress,
            CloudOperationKind.Push
        );
        var markerBackup = FindBackupPath(local, context.MarkerPath);
        ExpectContent(local, markerBackup, unreadableMarker);
        Expect(
            cloud.DeleteCount == deletesBeforeRecovery + 1,
            "Confirmed Push did not invalidate the unreadable context marker."
        );
        ExpectBefore(
            trace,
            $"local:write:{markerBackup}",
            $"cloud:delete-async:start:{context.MarkerPath}",
            "Unreadable context marker must be backed up before invalidation"
        );
        ExpectContent(cloud, context.MarkerPath, context.SerializeMarker());
        Expect(
            SaveContext.ParseMarker(
                cloud.TryReadSeeded(context.MarkerPath, out var marker)
                    ? marker
                    : throw new InvalidOperationException(
                        "Recovered context marker is missing."
                    )
            ).Equals(context),
            "Confirmed Push did not write a valid matching context marker."
        );
    }

    private static async Task PushCurrentRunTombstoneAsync()
    {
        var trace = new ConcurrentQueue<string>();
        var local = Store("local", trace);
        var cloud = Store("cloud", trace);
        var context = VanillaContext();
        local.Seed(ProgressPath, NewProgress);
        var staleAuthoritativeFiles = new Dictionary<string, string>
        {
            [$"{ProgressPath}.backup"] = "stale-written-primary-sidecar",
            [Profile2ProgressPath] = "{\"progress\":\"stale-profile2\"}",
            [$"{Profile2ProgressPath}.backup"] = "stale-profile2-progress-sidecar",
            [Profile2LegacyPrefsPath] = "{\"prefs\":\"stale-legacy\"}",
            [$"{Profile2LegacyPrefsPath}.backup"] = "stale-legacy-sidecar",
            [Profile2PrefsPath] = "{\"prefs\":\"stale-save\"}",
            [$"{Profile2PrefsPath}.backup"] = "stale-prefs-sidecar",
            [Profile2CurrentRunPath] = "{\"run\":\"stale-profile2\"}",
            [Profile2MultiplayerRunPath] = "{\"run\":\"stale-profile2-mp\"}",
            [Profile2HistoryPath] = "{\"run\":\"stale-history\"}",
            [$"{Profile2HistoryPath}.backup"] = "stale-history-sidecar",
            [OrphanHistoryBackupPath] = "stale-orphan-history-sidecar",
        };
        foreach (var (path, content) in staleAuthoritativeFiles)
            cloud.Seed(path, content);
        var rejectedFiles = new Dictionary<string, string>
        {
            [SharedProfilePath] = "{\"shared\":\"keep\"}",
            [$"{SharedProfilePath}.backup"] = "keep-root-sidecar",
            [SettingsPath] = "{\"settings\":\"keep\"}",
            [RejectedArbitraryPath] = "keep-arbitrary",
            [RejectedBackupPath] = "keep-arbitrary-backup",
            [RejectedHistoryBakPath] = "keep-history-bak",
        };
        foreach (var (path, content) in rejectedFiles)
            cloud.Seed(path, content);

        var progress = Progress(CloudOperationKind.Push);
        var result = await PushAsync(local, cloud, progress).ConfigureAwait(false);

        ExpectSuccessfulResult(result, progress, CloudOperationKind.Push);
        ExpectContent(cloud, context.MarkerPath, context.SerializeMarker());
        foreach (var (path, content) in staleAuthoritativeFiles)
        {
            ExpectMissing(cloud, path);
            var backup = FindBackupPath(local, path);
            ExpectContent(local, backup, content);
            ExpectBefore(
                trace,
                $"local:write:{backup}",
                $"cloud:delete-async:start:{path}",
                $"Push tombstone backup must finish before deleting {path}"
            );
        }
        foreach (var (path, content) in rejectedFiles)
            ExpectContent(cloud, path, content);
        ExpectBefore(
            trace,
            $"cloud:delete-async:complete:{Profile2HistoryPath}",
            $"cloud:delete-async:start:{Profile2HistoryPath}.backup",
            "Push must clean a destination-only history sidecar after its primary"
        );
        ExpectBefore(
            trace,
            $"cloud:delete-async:complete:{Profile2HistoryPath}",
            $"cloud:delete-async:start:{OrphanHistoryBackupPath}",
            "Push must clean orphan allowed history sidecars after history primaries"
        );
    }

    private static async Task PullCurrentRunTombstoneAsync()
    {
        var trace = new ConcurrentQueue<string>();
        var local = Store("local", trace);
        var cloud = Store("cloud", trace);
        var context = VanillaContext();
        var staleAuthoritativeFiles = new Dictionary<string, string>
        {
            [$"{ProgressPath}.backup"] = "stale-written-primary-sidecar",
            [Profile2ProgressPath] = "{\"progress\":\"stale-profile2\"}",
            [$"{Profile2ProgressPath}.backup"] = "stale-profile2-progress-sidecar",
            [Profile2LegacyPrefsPath] = "{\"prefs\":\"stale-legacy\"}",
            [$"{Profile2LegacyPrefsPath}.backup"] = "stale-legacy-sidecar",
            [Profile2PrefsPath] = "{\"prefs\":\"stale-save\"}",
            [$"{Profile2PrefsPath}.backup"] = "stale-prefs-sidecar",
            [Profile2CurrentRunPath] = "{\"run\":\"stale-profile2\"}",
            [Profile2MultiplayerRunPath] = "{\"run\":\"stale-profile2-mp\"}",
            [Profile2HistoryPath] = "{\"run\":\"stale-history\"}",
            [$"{Profile2HistoryPath}.backup"] = "stale-history-sidecar",
            [OrphanHistoryBackupPath] = "stale-orphan-history-sidecar",
        };
        foreach (var (path, content) in staleAuthoritativeFiles)
            local.Seed(path, content);
        var rejectedFiles = new Dictionary<string, string>
        {
            [SharedProfilePath] = "{\"shared\":\"keep\"}",
            [$"{SharedProfilePath}.backup"] = "keep-root-sidecar",
            [SettingsPath] = "{\"settings\":\"keep\"}",
            [RejectedArbitraryPath] = "keep-arbitrary",
            [RejectedBackupPath] = "keep-arbitrary-backup",
            [RejectedHistoryBakPath] = "keep-history-bak",
        };
        foreach (var (path, content) in rejectedFiles)
            local.Seed(path, content);
        cloud.Seed(ProgressPath, NewProgress);
        cloud.Seed($"{ProgressPath}.backup", "not-authoritative-source");
        SeedMarker(cloud, context);

        var progress = Progress(CloudOperationKind.Pull);
        var result = await PullAsync(local, cloud, progress).ConfigureAwait(false);

        ExpectSuccessfulResult(result, progress, CloudOperationKind.Pull);
        foreach (var (path, content) in staleAuthoritativeFiles)
        {
            ExpectMissing(local, path);
            var backup = FindBackupPath(local, path);
            ExpectContent(local, backup, content);
            ExpectBefore(
                trace,
                $"local:write:{backup}",
                $"local:delete-async:start:{path}",
                $"Pull tombstone backup must finish before deleting {path}"
            );
        }
        foreach (var (path, content) in rejectedFiles)
            ExpectContent(local, path, content);
        ExpectContent(
            cloud,
            $"{ProgressPath}.backup",
            "not-authoritative-source"
        );
        ExpectBefore(
            trace,
            $"local:delete-async:complete:{Profile2HistoryPath}",
            $"local:delete-async:start:{Profile2HistoryPath}.backup",
            "Pull must clean a destination-only history sidecar after its primary"
        );
        ExpectBefore(
            trace,
            $"local:delete-async:complete:{Profile2HistoryPath}",
            $"local:delete-async:start:{OrphanHistoryBackupPath}",
            "Pull must clean orphan allowed history sidecars after history primaries"
        );
    }

    private static async Task TombstoneDeleteFailureAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        var context = VanillaContext();
        local.Seed(ProgressPath, NewProgress);
        cloud.Seed(CurrentRunPath, "{\"run\":\"protected\"}");
        SeedMarker(cloud, context);
        cloud.DeleteFailure = path => path.Equals(
            CurrentRunPath,
            StringComparison.OrdinalIgnoreCase
        )
            ? new IOException("Injected remote deletion failure.")
            : null;
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "deletion failure");
        ExpectContent(cloud, CurrentRunPath, "{\"run\":\"protected\"}");
        ExpectMissing(cloud, context.MarkerPath);
        ExpectContent(
            local,
            FindBackupPath(local, CurrentRunPath),
            "{\"run\":\"protected\"}"
        );
    }

    private static async Task HistoryListingFailureAsync()
    {
        var pushLocal = new InMemoryCloudSaveStore("local");
        var pushCloud = new InMemoryCloudSaveStore("cloud");
        pushLocal.Seed(ProgressPath, NewProgress);
        pushCloud.Seed(Profile2HistoryPath, "{\"run\":\"unchanged\"}");
        pushCloud.ListFilesFailure = directory => directory.Equals(
            "profile2/saves/history",
            StringComparison.OrdinalIgnoreCase
        )
            ? new IOException("Injected Push history listing failure.")
            : null;
        var pushProgress = Progress(CloudOperationKind.Push);

        var pushFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            pushProgress,
            () => PushAsync(pushLocal, pushCloud, pushProgress)
        ).ConfigureAwait(false);

        ExpectContains(pushFailure.Message, "Push history listing failure");
        ExpectNoDestinationMutation(
            pushLocal,
            pushCloud,
            "Push destination history listing failure"
        );
        ExpectContent(pushCloud, Profile2HistoryPath, "{\"run\":\"unchanged\"}");
        ExpectMissing(pushCloud, VanillaContext().MarkerPath);
        Expect(
            !pushLocal.Paths.Any(IsTransferBackupPath),
            "Push history listing failure created a backup session."
        );

        var pullLocal = new InMemoryCloudSaveStore("local");
        var pullCloud = new InMemoryCloudSaveStore("cloud");
        var context = VanillaContext();
        pullLocal.Seed(ProgressPath, OldProgress);
        pullCloud.Seed(ProgressPath, NewProgress);
        pullCloud.Seed(Profile2HistoryPath, "{\"run\":\"unread\"}");
        SeedMarker(pullCloud, context);
        pullCloud.ListFilesFailure = directory => directory.Equals(
            "profile2/saves/history",
            StringComparison.OrdinalIgnoreCase
        )
            ? new IOException("Injected Pull history listing failure.")
            : null;
        var pullProgress = Progress(CloudOperationKind.Pull);

        var pullFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Pull,
            pullProgress,
            () => PullAsync(pullLocal, pullCloud, pullProgress)
        ).ConfigureAwait(false);

        ExpectContains(pullFailure.Message, "Pull history listing failure");
        ExpectNoDestinationMutation(
            pullLocal,
            pullCloud,
            "Pull source history listing failure"
        );
        ExpectContent(pullLocal, ProgressPath, OldProgress);
        ExpectMissing(pullLocal, VanillaIncompletePullPath);
        Expect(
            !pullLocal.Paths.Any(IsTransferBackupPath),
            "Pull history listing failure created a backup session."
        );
    }

    private static async Task DestinationDriftBeforeMutationAsync()
    {
        var createLocal = new InMemoryCloudSaveStore("local");
        var createCloud = new InMemoryCloudSaveStore("cloud");
        createLocal.Seed(ProgressPath, NewProgress);
        var createChecks = 0;
        createCloud.BeforeVerificationExistsAsync = (path, _) =>
        {
            if (
                path.Equals(ProgressPath, StringComparison.OrdinalIgnoreCase)
                && ++createChecks == 2
            )
            {
                createCloud.Seed(
                    ProgressPath,
                    "{\"progress\":\"created-concurrently\"}"
                );
            }
            return Task.CompletedTask;
        };
        var createProgress = Progress(CloudOperationKind.Push);

        var createFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            createProgress,
            () => PushAsync(createLocal, createCloud, createProgress)
        ).ConfigureAwait(false);

        ExpectContains(createFailure.Message, "Destination drift");
        ExpectContent(
            createCloud,
            ProgressPath,
            "{\"progress\":\"created-concurrently\"}"
        );
        ExpectMissing(createCloud, VanillaContext().MarkerPath);
        Expect(
            createCloud.WriteCount == 0 && createCloud.DeleteCount == 0,
            "Concurrent destination creation was overwritten before drift failure."
        );

        var changeLocal = new InMemoryCloudSaveStore("local");
        var changeCloud = new InMemoryCloudSaveStore("cloud");
        changeLocal.Seed(ProgressPath, NewProgress);
        changeCloud.Seed(ProgressPath, OldProgress);
        var changeChecks = 0;
        changeCloud.BeforeVerificationExistsAsync = (path, _) =>
        {
            if (
                path.Equals(ProgressPath, StringComparison.OrdinalIgnoreCase)
                && ++changeChecks == 2
            )
            {
                changeCloud.Seed(
                    ProgressPath,
                    "{\"progress\":\"changed-concurrently\"}"
                );
            }
            return Task.CompletedTask;
        };
        var changeProgress = Progress(CloudOperationKind.Push);

        var changeFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            changeProgress,
            () => PushAsync(changeLocal, changeCloud, changeProgress)
        ).ConfigureAwait(false);

        ExpectContains(changeFailure.Message, "Destination drift");
        ExpectContent(
            changeCloud,
            ProgressPath,
            "{\"progress\":\"changed-concurrently\"}"
        );
        ExpectContent(
            changeLocal,
            FindBackupPath(changeLocal, ProgressPath),
            OldProgress
        );
        Expect(
            changeCloud.WriteCount == 0 && changeCloud.DeleteCount == 0,
            "Concurrent destination change was overwritten after backup."
        );
    }

    private static async Task ContextDriftBeforeInvalidationAsync()
    {
        var context = VanillaContext();
        var driftedContext = VanillaContext(runtime: PublicBeta);
        var changeLocal = new InMemoryCloudSaveStore("local");
        var changeCloud = new InMemoryCloudSaveStore("cloud");
        changeLocal.Seed(ProgressPath, NewProgress);
        changeCloud.Seed(ProgressPath, OldProgress);
        SeedMarker(changeCloud, context);
        var markerChecks = 0;
        changeCloud.BeforeVerificationExistsAsync = (path, _) =>
        {
            if (
                path.Equals(context.MarkerPath, StringComparison.OrdinalIgnoreCase)
                && ++markerChecks == 3
            )
            {
                changeCloud.Seed(
                    context.MarkerPath,
                    driftedContext.SerializeMarker()
                );
            }
            return Task.CompletedTask;
        };
        var changeProgress = Progress(CloudOperationKind.Push);

        var changeFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            changeProgress,
            () => PushAsync(changeLocal, changeCloud, changeProgress)
        ).ConfigureAwait(false);

        ExpectContains(changeFailure.Message, "Steam save context drift");
        ExpectContent(
            changeCloud,
            context.MarkerPath,
            driftedContext.SerializeMarker()
        );
        ExpectContent(changeCloud, ProgressPath, OldProgress);
        ExpectContent(
            changeLocal,
            FindBackupPath(changeLocal, context.MarkerPath),
            context.SerializeMarker()
        );
        Expect(
            changeCloud.WriteCount == 0 && changeCloud.DeleteCount == 0,
            "Changed context marker was invalidated despite baseline drift."
        );

        var createLocal = new InMemoryCloudSaveStore("local");
        var createCloud = new InMemoryCloudSaveStore("cloud");
        createLocal.Seed(ProgressPath, NewProgress);
        createCloud.Seed(ProgressPath, OldProgress);
        var missingMarkerChecks = 0;
        createCloud.BeforeVerificationExistsAsync = (path, _) =>
        {
            if (
                path.Equals(context.MarkerPath, StringComparison.OrdinalIgnoreCase)
                && ++missingMarkerChecks == 2
            )
            {
                createCloud.Seed(
                    context.MarkerPath,
                    driftedContext.SerializeMarker()
                );
            }
            return Task.CompletedTask;
        };
        var createProgress = Progress(CloudOperationKind.Push);

        var createFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            createProgress,
            () => PushAsync(createLocal, createCloud, createProgress)
        ).ConfigureAwait(false);

        ExpectContains(createFailure.Message, "Steam save context drift");
        ExpectContent(
            createCloud,
            context.MarkerPath,
            driftedContext.SerializeMarker()
        );
        ExpectContent(
            createLocal,
            FindBackupPath(createLocal, context.MarkerPath),
            driftedContext.SerializeMarker()
        );
        Expect(
            createCloud.WriteCount == 0 && createCloud.DeleteCount == 0,
            "New context marker was overwritten after the initial missing check."
        );
    }

    private static async Task ConcurrentMarkerReplacementSurvivesCleanupAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        var context = VanillaContext();
        var replacementContext = VanillaContext(runtime: PublicBeta);
        local.Seed(ProgressPath, NewProgress);
        var replaced = false;
        cloud.BeforeVerificationReadAsync = (path, _) =>
        {
            if (
                !replaced
                && path.Equals(
                    context.MarkerPath,
                    StringComparison.OrdinalIgnoreCase
                )
                && cloud.WriteCountFor(path) > 0
            )
            {
                replaced = true;
                cloud.Seed(
                    context.MarkerPath,
                    replacementContext.SerializeMarker()
                );
            }

            return Task.CompletedTask;
        };
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "hash mismatch");
        Expect(replaced, "The fake store did not replace the context marker.");
        ExpectContent(
            cloud,
            context.MarkerPath,
            replacementContext.SerializeMarker()
        );
        ExpectContent(cloud, ProgressPath, NewProgress);
        Expect(
            cloud.DeleteCount == 0,
            "Failed cleanup deleted a context marker written by another transfer."
        );
    }

    private static async Task LateFinalDriftAsync()
    {
        var pushLocal = new InMemoryCloudSaveStore("local");
        var pushCloud = new InMemoryCloudSaveStore("cloud");
        pushLocal.Seed(ProgressPath, NewProgress);
        pushCloud.Seed(ProgressPath, OldProgress);
        var pushMetadataCount = 0;
        pushCloud.BeforeMetadataAsync = _ =>
        {
            if (++pushMetadataCount == 2)
            {
                pushCloud.Seed(
                    ProgressPath,
                    "{\"progress\":\"changed-after-write\"}"
                );
            }
            return Task.CompletedTask;
        };
        var pushProgress = Progress(CloudOperationKind.Push);

        var pushFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            pushProgress,
            () => PushAsync(pushLocal, pushCloud, pushProgress)
        ).ConfigureAwait(false);

        ExpectContains(pushFailure.Message, "Final destination drift");
        ExpectContent(
            pushCloud,
            ProgressPath,
            "{\"progress\":\"changed-after-write\"}"
        );
        ExpectMissing(pushCloud, VanillaContext().MarkerPath);
        Expect(
            pushCloud.DeleteCount == 0,
            "Final mirror verification wrote or invalidated a context marker before failing."
        );

        var historyLocal = new InMemoryCloudSaveStore("local");
        var historyCloud = new InMemoryCloudSaveStore("cloud");
        historyLocal.Seed(ProgressPath, NewProgress);
        var historyMetadataCount = 0;
        historyCloud.BeforeMetadataAsync = _ =>
        {
            if (++historyMetadataCount == 2)
            {
                historyCloud.Seed(
                    OrphanHistoryBackupPath,
                    "late-unexpected-history-sidecar"
                );
            }
            return Task.CompletedTask;
        };
        var historyProgress = Progress(CloudOperationKind.Push);

        var historyFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            historyProgress,
            () => PushAsync(historyLocal, historyCloud, historyProgress)
        ).ConfigureAwait(false);

        ExpectContains(historyFailure.Message, "destination history drift");
        ExpectContent(
            historyCloud,
            OrphanHistoryBackupPath,
            "late-unexpected-history-sidecar"
        );
        ExpectMissing(historyCloud, VanillaContext().MarkerPath);

        var pullLocal = new InMemoryCloudSaveStore("local");
        var pullCloud = new InMemoryCloudSaveStore("cloud");
        var context = VanillaContext();
        pullLocal.Seed(ProgressPath, OldProgress);
        pullCloud.Seed(ProgressPath, NewProgress);
        SeedMarker(pullCloud, context);
        var pullMetadataCount = 0;
        pullCloud.BeforeMetadataAsync = _ =>
        {
            if (++pullMetadataCount == 2)
            {
                pullCloud.Seed(
                    ProgressPath,
                    "{\"progress\":\"source-changed-late\"}"
                );
            }
            return Task.CompletedTask;
        };
        var pullProgress = Progress(CloudOperationKind.Pull);

        var pullFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Pull,
            pullProgress,
            () => PullAsync(pullLocal, pullCloud, pullProgress)
        ).ConfigureAwait(false);

        ExpectContains(pullFailure.Message, "Steam source drift");
        ExpectContent(pullLocal, ProgressPath, NewProgress);
        ExpectContent(
            pullLocal,
            VanillaIncompletePullPath,
            IncompletePullMarkerContent
        );
        ExpectContent(pullCloud, context.MarkerPath, context.SerializeMarker());
        Expect(
            pullCloud.WriteCount == 0 && pullCloud.DeleteCount == 0,
            "Pull source revalidation mutated Steam Cloud."
        );
    }

    private static async Task AccountMismatchAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud", SteamId64);
        local.Seed(ProgressPath, NewProgress);
        SeedMarker(cloud, VanillaContext(OtherSteamId64));
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "Steam account");
        ExpectNoDestinationMutation(local, cloud, "account mismatch");
    }

    private static async Task NamespaceMismatchAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        local.Seed(ModdedProgressPath, NewProgress);
        var selected = ModdedContext(ModFingerprintA);
        cloud.Seed(selected.MarkerPath, VanillaContext().SerializeMarker());
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(
                local,
                cloud,
                progress,
                SaveNamespace.Modded,
                PublicBeta,
                ModFingerprintA
            )
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "save namespace");
        ExpectNoDestinationMutation(local, cloud, "namespace mismatch");
    }

    private static async Task RuntimeMismatchAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        local.Seed(ProgressPath, NewProgress);
        SeedMarker(cloud, VanillaContext(runtime: SteamGameBranch.Public));
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(
                local,
                cloud,
                progress,
                SaveNamespace.Vanilla,
                PublicBeta,
                ""
            )
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "runtime compatibility/branch");
        ExpectNoDestinationMutation(local, cloud, "runtime mismatch");
    }

    private static async Task ModSetMismatchAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        local.Seed(ModdedProgressPath, NewProgress);
        SeedMarker(cloud, ModdedContext(ModFingerprintA));
        var progress = Progress(CloudOperationKind.Push);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(
                local,
                cloud,
                progress,
                SaveNamespace.Modded,
                PublicBeta,
                ModFingerprintB
            )
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "mod set");
        ExpectNoDestinationMutation(local, cloud, "mod-set mismatch");
    }

    private static async Task FailedPullLeavesIncompleteMarkerAsync()
    {
        var trace = new ConcurrentQueue<string>();
        var local = Store("local", trace);
        var cloud = Store("cloud", trace);
        var context = VanillaContext();
        local.Seed(ProgressPath, OldProgress);
        cloud.Seed(ProgressPath, NewProgress);
        SeedMarker(cloud, context);
        local.WriteFailure = path => path.Equals(
            ProgressPath,
            StringComparison.OrdinalIgnoreCase
        )
            ? new IOException("Injected local destination write failure.")
            : null;
        var progress = Progress(CloudOperationKind.Pull);

        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Pull,
            progress,
            () => PullAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "destination write failure");
        ExpectContent(
            local,
            VanillaIncompletePullPath,
            IncompletePullMarkerContent
        );
        var progressBackup = FindBackupPath(local, ProgressPath);
        ExpectContent(local, progressBackup, OldProgress);
        ExpectBefore(
            trace,
            $"local:write:{progressBackup}",
            $"local:write-async:start:{VanillaIncompletePullPath}",
            "Failed Pull must finish data backup before marking itself incomplete"
        );
        ExpectBefore(
            trace,
            $"local:read-async:{VanillaIncompletePullPath}",
            $"local:raw-write:start:{ProgressPath}",
            "Failed Pull must verify its incomplete marker before data mutation"
        );
        Expect(
            cloud.WriteCount == 0 && cloud.DeleteCount == 0,
            "Failed Pull mutated its Steam source."
        );

        await ExpectIncompletePullBlocksPushAsync(
            local,
            cloud,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            "vanilla"
        ).ConfigureAwait(false);
        await ExpectIncompletePullBlocksPushAsync(
            local,
            cloud,
            SaveNamespace.Modded,
            PublicBeta,
            ModFingerprintA,
            "vanilla"
        ).ConfigureAwait(false);
        ExpectContent(
            local,
            VanillaIncompletePullPath,
            IncompletePullMarkerContent
        );
        ExpectContent(cloud, context.MarkerPath, context.SerializeMarker());

        var moddedIncompleteLocal = new InMemoryCloudSaveStore("local");
        var untouchedCloud = new InMemoryCloudSaveStore("cloud");
        moddedIncompleteLocal.Seed(ProgressPath, NewProgress);
        moddedIncompleteLocal.Seed(
            ModdedIncompletePullPath,
            IncompletePullMarkerContent
        );
        await ExpectIncompletePullBlocksPushAsync(
            moddedIncompleteLocal,
            untouchedCloud,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            "modded"
        ).ConfigureAwait(false);
    }

    private static async Task ExpectIncompletePullBlocksPushAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        SaveNamespace saveNamespace,
        string runtime,
        string modFingerprint,
        string incompleteNamespace
    )
    {
        var localWritesBeforePush = local.WriteCount;
        var localDeletesBeforePush = local.DeleteCount;
        var cloudWritesBeforePush = cloud.WriteCount;
        var cloudDeletesBeforePush = cloud.DeleteCount;
        var progress = Progress(CloudOperationKind.Push);
        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(
                local,
                cloud,
                progress,
                saveNamespace,
                runtime,
                modFingerprint
            )
        ).ConfigureAwait(false);

        ExpectContains(
            failure.Message,
            $"previous {incompleteNamespace} Pull did not complete"
        );
        Expect(
            local.WriteCount == localWritesBeforePush
                && local.DeleteCount == localDeletesBeforePush
                && cloud.WriteCount == cloudWritesBeforePush
                && cloud.DeleteCount == cloudDeletesBeforePush,
            $"{saveNamespace} Push mutated state despite a persisted "
                + $"{incompleteNamespace} Pull marker."
        );
    }

    private static async Task SharedRootProfileAloneIsRejectedAsync()
    {
        var local = new InMemoryCloudSaveStore("local");
        var cloud = new InMemoryCloudSaveStore("cloud");
        local.Seed("profile.save", "{\"sharedProfileOnly\":true}");

        Expect(
            !SaveTransferAllowlist.HasAnyContent(
                local,
                SaveNamespace.Vanilla
            ),
            "Shared root profile.save alone made Vanilla Push eligible."
        );
        Expect(
            !SaveTransferAllowlist.HasAnyContent(
                local,
                SaveNamespace.Modded
            ),
            "Shared root profile.save alone made Modded Push eligible."
        );

        var progress = Progress(CloudOperationKind.Push);
        var failure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            progress,
            () => PushAsync(local, cloud, progress)
        ).ConfigureAwait(false);

        ExpectContains(failure.Message, "profile save files");
        ExpectNoDestinationMutation(
            local,
            cloud,
            "shared root profile.save-only Push"
        );
    }

    private static async Task EmptyAllowlistedSourcesFailBeforeMutationAsync()
    {
        var context = VanillaContext();
        var pushLocal = new InMemoryCloudSaveStore("local");
        var pushCloud = new InMemoryCloudSaveStore("cloud");
        pushLocal.Seed(ProgressPath, "  \r\n\t");
        pushCloud.Seed(ProgressPath, OldProgress);
        SeedMarker(pushCloud, context);
        var pushProgress = Progress(CloudOperationKind.Push);

        var pushFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Push,
            pushProgress,
            () => PushAsync(pushLocal, pushCloud, pushProgress)
        ).ConfigureAwait(false);

        ExpectContains(pushFailure.Message, "Source save file is empty");
        ExpectContains(pushFailure.Message, ProgressPath);
        ExpectNoDestinationMutation(
            pushLocal,
            pushCloud,
            "whitespace-source Push"
        );
        ExpectContent(pushCloud, ProgressPath, OldProgress);
        ExpectContent(pushCloud, context.MarkerPath, context.SerializeMarker());
        Expect(
            !pushLocal.Paths.Any(IsTransferBackupPath),
            "Whitespace-source Push created a backup session during snapshot."
        );

        var pullLocal = new InMemoryCloudSaveStore("local");
        var pullCloud = new InMemoryCloudSaveStore("cloud");
        pullLocal.Seed(ProgressPath, OldProgress);
        pullCloud.Seed(ProgressPath, "");
        SeedMarker(pullCloud, context);
        var pullProgress = Progress(CloudOperationKind.Pull);

        var pullFailure = await ExpectTransferFailureAsync(
            CloudOperationKind.Pull,
            pullProgress,
            () => PullAsync(pullLocal, pullCloud, pullProgress)
        ).ConfigureAwait(false);

        ExpectContains(pullFailure.Message, "Source save file is empty");
        ExpectContains(pullFailure.Message, ProgressPath);
        ExpectNoDestinationMutation(
            pullLocal,
            pullCloud,
            "empty-source Pull"
        );
        ExpectContent(pullLocal, ProgressPath, OldProgress);
        ExpectContent(pullCloud, ProgressPath, "");
        ExpectContent(pullCloud, context.MarkerPath, context.SerializeMarker());
        ExpectMissing(pullLocal, VanillaIncompletePullPath);
        Expect(
            !pullLocal.Paths.Any(IsTransferBackupPath),
            "Empty-source Pull created a backup session during snapshot."
        );
    }

    private static Task<ManualCloudSyncResult> PushAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        CloudOperationProgressTracker progress,
        SaveNamespace saveNamespace = SaveNamespace.Vanilla,
        string runtime = SteamGameBranch.Public,
        string modFingerprint = ""
    )
        => CloudSyncCoordinator.ManualPushAllAsync(
            local,
            cloud,
            saveNamespace,
            runtime,
            modFingerprint,
            progress,
            CancellationToken.None
        );

    private static Task<ManualCloudSyncResult> PullAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        CloudOperationProgressTracker progress,
        SaveNamespace saveNamespace = SaveNamespace.Vanilla,
        string runtime = SteamGameBranch.Public,
        string modFingerprint = ""
    )
        => CloudSyncCoordinator.ManualPullAllAsync(
            local,
            cloud,
            saveNamespace,
            runtime,
            modFingerprint,
            progress,
            CancellationToken.None
        );

    private static async Task<Exception> ExpectTransferFailureAsync(
        CloudOperationKind kind,
        CloudOperationProgressTracker progress,
        Func<Task<ManualCloudSyncResult>> run
    )
    {
        ManualCloudSyncResult? unexpectedResult = null;
        Exception? failure = null;
        try
        {
            unexpectedResult = await run().WaitAsync(
                TimeSpan.FromSeconds(15)
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        Expect(failure is not null, "Injected transfer failure did not propagate.");
        Expect(!unexpectedResult.HasValue, "Failed transfer returned a result.");
        Expect(
            progress.State.Phase == CloudOperationPhase.Failed
                && progress.State.IsTerminal,
            $"Failed transfer ended in {progress.State.Phase}, not Failed."
        );

        var livePresentation = CloudOperationPresentation.Create(progress.State);
        var terminal = CloudOperationTerminalPresentation.CreateFailure(
            kind == CloudOperationKind.Pull ? "Pull" : "Upload",
            progress.State,
            failure!.Message
        );
        Expect(livePresentation.IsFailed && !livePresentation.IsComplete, "Failure progress presentation claimed completion.");
        Expect(
            terminal.Outcome == CloudOperationTerminalOutcome.Failure,
            "Failure terminal presentation did not report Failure."
        );
        ExpectNoSuccessClaim(
            $"{livePresentation.PhaseText} {livePresentation.DetailText} "
                + $"{terminal.StatusText} {terminal.LogText}"
        );
        return failure;
    }

    private static void ExpectSuccessfulResult(
        ManualCloudSyncResult result,
        CloudOperationProgressTracker progress,
        CloudOperationKind kind
    )
    {
        Expect(result.Kind == kind, "Transfer result direction changed.");
        Expect(
            result.TransferredPathCount > 0
                && !string.IsNullOrWhiteSpace(result.Detail),
            "Verified transfer returned an empty result."
        );
        Expect(
            result.TransferredPathCount
                == progress.State.TransferCompletedCount
                && result.TransferredPathCount
                    == progress.State.TransferTotalCount,
            "Successful transfer result disagrees with verified progress."
        );
        Expect(
            result.BackupCreatedCount == progress.State.BackupCreatedCount,
            "Successful transfer result disagrees with backup progress."
        );
        Expect(
            progress.State.Phase == CloudOperationPhase.Completed
                && progress.State.IsTerminal,
            "Successful transfer did not finish in Completed progress."
        );
    }

    private static void ExpectNoDestinationMutation(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        string scenario
    )
    {
        Expect(
            local.WriteCount == 0
                && local.DeleteCount == 0
                && cloud.WriteCount == 0
                && cloud.DeleteCount == 0,
            $"{scenario} allowed a destination or backup mutation."
        );
    }

    private static SaveContext VanillaContext(
        ulong steamId64 = SteamId64,
        string runtime = SteamGameBranch.Public
    )
        => SaveContext.Create(
            steamId64,
            SaveNamespace.Vanilla,
            runtime,
            ""
        );

    private static SaveContext ModdedContext(
        string fingerprint,
        ulong steamId64 = SteamId64,
        string runtime = PublicBeta
    )
        => SaveContext.Create(
            steamId64,
            SaveNamespace.Modded,
            runtime,
            fingerprint
        );

    private static void SeedMarker(
        InMemoryCloudSaveStore cloud,
        SaveContext context
    )
        => cloud.Seed(context.MarkerPath, context.SerializeMarker());

    private static InMemoryCloudSaveStore Store(
        string name,
        ConcurrentQueue<string> trace
    )
        => new(name, SteamId64, trace.Enqueue);

    private static CloudOperationProgressTracker Progress(
        CloudOperationKind kind
    )
        => new(kind);

    private static string FindBackupPath(
        InMemoryCloudSaveStore local,
        string destinationPath
    )
    {
        var suffix = $"/files/{Canonical(destinationPath)}";
        var matches = local.Paths
            .Where(path => IsTransferBackupPath(path))
            .Where(path => path.EndsWith(
                suffix,
                StringComparison.OrdinalIgnoreCase
            ))
            .ToArray();
        Expect(
            matches.Length == 1,
            $"Expected one mandatory backup for {destinationPath}, found {matches.Length}."
        );
        return matches[0];
    }

    private static bool IsTransferBackupPath(string path)
        => Canonical(path).StartsWith(
            ".sts2-launcher/transfer-backups/",
            StringComparison.OrdinalIgnoreCase
        );

    private static void ExpectBefore(
        ConcurrentQueue<string> trace,
        string first,
        string second,
        string message
    )
    {
        var operations = trace.ToArray();
        var firstIndex = Array.FindIndex(
            operations,
            operation => operation.Equals(first, StringComparison.OrdinalIgnoreCase)
        );
        var secondIndex = Array.FindIndex(
            operations,
            operation => operation.Equals(second, StringComparison.OrdinalIgnoreCase)
        );
        Expect(
            firstIndex >= 0 && secondIndex >= 0 && firstIndex < secondIndex,
            $"{message}. first={firstIndex}; second={secondIndex}."
        );
    }

    private static void ExpectContent(
        InMemoryCloudSaveStore store,
        string path,
        string expected
    )
        => Expect(
            store.TryReadSeeded(path, out var actual)
                && string.Equals(actual, expected, StringComparison.Ordinal),
            $"Expected exact content at {path}."
        );

    private static void ExpectBytes(
        InMemoryCloudSaveStore store,
        string path,
        byte[] expected
    )
        => Expect(
            store.TryReadSeededBytes(path, out var actual)
                && actual.AsSpan().SequenceEqual(expected),
            $"Expected exact bytes at {path}."
        );

    private static byte[] RawTransferFixture(string content)
        => Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(content + "\r\n"))
            .Concat(new byte[] { 0 })
            .ToArray();

    private static void ExpectMissing(
        InMemoryCloudSaveStore store,
        string path
    )
        => Expect(!store.Contains(path), $"Expected {path} to be absent.");

    private static void ExpectNoSuccessClaim(string text)
    {
        Expect(
            !text.Contains("synced", StringComparison.OrdinalIgnoreCase)
                && !text.Contains("succeeded", StringComparison.OrdinalIgnoreCase),
            $"Failure presentation made a success claim: {text}"
        );
    }

    private static void ExpectContains(string actual, string expected)
        => Expect(
            actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            $"Expected '{actual}' to contain '{expected}'."
        );

    private static string Canonical(string path)
        => path.Replace('\\', '/').Trim('/');

    private static async Task RunAsync(
        string name,
        Func<Task> test
    )
    {
        await test().ConfigureAwait(false);
        _passed++;
        Console.WriteLine($"PASS: {name}");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
