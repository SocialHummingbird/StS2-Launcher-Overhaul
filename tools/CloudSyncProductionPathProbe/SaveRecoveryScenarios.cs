#nullable enable

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal static class SaveRecoveryScenarios
{
    private const string ProfilePath = "profile.save";
    private const string ProgressPath = "profile1/saves/progress.save";
    private const string PrefsPath = "profile1/saves/prefs.save";
    private const string CurrentRunPath = "profile1/saves/current_run.save";
    private const string HistoryA = "profile1/saves/history/20260806.run";
    private const string HistoryB = "profile1/saves/history/20260805.run";
    private static int _passed;

    internal static async Task<int> RunAllAsync()
    {
        await RunAsync(
            "current-save export is byte-exact, local-only, and scan-independent",
            CurrentSaveExportIsByteExactAndLocalOnlyAsync
        );
        await RunAsync(
            "recovery Restore and Undo preserve raw bytes and tombstones",
            RestoreAndUndoAreByteExactAsync
        );
        await RunAsync(
            "case-fold history collision blocks recovery before mutation",
            CaseFoldHistoryCollisionBlocksBeforeMutationAsync
        );
        await RunAsync(
            "full Restore rejects source/local history case collision",
            FullSourceLocalHistoryCaseCollisionBlocksAsync
        );
        await RunAsync(
            "partial Restore rejects source/local history case collision",
            PartialSourceLocalHistoryCaseCollisionBlocksAsync
        );
        await RunAsync(
            "partial legacy restore never infers tombstones",
            PartialRestoreIsOverwriteOnlyAsync
        );
        await RunAsync(
            "partial Restore detects unrelated post-write drift",
            PartialRestorePostWriteDriftCannotBeUndoneAsync
        );
        await RunAsync(
            "post-restore drift blocks Undo",
            DriftBlocksUndoAsync
        );
        await RunAsync(
            "explicit approval re-verifies bytes and releases sync hold",
            ApprovalReleasesHoldAsync
        );
        await RunAsync(
            "failed restore remains held and Undo converges",
            InterruptedRestoreCanUndoAsync
        );
        await RunAsync(
            "Restore journal drift cannot be erased by a later Undo",
            RestoreJournalCommitDriftCannotBeUndoneAsync
        );
        await RunAsync(
            "Undo journal drift blocks both Undo and retry",
            UndoJournalCommitDriftBlocksRetryAsync
        );
        await RunAsync(
            "journal snapshot roles reject self-consistent cross-links",
            JournalSnapshotRoleCrossLinksFailClosedAsync
        );
        await RunAsync(
            "Undo snapshot must match the exact recovery context",
            UndoSnapshotContextMismatchFailsAsync
        );
        await RunAsync(
            "orphaned legacy sync hold no longer blocks sync",
            OrphanedLegacyHoldDoesNotBlockSyncAsync
        );
        await RunAsync(
            "quarantine and context mismatch block before mutation",
            QuarantineAndContextMismatchAsync
        );
        await RunAsync(
            "unknown recovery cannot inherit a supplied Steam account",
            UnknownRecoveryCannotInheritTargetAccountAsync
        );
        await RunAsync(
            "recovery binds exact snapshots and approval to the Steam account",
            AccountBindingAndUnknownProvenanceAsync
        );
        await RunAsync(
            "one authoritative journal fails closed across crash states",
            AuthoritativeJournalFailsClosedAsync
        );
        await RunAsync(
            "the core lease serializes sync and recovery mutations",
            CoreLeaseSerializesSyncAndRecoveryAsync
        );
        return _passed;
    }

    private static async Task CurrentSaveExportIsByteExactAndLocalOnlyAsync()
    {
        var local = new InMemoryCloudSaveStore("current-export-local");
        var exactBytes = SpecialBytes(
            "{\"ironclad\":10}\r\n\0"
        );
        var moddedBytes = SpecialBytes("{\"wrongNamespace\":true}");
        local.SeedBytes(ProfilePath, exactBytes);
        local.SeedBytes(ProgressPath, exactBytes);
        foreach (var historyName in new[]
        {
            "a", "i", "z", "\u00e4", "\u00e5", "\u0130", "\u0131",
        })
        {
            local.SeedBytes(
                $"profile1/saves/history/{historyName}.run",
                Encoding.UTF8.GetBytes($"history:{historyName}")
            );
        }
        local.SeedBytes("profile1/saves/STS2Modded/progress.save", moddedBytes);
        local.Seed(
            CloudSyncCoordinator.SaveRecoveryHoldPath,
            "{\"legacyHold\":true}"
        );

        var context = Context();
        var beforePath = CloudSyncCoordinator.AutomaticSyncSnapshotPath(
            context,
            new string('a', 64)
        );
        var pendingJson = JsonSerializer.Serialize(new
        {
            ContextMarker = context.SerializeMarker(),
            BeforeGameSnapshotPath = beforePath,
        });
        const string baselineJson = "{\"baseline\":\"raw\"}";
        const string beforeJson = "{\"beforeGame\":\"raw\"}";
        local.Seed(CloudSyncCoordinator.AutomaticSyncPendingPath, pendingJson);
        local.Seed(
            CloudSyncCoordinator.AutomaticSyncBaselinePath(context),
            baselineJson
        );
        local.Seed(beforePath, beforeJson);

        var rawWritesBefore = local.RawWriteCount;
        var recoveryWritesBefore = local.RecoveryWriteCount;
        var json = await CloudSyncCoordinator.BuildCurrentSaveExportBundleAsync(
            local,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            cloudSyncEnabled: false,
            CancellationToken.None
        ).ConfigureAwait(false);

        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidDataException("Current save export was empty");
        Expect(root["Version"]?.GetValue<int>() == 2);
        var exportId = root["ExportId"]?.GetValue<string>() ?? "";
        var currentTreeSha256 = root["CurrentAndroidTreeSha256"]
            ?.GetValue<string>() ?? "";
        var selectedContextSha256 = root["SelectedSaveContextSha256"]
            ?.GetValue<string>() ?? "";
        Expect(exportId.Length == 32 && exportId.All(Uri.IsHexDigit));
        Expect(currentTreeSha256.Length == 64
            && currentTreeSha256.All(Uri.IsHexDigit));
        Expect(
            currentTreeSha256
                == "a89a0a8151e69de8e8fff755ba3840cb1c7d3617de83bc7a800fd54834e8ec40",
            $"Canonical Unicode tree hash changed: {currentTreeSha256}"
        );
        Expect(selectedContextSha256.Length == 64
            && selectedContextSha256.All(Uri.IsHexDigit));
        Expect(root["SteamWasContacted"]?.GetValue<bool>() == false);
        Expect(root["OriginalSourcesWereModified"]?.GetValue<bool>() == false);
        Expect(root["CloudSyncEnabled"]?.GetValue<bool>() == false);
        Expect(root["SelectedCandidateId"]?.GetValue<string>() == "");
        Expect(root["RecoverySnapshots"]?.AsArray().Count == 0);
        Expect(root["ExportOnlyFiles"]?.AsArray().Count == 0);

        var selected = root["SelectedSaveContext"]?.AsObject()
            ?? throw new InvalidDataException("Selected save context was omitted");
        Expect(selected["SaveNamespace"]?.GetValue<string>() == "vanilla");
        Expect(selected["RuntimeIdentity"]?.GetValue<string>()
            == SteamGameBranch.Public);
        Expect(selected["ModSetFingerprint"]?.GetValue<string>() == "");
        Expect(selected["SteamId64"]?.GetValue<ulong>()
            == InMemoryCloudSaveStore.DefaultSteamId64);

        Expect(root["RecoveryHoldJson"]?.GetValue<string>()
            == "{\"legacyHold\":true}");
        Expect(root["AutomaticSyncPendingJson"]?.GetValue<string>()
            == pendingJson);
        Expect(root["AutomaticSyncBaselineJson"]?.GetValue<string>()
            == baselineJson);
        Expect(root["AutomaticSyncBeforeGameSnapshotJson"]?.GetValue<string>()
            == beforeJson);

        var snapshot = root["CurrentAndroidSnapshot"]?.AsObject()
            ?? throw new InvalidDataException("Current Android snapshot was omitted");
        Expect(snapshot["ContextMarker"]?.GetValue<string>()
            == context.SerializeMarker());
        var files = snapshot["Files"]?.AsArray()
            ?? throw new InvalidDataException("Current save bytes were omitted");
        var progress = files
            .Select(node => node?.AsObject())
            .Single(file => file?["Path"]?.GetValue<string>() == ProgressPath)
            ?? throw new InvalidDataException("Progress bytes were omitted");
        var exportedBytes = Convert.FromBase64String(
            progress["ContentBase64"]?.GetValue<string>() ?? ""
        );
        Expect(exportedBytes.AsSpan().SequenceEqual(exactBytes));
        Expect(progress["ByteSha256"]?.GetValue<string>()
            == AutomaticSyncHash.Compute(exactBytes));
        Expect(!files.Any(node => node?["Path"]?.GetValue<string>()
            ?.Contains("STS2Modded", StringComparison.Ordinal) == true));
        Expect(local.RawWriteCount == rawWritesBefore);
        Expect(local.RecoveryWriteCount == recoveryWritesBefore);

        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            $"sts2-current-export-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(outputRoot);
        var completionLogs = new List<string>();
        void CaptureCompletionLog(string message)
        {
            if (message.StartsWith(
                    "[Recovery] STS2_SAVE_EXPORT_COMPLETE ",
                    StringComparison.Ordinal
                ))
            {
                completionLogs.Add(message);
            }
        }
        PatchHelper.LogEmitted += CaptureCompletionLog;
        try
        {
            var path = LauncherDiagnostics.WriteSaveRecoveryBundle(
                json,
                outputRoot
            );
            Expect(File.Exists(path));
            Expect(string.Equals(
                await File.ReadAllTextAsync(path).ConfigureAwait(false),
                json,
                StringComparison.Ordinal
            ));
            Expect(completionLogs.Count == 1);
            ExpectExportCompletionLog(
                completionLogs.Single(),
                path,
                exportId,
                currentTreeSha256,
                selectedContextSha256
            );
            completionLogs.Clear();
            var secondPath = LauncherDiagnostics.WriteSaveRecoveryBundle(
                json: json,
                fallbackDirectory: outputRoot
            );
            Expect(!string.Equals(path, secondPath, StringComparison.Ordinal));
            Expect(File.Exists(path) && File.Exists(secondPath));
            Expect(string.Equals(
                await File.ReadAllTextAsync(path).ConfigureAwait(false),
                json,
                StringComparison.Ordinal
            ));
            Expect(completionLogs.Count == 1);
            ExpectExportCompletionLog(
                completionLogs.Single(),
                secondPath,
                exportId,
                currentTreeSha256,
                selectedContextSha256
            );
        }
        finally
        {
            PatchHelper.LogEmitted -= CaptureCompletionLog;
            Directory.Delete(outputRoot, recursive: true);
        }

        var unknown = new InMemoryCloudSaveStore("current-export-unknown");
        unknown.SeedBytes(ProgressPath, exactBytes);
        const string unreadablePending = "{not-json";
        unknown.Seed(
            CloudSyncCoordinator.AutomaticSyncPendingPath,
            unreadablePending
        );
        var unknownJson = await CloudSyncCoordinator
            .BuildCurrentSaveExportBundleAsync(
                unknown,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                cloudSyncEnabled: true,
                CancellationToken.None
            ).ConfigureAwait(false);
        var unknownRoot = JsonNode.Parse(unknownJson)?.AsObject()
            ?? throw new InvalidDataException("Unknown-context export was empty");
        Expect(unknownRoot["SelectedSaveContext"]?["SteamId64"]
            ?.GetValue<ulong>() == 0);
        Expect(unknownRoot["CloudSyncEnabled"]?.GetValue<bool>() == true);
        Expect(unknownRoot["AutomaticSyncPendingJson"]?.GetValue<string>()
            == unreadablePending);
        Expect(unknownRoot["AutomaticSyncBaselineJson"]?.GetValue<string>()
            == "");

        var established = new InMemoryCloudSaveStore(
            "current-export-established"
        );
        established.SeedBytes(ProgressPath, exactBytes);
        var establishedBaseline = JsonSerializer.Serialize(new
        {
            ContextMarker = context.SerializeMarker(),
            Established = true,
        });
        established.Seed(
            CloudSyncCoordinator.AutomaticSyncBaselinePath(context),
            establishedBaseline
        );
        var establishedJson = await CloudSyncCoordinator
            .BuildCurrentSaveExportBundleAsync(
                established,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                cloudSyncEnabled: true,
                CancellationToken.None
            ).ConfigureAwait(false);
        var establishedRoot = JsonNode.Parse(establishedJson)?.AsObject()
            ?? throw new InvalidDataException("Established-context export was empty");
        Expect(establishedRoot["SelectedSaveContext"]?["SteamId64"]
            ?.GetValue<ulong>()
            == InMemoryCloudSaveStore.DefaultSteamId64);
        Expect(establishedRoot["AutomaticSyncBaselineJson"]?.GetValue<string>()
            == establishedBaseline);

        var otherContext = SaveContext.Create(
            InMemoryCloudSaveStore.DefaultSteamId64 + 1,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            ""
        );
        established.Seed(
            CloudSyncCoordinator.AutomaticSyncBaselinePath(otherContext),
            JsonSerializer.Serialize(new
            {
                ContextMarker = otherContext.SerializeMarker(),
                Established = true,
            })
        );
        var ambiguousJson = await CloudSyncCoordinator
            .BuildCurrentSaveExportBundleAsync(
                established,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                cloudSyncEnabled: true,
                CancellationToken.None
            ).ConfigureAwait(false);
        var ambiguousRoot = JsonNode.Parse(ambiguousJson)?.AsObject()
            ?? throw new InvalidDataException("Ambiguous-context export was empty");
        Expect(ambiguousRoot["SelectedSaveContext"]?["SteamId64"]
            ?.GetValue<ulong>() == 0);
        Expect(ambiguousRoot["AutomaticSyncBaselineJson"]?.GetValue<string>()
            == "");

        await CurrentSaveExportIncludesVerifiedRollbackSnapshotsAsync()
            .ConfigureAwait(false);
    }

    private static void ExpectExportCompletionLog(
        string line,
        string bundlePath,
        string exportId,
        string currentTreeSha256,
        string selectedContextSha256
    )
    {
        const string prefix = "[Recovery] STS2_SAVE_EXPORT_COMPLETE ";
        var payload = JsonNode.Parse(line[prefix.Length..])?.AsObject()
            ?? throw new InvalidDataException(
                "Save export completion log was not structured JSON"
            );
        Expect(payload.Count == 6);
        Expect(payload["Event"]?.GetValue<string>()
            == "save-recovery-export-complete");
        Expect(payload["Version"]?.GetValue<int>() == 1);
        Expect(payload["ExportId"]?.GetValue<string>() == exportId);
        Expect(payload["BundleSha256"]?.GetValue<string>()
            == AutomaticSyncHash.Compute(File.ReadAllBytes(bundlePath)));
        Expect(payload["CurrentAndroidTreeSha256"]?.GetValue<string>()
            == currentTreeSha256);
        Expect(payload["SelectedSaveContextSha256"]?.GetValue<string>()
            == selectedContextSha256);
    }

    private static async Task
        CurrentSaveExportIncludesVerifiedRollbackSnapshotsAsync()
    {
        var local = new InMemoryCloudSaveStore(
            "current-export-rollback-local"
        );
        var context = Context();
        var restoredBytes = SpecialBytes(
            "{\"ironclad\":10,\"regent\":8,\"state\":\"restored\"}\r\n\0"
        );
        var rollbackBytes = SpecialBytes(
            "{\"ironclad\":7,\"state\":\"before-restore\"}\r\n\0"
        );
        var olderRetainedBytes = SpecialBytes(
            "{\"ironclad\":6,\"state\":\"retained\"}\r\n\0"
        );
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.SeedBytes(ProgressPath, restoredBytes);
        var source = await CloudSyncCoordinator
            .CaptureRetainedLocalSnapshotAsync(
                local,
                context,
                "export-restore-source",
                CancellationToken.None
            ).ConfigureAwait(false);
        local.SeedBytes(ProgressPath, olderRetainedBytes);
        var olderRetained = await CloudSyncCoordinator
            .CaptureRetainedLocalSnapshotAsync(
                local,
                context,
                "export-older-retained",
                CancellationToken.None
            ).ConfigureAwait(false);
        local.SeedBytes(ProgressPath, rollbackBytes);
        await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
            local,
            source.Path,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            InMemoryCloudSaveStore.DefaultSteamId64,
            CancellationToken.None
        ).ConfigureAwait(false);

        Expect(local.TryReadSeeded(
            CloudSyncCoordinator.SaveRecoveryJournalPath,
            out var journalJson
        ));
        var journal = JsonNode.Parse(journalJson)?.AsObject()
            ?? throw new InvalidDataException("Recovery journal was empty");
        var sourceSha = journal["SourceSnapshotSha256"]?.GetValue<string>()
            ?? "";
        var undoSha = journal["UndoSnapshotSha256"]?.GetValue<string>()
            ?? "";
        var appliedSha = journal["AppliedSnapshotSha256"]?.GetValue<string>()
            ?? "";
        Expect(sourceSha.Length == 64);
        Expect(undoSha.Length == 64);
        Expect(appliedSha.Length == 64);

        var mutations = new List<StoreMutation>();
        local.MutationObserved = mutation => mutations.Add(mutation);
        var writeCount = local.WriteCount;
        var rawWriteCount = local.RawWriteCount;
        var recoveryWriteCount = local.RecoveryWriteCount;
        var deleteCount = local.DeleteCount;
        var recoveryDeleteCount = local.RecoveryDeleteCount;
        var json = await CloudSyncCoordinator.BuildCurrentSaveExportBundleAsync(
            local,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            cloudSyncEnabled: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidDataException("Rollback export was empty");
        var snapshots = root["RecoverySnapshots"]?.AsArray()
            ?? throw new InvalidDataException(
                "Rollback snapshots were omitted"
            );
        Expect(snapshots.Count >= 4);
        Expect(root["OmittedSnapshotCount"]?.GetValue<int>() == 0);
        Expect(snapshots.All(node =>
            node?["Coverage"]?.GetValue<string>() == "full"
            && node?["Snapshot"]?["Coverage"]?.GetValue<string>() == "full"
        ));
        var byIdentity = snapshots
            .Select(node => node?.AsObject()
                ?? throw new InvalidDataException(
                    "Rollback snapshot entry was empty"
                ))
            .ToDictionary(
                item => item["CandidateId"]?.GetValue<string>() ?? "",
                StringComparer.OrdinalIgnoreCase
            );
        foreach (var identity in new[]
        {
            sourceSha,
            undoSha,
            appliedSha,
            olderRetained.Sha256,
        })
        {
            Expect(byIdentity.ContainsKey(identity));
            Expect(byIdentity[identity]["SnapshotSha256"]?.GetValue<string>()
                == identity);
        }
        ExpectExportedSnapshotBytes(
            byIdentity[sourceSha],
            ProgressPath,
            restoredBytes
        );
        ExpectExportedSnapshotBytes(
            byIdentity[undoSha],
            ProgressPath,
            rollbackBytes
        );
        ExpectExportedSnapshotBytes(
            byIdentity[appliedSha],
            ProgressPath,
            restoredBytes
        );
        ExpectExportedSnapshotBytes(
            byIdentity[olderRetained.Sha256],
            ProgressPath,
            olderRetainedBytes
        );
        Expect(mutations.Count == 0);
        Expect(local.WriteCount == writeCount);
        Expect(local.RawWriteCount == rawWriteCount);
        Expect(local.RecoveryWriteCount == recoveryWriteCount);
        Expect(local.DeleteCount == deleteCount);
        Expect(local.RecoveryDeleteCount == recoveryDeleteCount);

        var partialLocal = new InMemoryCloudSaveStore(
            "current-export-partial-journal-local"
        );
        partialLocal.Seed(ProfilePath, "{\"profile\":1}");
        partialLocal.SeedBytes(ProgressPath, rollbackBytes);
        var partialSource = await CloudSyncCoordinator
            .WriteImportedSnapshotAsync(
                partialLocal,
                CloudSyncCoordinator.RecoveryUnknownDirectory,
                PartialSnapshot(ProgressPath, restoredBytes),
                CancellationToken.None
            ).ConfigureAwait(false);
        await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
            partialLocal,
            partialSource.Path,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            targetSteamId64: null,
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(partialLocal.TryReadSeeded(
            CloudSyncCoordinator.SaveRecoveryJournalPath,
            out var partialJournalJson
        ));
        var partialJournal = JsonNode.Parse(partialJournalJson)?.AsObject()
            ?? throw new InvalidDataException(
                "Partial recovery journal was empty"
            );
        var partialSourceSha = partialJournal["SourceSnapshotSha256"]
            ?.GetValue<string>() ?? "";
        var partialUndoSha = partialJournal["UndoSnapshotSha256"]
            ?.GetValue<string>() ?? "";
        var partialAppliedSha = partialJournal["AppliedSnapshotSha256"]
            ?.GetValue<string>() ?? "";
        var partialMutations = new List<StoreMutation>();
        partialLocal.MutationObserved = mutation =>
            partialMutations.Add(mutation);
        var partialWrites = partialLocal.WriteCount;
        var partialRawWrites = partialLocal.RawWriteCount;
        var partialRecoveryWrites = partialLocal.RecoveryWriteCount;
        var partialDeletes = partialLocal.DeleteCount;
        var partialRecoveryDeletes = partialLocal.RecoveryDeleteCount;
        var partialJson = await CloudSyncCoordinator
            .BuildCurrentSaveExportBundleAsync(
                partialLocal,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                cloudSyncEnabled: true,
                CancellationToken.None
            ).ConfigureAwait(false);
        var partialRoot = JsonNode.Parse(partialJson)?.AsObject()
            ?? throw new InvalidDataException(
                "Partial-journal export was empty"
            );
        var fullOnly = partialRoot["RecoverySnapshots"]?.AsArray()
            ?? throw new InvalidDataException(
                "Full rollback snapshots were omitted"
            );
        var fullOnlyIds = fullOnly
            .Select(node => node?["CandidateId"]?.GetValue<string>() ?? "")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Expect(!fullOnlyIds.Contains(partialSourceSha));
        Expect(fullOnlyIds.Contains(partialUndoSha));
        Expect(fullOnlyIds.Contains(partialAppliedSha));
        Expect(fullOnly.All(node =>
            node?["Snapshot"]?["Coverage"]?.GetValue<string>() == "full"
        ));
        Expect(partialRoot["OmittedSnapshotCount"]?.GetValue<int>() >= 1);
        Expect(partialMutations.Count == 0);
        Expect(partialLocal.WriteCount == partialWrites);
        Expect(partialLocal.RawWriteCount == partialRawWrites);
        Expect(partialLocal.RecoveryWriteCount == partialRecoveryWrites);
        Expect(partialLocal.DeleteCount == partialDeletes);
        Expect(partialLocal.RecoveryDeleteCount
            == partialRecoveryDeletes);
    }

    private static void ExpectExportedSnapshotBytes(
        JsonObject recoveryEntry,
        string path,
        byte[] expected
    )
    {
        var files = recoveryEntry["Snapshot"]?["Files"]?.AsArray()
            ?? throw new InvalidDataException(
                "Exported recovery snapshot has no files"
            );
        var file = files
            .Select(node => node?.AsObject())
            .Single(item => string.Equals(
                item?["Path"]?.GetValue<string>(),
                path,
                StringComparison.Ordinal
            )) ?? throw new InvalidDataException(
                $"Exported recovery snapshot omitted {path}"
            );
        var bytes = Convert.FromBase64String(
            file["ContentBase64"]?.GetValue<string>() ?? ""
        );
        Expect(bytes.AsSpan().SequenceEqual(expected));
        Expect(file["ByteSha256"]?.GetValue<string>()
            == AutomaticSyncHash.Compute(expected));
    }

    private static async Task RestoreAndUndoAreByteExactAsync()
    {
        var local = new InMemoryCloudSaveStore("recovery-local");
        var cloud = new InMemoryCloudSaveStore("recovery-cloud");
        var advanced = SpecialBytes("{\"ironclad\":10,\"name\":\"Régent\"}\r\n\0");
        var regressed = SpecialBytes("{\"ironclad\":7}\r\n\0");
        var run = Encoding.UTF8.GetBytes("{\"floor\":12}");
        var extraHistory = Encoding.UTF8.GetBytes("{\"run\":\"new-local\"}");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.SeedBytes(ProgressPath, advanced);
        local.Seed(HistoryA, "{\"run\":\"advanced\"}");
        var context = Context();
        var source = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            context,
            "recovery-test",
            CancellationToken.None
        ).ConfigureAwait(false);

        local.SeedBytes(ProgressPath, regressed);
        local.SeedBytes(CurrentRunPath, run);
        local.SeedBytes(HistoryB, extraHistory);
        cloud.Seed(ProgressPath, "{\"cloud\":\"untouched\"}");

        var terminalLogs = new List<string>();
        void CaptureTerminal(string message)
        {
            if (message.StartsWith(
                    SaveEvidenceEvents.Marker,
                    StringComparison.Ordinal
                ))
            {
                terminalLogs.Add(message);
            }
        }
        PatchHelper.LogEmitted += CaptureTerminal;
        SaveRecoveryOperationResult restored;
        SaveRecoveryOperationResult undone;
        try
        {
            restored = await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                source.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                InMemoryCloudSaveStore.DefaultSteamId64,
                CancellationToken.None
            ).ConfigureAwait(false);

        Expect(restored.Status.SyncHeld);
        Expect(restored.Status.ValidationRequired);
        ExpectBytes(local, ProgressPath, advanced);
        Expect(!local.Contains(CurrentRunPath));
        Expect(!local.Contains(HistoryB));
        Expect(local.RecoveryWriteCount > 0);
        Expect(local.RecoveryDeleteCount > 0);
        Expect(local.RawWriteCount == 0);
        Expect(cloud.Operations.Count == 0);

        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ManualPushAllAsync(
                local,
                cloud,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                new CloudOperationProgressTracker(CloudOperationKind.Push),
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ReconcileAutomaticSyncAsync(
                local,
                cloud,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                sourceChoice: null,
                new CloudOperationProgressTracker(CloudOperationKind.Push),
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        Expect(cloud.Operations.Count == 0);

            var recoveryWritesBeforeUndo = local.RecoveryWriteCount;
            undone = await CloudSyncCoordinator.UndoSaveRecoveryAsync(
                local,
                CancellationToken.None
            ).ConfigureAwait(false);
            Expect(local.RecoveryWriteCount > recoveryWritesBeforeUndo);
        }
        finally
        {
            PatchHelper.LogEmitted -= CaptureTerminal;
        }
        Expect(!undone.Status.SyncHeld);
        Expect(!undone.Status.CanUndo);
        ExpectBytes(local, ProgressPath, regressed);
        ExpectBytes(local, CurrentRunPath, run);
        ExpectBytes(local, HistoryB, extraHistory);
        Expect(local.RawWriteCount == 0);
        Expect(cloud.Operations.Count == 0);
        ExpectRecoveryTerminal(terminalLogs, "restore");
        ExpectRecoveryTerminal(terminalLogs, "undo");
        _ = await CloudSyncCoordinator.ReadVerifiedSnapshotAsync(
            local,
            source.Path,
            context,
            CancellationToken.None
        ).ConfigureAwait(false);
    }

    private static void ExpectRecoveryTerminal(
        IEnumerable<string> messages,
        string operation
    )
    {
        var matches = 0;
        foreach (var message in messages)
        {
            using var document = JsonDocument.Parse(
                message[SaveEvidenceEvents.Marker.Length..]
            );
            var root = document.RootElement;
            if (root.GetProperty("Event").GetString()
                    != "save-recovery-terminal")
            {
                continue;
            }
            Expect(root.EnumerateObject().Count() == 5);
            Expect(root.GetProperty("Version").ValueKind
                == JsonValueKind.Number);
            Expect(root.GetProperty("Version").GetInt32() == 1);
            Expect(root.GetProperty("Outcome").GetString() == "completed");
            Expect(root.GetProperty("Detail").GetString()
                == "byte-verified-local-only");
            if (root.GetProperty("Operation").GetString() == operation)
                matches++;
        }
        Expect(matches == 1);
    }

    private static async Task CaseFoldHistoryCollisionBlocksBeforeMutationAsync()
    {
        var local = new InMemoryCloudSaveStore("collision-local");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":10}");
        var source = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            Context(),
            "collision-source",
            CancellationToken.None
        ).ConfigureAwait(false);

        local.Seed(ProgressPath, "{\"progress\":7}");
        var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
            SaveNamespace.Vanilla,
            1
        );
        local.Seed($"{historyDirectory}/Collision.run", "{\"run\":1}");
        local.FileListingTransform = (directory, files) =>
            string.Equals(
                directory,
                historyDirectory,
                StringComparison.OrdinalIgnoreCase
            )
                ? files.Concat(new[] { "collision.run" }).ToArray()
                : files;
        var before = ReadBytes(local, ProgressPath);

        await ExpectThrowsAsync<InvalidDataException>(() =>
            CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                source.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                InMemoryCloudSaveStore.DefaultSteamId64,
                CancellationToken.None
            )
        ).ConfigureAwait(false);

        ExpectBytes(local, ProgressPath, before);
        Expect(local.RecoveryWriteCount == 0);
        Expect(local.RecoveryDeleteCount == 0);
        Expect(local.RawWriteCount == 0);
        Expect(!CloudSyncCoordinator.HasSaveRecoverySyncHold(local));
    }

    private static Task FullSourceLocalHistoryCaseCollisionBlocksAsync()
        => SourceLocalHistoryCaseCollisionBlocksAsync(full: true);

    private static Task PartialSourceLocalHistoryCaseCollisionBlocksAsync()
        => SourceLocalHistoryCaseCollisionBlocksAsync(full: false);

    private static async Task SourceLocalHistoryCaseCollisionBlocksAsync(
        bool full
    )
    {
        var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
            SaveNamespace.Vanilla,
            1
        );
        var localHistoryPath = $"{historyDirectory}/Run.run";
        var sourceHistoryPath = $"{historyDirectory}/run.run";
        var localHistoryBytes = Encoding.UTF8.GetBytes(
            "{\"run\":\"original-local\"}"
        );
        var sourceHistoryBytes = Encoding.UTF8.GetBytes(
            "{\"run\":\"source-replacement\"}"
        );
        var local = new InMemoryCloudSaveStore(
            full
                ? "combined-full-case-collision-local"
                : "combined-partial-case-collision-local"
        );
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":7}");
        local.SeedBytes(localHistoryPath, localHistoryBytes);

        CloudSyncCoordinator.AutomaticSaveSnapshot sourceSnapshot;
        ulong? targetSteamId64;
        if (full)
        {
            var sourceStore = new InMemoryCloudSaveStore(
                "combined-full-case-collision-source"
            );
            sourceStore.Seed(ProfilePath, "{\"profile\":1}");
            sourceStore.Seed(ProgressPath, "{\"progress\":10}");
            sourceStore.SeedBytes(sourceHistoryPath, sourceHistoryBytes);
            var captured = await CloudSyncCoordinator
                .CaptureRetainedLocalSnapshotAsync(
                    sourceStore,
                    Context(),
                    "combined-full-case-collision-source",
                    CancellationToken.None
                ).ConfigureAwait(false);
            sourceSnapshot = captured.Snapshot;
            targetSteamId64 = InMemoryCloudSaveStore.DefaultSteamId64;
        }
        else
        {
            sourceSnapshot = PartialSnapshot(
                sourceHistoryPath,
                sourceHistoryBytes
            );
            targetSteamId64 = null;
        }

        var imported = await CloudSyncCoordinator.WriteImportedSnapshotAsync(
            local,
            full
                ? CloudSyncCoordinator.RecoveryImportedDirectory
                : CloudSyncCoordinator.RecoveryUnknownDirectory,
            sourceSnapshot,
            CancellationToken.None
        ).ConfigureAwait(false);
        var recoveryWritesBefore = local.RecoveryWriteCount;
        var recoveryDeletesBefore = local.RecoveryDeleteCount;

        await ExpectThrowsAsync<InvalidDataException>(() =>
            CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                imported.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                targetSteamId64,
                CancellationToken.None
            )
        ).ConfigureAwait(false);

        Expect(local.RecoveryWriteCount == recoveryWritesBefore);
        Expect(local.RecoveryDeleteCount == recoveryDeletesBefore);
        Expect(local.RawWriteCount == 0);
        ExpectBytes(local, localHistoryPath, localHistoryBytes);
        Expect(local.Paths.Contains(
            localHistoryPath,
            StringComparer.Ordinal
        ));
        Expect(!local.Paths.Contains(
            sourceHistoryPath,
            StringComparer.Ordinal
        ));
    }

    private static async Task PartialRestoreIsOverwriteOnlyAsync()
    {
        var local = new InMemoryCloudSaveStore("partial-local");
        var replacement = SpecialBytes("{\"regent\":8}\r\n\0");
        var existingRun = Encoding.UTF8.GetBytes("{\"floor\":33}");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"regent\":1}");
        local.SeedBytes(CurrentRunPath, existingRun);
        var snapshot = PartialSnapshot(ProgressPath, replacement);
        var imported = await CloudSyncCoordinator.WriteImportedSnapshotAsync(
            local,
            CloudSyncCoordinator.RecoveryUnknownDirectory,
            snapshot,
            CancellationToken.None
        ).ConfigureAwait(false);

        await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
            local,
            imported.Path,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            InMemoryCloudSaveStore.DefaultSteamId64,
            CancellationToken.None
        ).ConfigureAwait(false);

        ExpectBytes(local, ProgressPath, replacement);
        ExpectBytes(local, CurrentRunPath, existingRun);
        await CloudSyncCoordinator.UndoSaveRecoveryAsync(
            local,
            CancellationToken.None
        ).ConfigureAwait(false);
        ExpectSeeded(local, ProgressPath, "{\"regent\":1}");
        ExpectBytes(local, CurrentRunPath, existingRun);
    }

    private static async Task PartialRestorePostWriteDriftCannotBeUndoneAsync()
    {
        var local = new InMemoryCloudSaveStore("partial-post-write-drift-local");
        var replacement = Encoding.UTF8.GetBytes("{\"progress\":8}");
        var existingRun = Encoding.UTF8.GetBytes("{\"floor\":33}");
        const string externalPrefs = "{\"independentWrite\":true}";
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":7}");
        local.Seed(PrefsPath, "{\"prefs\":\"before\"}");
        local.SeedBytes(CurrentRunPath, existingRun);
        var snapshot = PartialSnapshot(
            ProgressPath,
            replacement,
            CurrentRunPath
        );
        var imported = await CloudSyncCoordinator.WriteImportedSnapshotAsync(
            local,
            CloudSyncCoordinator.RecoveryUnknownDirectory,
            snapshot,
            CancellationToken.None
        ).ConfigureAwait(false);

        var injected = false;
        local.MutationObserved = mutation =>
        {
            if (injected
                || mutation.Kind != StoreMutationKind.Write
                || mutation.Edge != StoreMutationEdge.After
                || !string.Equals(
                    mutation.Path,
                    ProgressPath,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return;
            }

            injected = true;
            local.Seed(PrefsPath, externalPrefs);
        };

        try
        {
            await ExpectThrowsAsync<IOException>(() =>
                CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                    local,
                    imported.Path,
                    SaveNamespace.Vanilla,
                    SteamGameBranch.Public,
                    "",
                    targetSteamId64: null,
                    CancellationToken.None
                )
            ).ConfigureAwait(false);
        }
        finally
        {
            local.MutationObserved = null;
        }

        Expect(injected);
        ExpectBytes(local, ProgressPath, replacement);
        ExpectSeeded(local, PrefsPath, externalPrefs);
        ExpectBytes(local, CurrentRunPath, existingRun);
        Expect(CloudSyncCoordinator.HasSaveRecoverySyncHold(local));

        try
        {
            await CloudSyncCoordinator.UndoSaveRecoveryAsync(
                local,
                CancellationToken.None
            ).ConfigureAwait(false);
        }
        catch (Exception ex) when (
            ex is InvalidDataException
                or InvalidOperationException
                or IOException
        )
        {
            // Refusal is the expected conservative result. A future Undo may
            // also complete if it can prove the unrelated bytes are retained.
        }

        ExpectSeeded(local, PrefsPath, externalPrefs);
        ExpectBytes(local, CurrentRunPath, existingRun);
    }

    private static async Task DriftBlocksUndoAsync()
    {
        var fixture = await RestoredFixtureAsync("drift").ConfigureAwait(false);
        fixture.Local.Seed(PrefsPath, "{\"changedAfterRestore\":true}");
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.UndoSaveRecoveryAsync(
                fixture.Local,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        Expect(CloudSyncCoordinator.HasSaveRecoverySyncHold(fixture.Local));
        Expect(fixture.Cloud.Operations.Count == 0);
    }

    private static async Task ApprovalReleasesHoldAsync()
    {
        var fixture = await RestoredFixtureAsync("approval").ConfigureAwait(false);
        fixture.Local.Seed(PrefsPath, "{\"validatedLocally\":true}");
        var approved = await CloudSyncCoordinator.ApproveSaveRecoveryForSyncAsync(
            fixture.Local,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            InMemoryCloudSaveStore.DefaultSteamId64,
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(!approved.Status.SyncHeld);
        Expect(approved.Status.CanUndo);
        Expect(fixture.Cloud.Operations.Count == 0);

        fixture.Local.Seed(
            CloudSyncCoordinator.SaveRecoveryHoldPath,
            "{\"obsolete\":true}"
        );
        Expect(!CloudSyncCoordinator.HasSaveRecoverySyncHold(fixture.Local));
        var wrongAccountCloud = new InMemoryCloudSaveStore(
            "approval-wrong-account",
            InMemoryCloudSaveStore.DefaultSteamId64 + 1
        );
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ManualPushAllAsync(
                fixture.Local,
                wrongAccountCloud,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                new CloudOperationProgressTracker(CloudOperationKind.Push),
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        Expect(wrongAccountCloud.Operations.SequenceEqual(new[] { "authenticate" }));

        var accountB = SaveContext.Create(
            InMemoryCloudSaveStore.DefaultSteamId64 + 1,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            ""
        );
        var accountBSnapshot = await CloudSyncCoordinator
            .CaptureRetainedLocalSnapshotAsync(
                fixture.Local,
                accountB,
                "nested-account-b",
                CancellationToken.None
            ).ConfigureAwait(false);
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                fixture.Local,
                accountBSnapshot.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                accountB.SteamId64,
                CancellationToken.None
            )
        ).ConfigureAwait(false);

        await CloudSyncCoordinator.UndoSaveRecoveryAsync(
            fixture.Local,
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(!CloudSyncCoordinator.HasSaveRecoverySyncHold(fixture.Local));
        Expect(!fixture.Local.Contains(PrefsPath));
    }

    private static async Task InterruptedRestoreCanUndoAsync()
    {
        var local = new InMemoryCloudSaveStore("interrupted-local");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":10}");
        var source = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            Context(),
            "interruption-source",
            CancellationToken.None
        ).ConfigureAwait(false);
        local.Seed(ProgressPath, "{\"progress\":7}");
        local.MutationObserved = mutation =>
        {
            if (mutation.Kind == StoreMutationKind.Write
                && mutation.Edge == StoreMutationEdge.Before
                && string.Equals(
                    mutation.Path,
                    ProgressPath,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                throw new IOException("simulated restore interruption");
            }
        };

        await ExpectThrowsAsync<IOException>(() =>
            CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                source.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                InMemoryCloudSaveStore.DefaultSteamId64,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        local.MutationObserved = null;
        var status = CloudSyncCoordinator.InspectSaveRecoveryStatus(local);
        Expect(status.SyncHeld);
        Expect(status.CanUndo);

        await CloudSyncCoordinator.UndoSaveRecoveryAsync(
            local,
            CancellationToken.None
        ).ConfigureAwait(false);
        ExpectSeeded(local, ProgressPath, "{\"progress\":7}");
        Expect(!CloudSyncCoordinator.HasSaveRecoverySyncHold(local));
    }

    private static async Task RestoreJournalCommitDriftCannotBeUndoneAsync()
    {
        var local = new InMemoryCloudSaveStore("restore-journal-drift-local");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":10}");
        var source = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            Context(),
            "restore-journal-drift-source",
            CancellationToken.None
        ).ConfigureAwait(false);
        local.Seed(ProgressPath, "{\"progress\":7}");

        const string externalProgress = "{\"externalAfterJournal\":true}";
        var injected = false;
        local.MutationObserved = mutation =>
        {
            if (injected
                || mutation.Kind != StoreMutationKind.Rename
                || mutation.Edge != StoreMutationEdge.After
                || !string.Equals(
                    mutation.DestinationPath,
                    CloudSyncCoordinator.SaveRecoveryJournalPath,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return;
            }

            injected = true;
            local.Seed(ProgressPath, externalProgress);
        };

        try
        {
            await ExpectThrowsAsync<InvalidOperationException>(() =>
                CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                    local,
                    source.Path,
                    SaveNamespace.Vanilla,
                    SteamGameBranch.Public,
                    "",
                    InMemoryCloudSaveStore.DefaultSteamId64,
                    CancellationToken.None
                )
            ).ConfigureAwait(false);
        }
        finally
        {
            local.MutationObserved = null;
        }

        Expect(injected);
        ExpectSeeded(local, ProgressPath, externalProgress);
        var undone = await CloudSyncCoordinator.UndoSaveRecoveryAsync(
            local,
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(!undone.Status.CanUndo);
        ExpectSeeded(local, ProgressPath, externalProgress);
    }

    private static async Task UndoJournalCommitDriftBlocksRetryAsync()
    {
        var fixture = await RestoredFixtureAsync("undo-journal-drift")
            .ConfigureAwait(false);
        const string externalPrefs = "{\"externalDuringUndo\":true}";
        var injected = false;
        fixture.Local.MutationObserved = mutation =>
        {
            if (injected
                || mutation.Kind != StoreMutationKind.Rename
                || mutation.Edge != StoreMutationEdge.After
                || !string.Equals(
                    mutation.DestinationPath,
                    CloudSyncCoordinator.SaveRecoveryJournalPath,
                    StringComparison.OrdinalIgnoreCase
                )
                || !fixture.Local.TryReadSeeded(
                    CloudSyncCoordinator.SaveRecoveryJournalPath,
                    out var journalJson
                ))
            {
                return;
            }

            var phase = JsonNode.Parse(journalJson)?["Phase"]?.GetValue<string>();
            if (!string.Equals(phase, "undoing", StringComparison.Ordinal))
                return;

            injected = true;
            fixture.Local.Seed(PrefsPath, externalPrefs);
        };

        try
        {
            await ExpectThrowsAsync<InvalidOperationException>(() =>
                CloudSyncCoordinator.UndoSaveRecoveryAsync(
                    fixture.Local,
                    CancellationToken.None
                )
            ).ConfigureAwait(false);
        }
        finally
        {
            fixture.Local.MutationObserved = null;
        }

        Expect(injected);
        ExpectSeeded(fixture.Local, PrefsPath, externalPrefs);
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.UndoSaveRecoveryAsync(
                fixture.Local,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        ExpectSeeded(fixture.Local, PrefsPath, externalPrefs);
        Expect(CloudSyncCoordinator.HasSaveRecoverySyncHold(fixture.Local));
        Expect(fixture.Cloud.Operations.Count == 0);
    }

    private static async Task JournalSnapshotRoleCrossLinksFailClosedAsync()
    {
        var fixture = await RestoredFixtureAsync("journal-role-cross-link")
            .ConfigureAwait(false);
        Expect(fixture.Local.TryReadSeeded(
            CloudSyncCoordinator.SaveRecoveryJournalPath,
            out var originalJournal
        ));
        var original = JsonNode.Parse(originalJournal)!.AsObject();

        foreach (var replacementRole in new[] { "Source", "Applied" })
        {
            var local = fixture.Local.Fork(
                $"journal-role-cross-link-{replacementRole.ToLowerInvariant()}"
            );
            var journal = JsonNode.Parse(originalJournal)!.AsObject();
            journal["UndoSnapshotPath"] =
                original[$"{replacementRole}SnapshotPath"]!.GetValue<string>();
            journal["UndoSnapshotSha256"] =
                original[$"{replacementRole}SnapshotSha256"]!.GetValue<string>();
            local.Seed(
                CloudSyncCoordinator.SaveRecoveryJournalPath,
                journal.ToJsonString()
            );
            var before = ReadBytes(local, ProgressPath);

            var status = CloudSyncCoordinator.InspectSaveRecoveryStatus(local);
            Expect(status.SyncHeld);
            Expect(!status.CanUndo);
            await ExpectThrowsAsync<InvalidDataException>(() =>
                CloudSyncCoordinator.UndoSaveRecoveryAsync(
                    local,
                    CancellationToken.None
                )
            ).ConfigureAwait(false);
            ExpectBytes(local, ProgressPath, before);
        }
    }

    private static async Task UndoSnapshotContextMismatchFailsAsync()
    {
        var fixture = await RestoredFixtureAsync("undo-context-mismatch")
            .ConfigureAwait(false);
        var accountB = SaveContext.Create(
            InMemoryCloudSaveStore.DefaultSteamId64 + 1,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            ""
        );
        var accountBSnapshot = await CloudSyncCoordinator
            .CaptureRetainedLocalSnapshotAsync(
                fixture.Local,
                accountB,
                "undo-account-b",
                CancellationToken.None
            ).ConfigureAwait(false);
        var accountBUndo = await CloudSyncCoordinator.WriteImportedSnapshotAsync(
            fixture.Local,
            ".sts2-launcher/recovery/undo",
            accountBSnapshot.Snapshot,
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(fixture.Local.TryReadSeeded(
            CloudSyncCoordinator.SaveRecoveryJournalPath,
            out var journalJson
        ));
        var journal = JsonNode.Parse(journalJson)!.AsObject();
        journal["UndoSnapshotPath"] = accountBUndo.Path;
        journal["UndoSnapshotSha256"] = accountBUndo.Sha256;
        fixture.Local.Seed(
            CloudSyncCoordinator.SaveRecoveryJournalPath,
            journal.ToJsonString()
        );
        var before = ReadBytes(fixture.Local, ProgressPath);

        await ExpectRecoveryRefusalAsync(() =>
            CloudSyncCoordinator.UndoSaveRecoveryAsync(
                fixture.Local,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        ExpectBytes(fixture.Local, ProgressPath, before);
        Expect(CloudSyncCoordinator.HasSaveRecoverySyncHold(fixture.Local));
        Expect(fixture.Cloud.Operations.Count == 0);
    }

    private static async Task OrphanedLegacyHoldDoesNotBlockSyncAsync()
    {
        var local = new InMemoryCloudSaveStore("legacy-hold-only-local");
        var cloud = new InMemoryCloudSaveStore("legacy-hold-only-cloud")
        {
            AuthenticationFailure = new IOException("planned auth stop"),
        };
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":10}");
        local.Seed(CloudSyncCoordinator.SaveRecoveryHoldPath, "{}");

        var status = CloudSyncCoordinator.InspectSaveRecoveryStatus(local);
        Expect(!status.SyncHeld);
        Expect(!status.CanUndo);
        Expect(!CloudSyncCoordinator.HasSaveRecoverySyncHold(local));
        await ExpectThrowsAsync<IOException>(() =>
            CloudSyncCoordinator.ManualPushAllAsync(
                local,
                cloud,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                new CloudOperationProgressTracker(CloudOperationKind.Push),
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        Expect(cloud.Operations.SequenceEqual(new[] { "authenticate" }));
    }

    private static async Task AccountBindingAndUnknownProvenanceAsync()
    {
        var local = new InMemoryCloudSaveStore("account-binding-local");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":10}");
        var exact = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            Context(),
            "account-a",
            CancellationToken.None
        ).ConfigureAwait(false);
        local.Seed(ProgressPath, "{\"progress\":7}");
        var before = ReadBytes(local, ProgressPath);

        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                exact.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                targetSteamId64: null,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                exact.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                InMemoryCloudSaveStore.DefaultSteamId64 + 1,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        ExpectBytes(local, ProgressPath, before);
        Expect(!CloudSyncCoordinator.HasSaveRecoverySyncHold(local));

        var unknown = await CloudSyncCoordinator.WriteImportedSnapshotAsync(
            local,
            CloudSyncCoordinator.RecoveryUnknownDirectory,
            PartialSnapshot(
                ProgressPath,
                Encoding.UTF8.GetBytes("{\"progress\":8}")
            ),
            CancellationToken.None
        ).ConfigureAwait(false);
        var restored = await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
            local,
            unknown.Path,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            targetSteamId64: null,
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(restored.Status.SyncHeld);
        Expect(restored.Status.ValidationRequired);
        Expect(!restored.Status.CanApprove);
        Expect(!restored.Status.SteamId64.HasValue);

        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ApproveSaveRecoveryForSyncAsync(
                local,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                InMemoryCloudSaveStore.DefaultSteamId64,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        var cloud = new InMemoryCloudSaveStore("unknown-account-cloud");
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ManualPushAllAsync(
                local,
                cloud,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                new CloudOperationProgressTracker(CloudOperationKind.Push),
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        Expect(cloud.Operations.Count == 0);
        await CloudSyncCoordinator.UndoSaveRecoveryAsync(
            local,
            CancellationToken.None
        ).ConfigureAwait(false);
    }

    private static async Task UnknownRecoveryCannotInheritTargetAccountAsync()
    {
        var local = new InMemoryCloudSaveStore("unknown-supplied-account-local");
        var cloud = new InMemoryCloudSaveStore("unknown-supplied-account-cloud");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":7}");
        var unknown = await CloudSyncCoordinator.WriteImportedSnapshotAsync(
            local,
            CloudSyncCoordinator.RecoveryUnknownDirectory,
            PartialSnapshot(
                ProgressPath,
                Encoding.UTF8.GetBytes("{\"progress\":8}")
            ),
            CancellationToken.None
        ).ConfigureAwait(false);

        var restored = await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
            local,
            unknown.Path,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            InMemoryCloudSaveStore.DefaultSteamId64,
            CancellationToken.None
        ).ConfigureAwait(false);

        Expect(restored.Status.SyncHeld);
        Expect(restored.Status.ValidationRequired);
        Expect(!restored.Status.CanApprove);
        Expect(!restored.Status.SteamId64.HasValue);
        Expect(local.TryReadSeeded(
            CloudSyncCoordinator.SaveRecoveryJournalPath,
            out var journalJson
        ));
        var targetContextMarker = JsonNode.Parse(journalJson)?[
            "TargetContextMarker"
        ]?.GetValue<string>();
        Expect(string.IsNullOrEmpty(targetContextMarker));

        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ApproveSaveRecoveryForSyncAsync(
                local,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                InMemoryCloudSaveStore.DefaultSteamId64,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ManualPushAllAsync(
                local,
                cloud,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                new CloudOperationProgressTracker(CloudOperationKind.Push),
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ReconcileAutomaticSyncAsync(
                local,
                cloud,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                sourceChoice: null,
                new CloudOperationProgressTracker(CloudOperationKind.Push),
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        Expect(cloud.Operations.Count == 0);

        await CloudSyncCoordinator.UndoSaveRecoveryAsync(
            local,
            CancellationToken.None
        ).ConfigureAwait(false);
    }

    private static async Task AuthoritativeJournalFailsClosedAsync()
    {
        var fixture = await RestoredFixtureAsync("journal-only")
            .ConfigureAwait(false);
        Expect(CloudSyncCoordinator.HasSaveRecoverySyncHold(fixture.Local));
        Expect(!fixture.Local.Contains(CloudSyncCoordinator.SaveRecoveryHoldPath));
        Expect(fixture.Local.TryReadSeeded(
            CloudSyncCoordinator.SaveRecoveryJournalPath,
            out var journalJson
        ));

        var broken = JsonNode.Parse(journalJson)!.AsObject();
        broken["UndoSnapshotPath"] = "";
        broken["UndoSnapshotSha256"] = "";
        fixture.Local.Seed(
            CloudSyncCoordinator.SaveRecoveryJournalPath,
            broken.ToJsonString()
        );
        var brokenStatus = CloudSyncCoordinator.InspectSaveRecoveryStatus(
            fixture.Local
        );
        Expect(brokenStatus.SyncHeld);
        Expect(!brokenStatus.CanUndo);
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.ManualPushAllAsync(
                fixture.Local,
                fixture.Cloud,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                new CloudOperationProgressTracker(CloudOperationKind.Push),
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        Expect(fixture.Cloud.Operations.Count == 0);

        var corrupt = new InMemoryCloudSaveStore("corrupt-journal");
        corrupt.Seed(CloudSyncCoordinator.SaveRecoveryJournalPath, "{");
        Expect(CloudSyncCoordinator.HasSaveRecoverySyncHold(corrupt));
    }

    private static async Task CoreLeaseSerializesSyncAndRecoveryAsync()
    {
        var local = new InMemoryCloudSaveStore("lease-local");
        var cloud = new InMemoryCloudSaveStore("lease-cloud")
        {
            AuthenticationFailure = new IOException("planned auth stop"),
        };
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":10}");
        var source = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            Context(),
            "lease-source",
            CancellationToken.None
        ).ConfigureAwait(false);
        local.Seed(ProgressPath, "{\"progress\":7}");

        var enteredAuthentication = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var releaseAuthentication = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        cloud.BeforeAuthenticateAsync = async token =>
        {
            enteredAuthentication.TrySetResult(true);
            await releaseAuthentication.Task.WaitAsync(token)
                .ConfigureAwait(false);
        };
        var syncTask = CloudSyncCoordinator.ManualPushAllAsync(
            local,
            cloud,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            new CloudOperationProgressTracker(CloudOperationKind.Push),
            CancellationToken.None
        );
        await enteredAuthentication.Task.WaitAsync(TimeSpan.FromSeconds(2))
            .ConfigureAwait(false);

        var restoreTask = CloudSyncCoordinator.RestoreSaveSnapshotAsync(
            local,
            source.Path,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            InMemoryCloudSaveStore.DefaultSteamId64,
            CancellationToken.None
        );
        var early = await Task.WhenAny(
            restoreTask,
            Task.Delay(100)
        ).ConfigureAwait(false);
        Expect(!ReferenceEquals(early, restoreTask));
        Expect(!local.Contains(CloudSyncCoordinator.SaveRecoveryJournalPath));

        releaseAuthentication.TrySetResult(true);
        await ExpectThrowsAsync<IOException>(() => syncTask)
            .ConfigureAwait(false);
        await restoreTask.ConfigureAwait(false);
        ExpectSeeded(local, ProgressPath, "{\"progress\":10}");
        await CloudSyncCoordinator.UndoSaveRecoveryAsync(
            local,
            CancellationToken.None
        ).ConfigureAwait(false);
    }

    private static async Task QuarantineAndContextMismatchAsync()
    {
        var local = new InMemoryCloudSaveStore("quarantine-local");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":7}");
        var other = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            SaveContext.Create(
                InMemoryCloudSaveStore.DefaultSteamId64 + 1,
                SaveNamespace.Vanilla,
                SteamGameBranch.Beta,
                ""
            ),
            "other-context",
            CancellationToken.None
        ).ConfigureAwait(false);
        var quarantined = await CloudSyncCoordinator.WriteImportedSnapshotAsync(
            local,
            CloudSyncCoordinator.RecoveryQuarantineDirectory,
            other.Snapshot,
            CancellationToken.None
        ).ConfigureAwait(false);
        var before = ReadBytes(local, ProgressPath);

        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                quarantined.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Beta,
                "",
                InMemoryCloudSaveStore.DefaultSteamId64,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        await ExpectThrowsAsync<InvalidOperationException>(() =>
            CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                other.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                InMemoryCloudSaveStore.DefaultSteamId64,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        ExpectBytes(local, ProgressPath, before);
        Expect(!CloudSyncCoordinator.HasSaveRecoverySyncHold(local));
    }

    private static async Task<(InMemoryCloudSaveStore Local, InMemoryCloudSaveStore Cloud)>
        RestoredFixtureAsync(string name)
    {
        var local = new InMemoryCloudSaveStore($"{name}-local");
        var cloud = new InMemoryCloudSaveStore($"{name}-cloud");
        local.Seed(ProfilePath, "{\"profile\":1}");
        local.Seed(ProgressPath, "{\"progress\":10}");
        var source = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            Context(),
            $"{name}-source",
            CancellationToken.None
        ).ConfigureAwait(false);
        local.Seed(ProgressPath, "{\"progress\":7}");
        await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
            local,
            source.Path,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            InMemoryCloudSaveStore.DefaultSteamId64,
            CancellationToken.None
        ).ConfigureAwait(false);
        return (local, cloud);
    }

    private static CloudSyncCoordinator.AutomaticSaveSnapshot PartialSnapshot(
        string path,
        byte[] bytes,
        params string[] ignoredTombstonePaths
    )
    {
        var content = CloudSyncCoordinator.DecodeAutomaticSnapshotBytes(
            bytes,
            path
        );
        var entries = new List<
            CloudSyncCoordinator.AutomaticSaveManifestEntry
        >
        {
            new()
            {
                Path = path,
                Exists = true,
                Sha256 = AutomaticSyncHash.Compute(content),
            },
        };
        entries.AddRange(ignoredTombstonePaths.Select(tombstonePath =>
            new CloudSyncCoordinator.AutomaticSaveManifestEntry
            {
                Path = tombstonePath,
                Exists = false,
                Sha256 = "",
            }
        ));
        return new CloudSyncCoordinator.AutomaticSaveSnapshot
        {
            ContextMarker = "",
            Coverage = "partial",
            SourceKind = "legacy-test",
            SourceLabel = "unknown legacy backup",
            CapturedUtc = DateTimeOffset.UtcNow,
            Manifest = CloudSyncCoordinator.AutomaticSaveManifest.Create(
                entries
            ),
            Files = new()
            {
                new CloudSyncCoordinator.AutomaticSaveContentEntry
                {
                    Path = path,
                    ContentBase64 = Convert.ToBase64String(bytes),
                    ByteSha256 = AutomaticSyncHash.Compute(bytes),
                },
            },
        };
    }

    private static SaveContext Context()
        => SaveContext.Create(
            InMemoryCloudSaveStore.DefaultSteamId64,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            ""
        );

    private static byte[] SpecialBytes(string content)
        => Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(content))
            .ToArray();

    private static byte[] ReadBytes(InMemoryCloudSaveStore store, string path)
    {
        Expect(store.TryReadSeededBytes(path, out var content));
        return content;
    }

    private static void ExpectBytes(
        InMemoryCloudSaveStore store,
        string path,
        byte[] expected
    )
        => Expect(ReadBytes(store, path).AsSpan().SequenceEqual(expected));

    private static void ExpectSeeded(
        InMemoryCloudSaveStore store,
        string path,
        string expected
    )
        => Expect(
            store.TryReadSeeded(path, out var content)
                && string.Equals(content, expected, StringComparison.Ordinal)
        );

    private static async Task ExpectThrowsAsync<T>(Func<Task> action)
        where T : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(
            $"Expected {typeof(T).Name} was not thrown"
        );
    }

    private static async Task ExpectRecoveryRefusalAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex) when (
            ex is InvalidDataException or InvalidOperationException
        )
        {
            return;
        }
        throw new InvalidOperationException(
            "Expected recovery metadata or context refusal was not thrown"
        );
    }

    private static async Task RunAsync(string name, Func<Task> scenario)
    {
        try
        {
            await scenario().ConfigureAwait(false);
            _passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"FAIL {name}: {ex.Message}",
                ex
            );
        }
    }

    private static void Expect(
        bool condition,
        [System.Runtime.CompilerServices.CallerArgumentExpression(
            nameof(condition)
        )]
        string expression = ""
    )
    {
        if (!condition)
            throw new InvalidOperationException(expression);
    }
}
