#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const string SaveRecoveryUndoDirectory =
        ".sts2-launcher/recovery/undo";
    private const string SaveRecoveryAppliedDirectory =
        ".sts2-launcher/recovery/applied";

    internal static Task<SaveRecoveryOperationResult> RestoreSaveSnapshotAsync(
        string snapshotPath,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        ulong? targetSteamId64,
        CancellationToken cancellationToken = default
    )
        => RestoreSaveSnapshotAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            snapshotPath,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            targetSteamId64,
            cancellationToken
        );

    internal static Task<SaveRecoveryOperationResult>
        RestoreSaveSnapshotAsync(
            ISaveStore local,
            string snapshotPath,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            ulong? targetSteamId64,
            CancellationToken cancellationToken
        )
        => RunWithSaveTransferAndRecoveryGateAsync(
            () => RestoreSaveSnapshotCoreAsync(
                local,
                snapshotPath,
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                targetSteamId64,
                cancellationToken
            ),
            cancellationToken
        );

    private static async Task<SaveRecoveryOperationResult>
        RestoreSaveSnapshotCoreAsync(
            ISaveStore local,
            string snapshotPath,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            ulong? targetSteamId64,
            CancellationToken cancellationToken
        )
    {
        ArgumentNullException.ThrowIfNull(local);
        cancellationToken.ThrowIfCancellationRequested();
        if (HasPendingAutomaticSync(local))
        {
            throw new InvalidOperationException(
                "Restore is blocked until the pending automatic save sync is resolved."
            );
        }
        var existingRecovery = InspectSaveRecoveryStatus(local);
        if (existingRecovery.SyncHeld || existingRecovery.CanUndo)
        {
            throw new InvalidOperationException(
                "A previous Restore is still active. Undo it before restoring another snapshot; nested recovery is intentionally not supported."
            );
        }

        var selection = NormalizeRecoverySelection(
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            targetSteamId64
        );
        var sourcePath = RequireRestorableSnapshotPath(snapshotPath);
        var source = await ReadRecoveryOrRetainedSnapshotAsync(
            local,
            sourcePath,
            cancellationToken
        ).ConfigureAwait(false);
        RequireSnapshotMatchesRecoverySelection(
            source,
            selection
        );
        if (!SnapshotExactContext(source).HasValue
            && selection.ExactContext.HasValue)
        {
            // A selected or inferred account cannot manufacture provenance for
            // an unknown snapshot. It remains local-only and unapprovable.
            selection = NormalizeRecoverySelection(
                selection.Namespace,
                selection.RuntimeIdentity,
                selection.ModSetFingerprint,
                steamId64: null
            );
        }
        if (!source.Manifest.Entries.Any(entry => entry.Exists))
            throw new InvalidDataException("Recovery snapshot contains no save files");

        // Always pin the source inside recovery storage. Automatic snapshots
        // may later be pruned, but an active Restore/Undo journal must never
        // lose one of its byte-for-byte inputs.
        var durableSource = await WriteImportedSnapshotAsync(
            local,
            RecoveryImportedDirectory,
            source,
            cancellationToken
        ).ConfigureAwait(false);
        sourcePath = durableSource.Path;
        source = durableSource.Snapshot;
        var sourceSha256 = durableSource.Sha256;
        var undoSnapshot = await CaptureRecoveryLocalSnapshotAsync(
            local,
            selection.ExactContext,
            selection,
            "pre-restore-undo",
            cancellationToken
        ).ConfigureAwait(false);
        var undo = await WriteImportedSnapshotAsync(
            local,
            SaveRecoveryUndoDirectory,
            undoSnapshot,
            cancellationToken
        ).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow.ToString("O");
        var journal = new SaveRecoveryJournalDocument
        {
            Phase = SaveRecoveryRestoringPhase,
            SaveNamespace = RecoveryNamespaceName(selection.Namespace),
            RuntimeIdentity = selection.RuntimeIdentity,
            ModSetFingerprint = selection.ModSetFingerprint,
            TargetContextMarker =
                selection.ExactContext?.SerializeMarker() ?? "",
            SourceSnapshotPath = sourcePath,
            SourceSnapshotSha256 = sourceSha256,
            UndoSnapshotPath = undo.Path,
            UndoSnapshotSha256 = undo.Sha256,
            AppliedManifest = AutomaticSaveManifest.Create(
                undo.Snapshot.Manifest.Entries
            ),
            CreatedUtc = now,
            UpdatedUtc = now,
        };
        await WriteRecoveryJournalAsync(
            local,
            journal,
            cancellationToken
        ).ConfigureAwait(false);

        // Re-read the complete live allowlist after the durable intent is in
        // place and immediately before the first live-file mutation.
        var preApply = await CaptureRecoveryLocalSnapshotAsync(
            local,
            selection.ExactContext,
            selection,
            "pre-restore-mutation-check",
            cancellationToken
        ).ConfigureAwait(false);
        if (!RecoverySnapshotsByteEqual(preApply, undo.Snapshot))
        {
            // No live save write has happened yet. Close the durable intent so
            // a later Undo cannot mistake the independently changed files for
            // an interrupted Restore and overwrite them.
            journal.Phase = SaveRecoveryUndonePhase;
            journal.AppliedSnapshotPath = "";
            journal.AppliedSnapshotSha256 = "";
            journal.AppliedManifest = AutomaticSaveManifest.Create(
                preApply.Manifest.Entries
            );
            journal.UpdatedUtc = DateTimeOffset.UtcNow.ToString("O");
            await WriteRecoveryJournalAsync(
                local,
                journal,
                cancellationToken
            ).ConfigureAwait(false);
            throw new InvalidOperationException(
                "Restore stopped because Android saves changed while the Undo snapshot was being prepared."
            );
        }

        await ApplyRecoverySnapshotAsync(
            local,
            source,
            selection.Namespace,
            cancellationToken
        ).ConfigureAwait(false);

        var appliedSnapshot = await CaptureRecoveryLocalSnapshotAsync(
            local,
            selection.ExactContext,
            selection,
            "post-restore-validation",
            cancellationToken
        ).ConfigureAwait(false);
        RequireRecoveryAppliedAsRequested(
            source,
            undo.Snapshot,
            appliedSnapshot
        );
        var applied = await WriteImportedSnapshotAsync(
            local,
            SaveRecoveryAppliedDirectory,
            appliedSnapshot,
            cancellationToken
        ).ConfigureAwait(false);

        journal.Phase = SaveRecoveryValidationPhase;
        journal.AppliedSnapshotPath = applied.Path;
        journal.AppliedSnapshotSha256 = applied.Sha256;
        journal.AppliedManifest = AutomaticSaveManifest.Create(
            applied.Snapshot.Manifest.Entries
        );
        journal.UpdatedUtc = DateTimeOffset.UtcNow.ToString("O");
        await WriteRecoveryJournalAsync(
            local,
            journal,
            cancellationToken
        ).ConfigureAwait(false);

        var status = InspectSaveRecoveryStatus(local);
        SaveEvidenceEvents.RecoveryTerminal("restore");
        return new SaveRecoveryOperationResult(
            "Restored on Android only. Steam was not contacted or changed. Open the save locally, validate it, then explicitly approve sync.",
            status
        );
    }

    internal static Task<SaveRecoveryOperationResult> UndoSaveRecoveryAsync(
        CancellationToken cancellationToken = default
    )
        => UndoSaveRecoveryAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            cancellationToken
        );

    internal static Task<SaveRecoveryOperationResult>
        UndoSaveRecoveryAsync(
            ISaveStore local,
            CancellationToken cancellationToken
        )
        => RunWithSaveTransferAndRecoveryGateAsync(
            () => UndoSaveRecoveryCoreAsync(local, cancellationToken),
            cancellationToken
        );

    private static async Task<SaveRecoveryOperationResult>
        UndoSaveRecoveryCoreAsync(
            ISaveStore local,
            CancellationToken cancellationToken
        )
    {
        ArgumentNullException.ThrowIfNull(local);
        var journal = await ReadRecoveryJournalAsync(
            local,
            cancellationToken
        ).ConfigureAwait(false) ?? throw new InvalidOperationException(
            "No local save restore is available to Undo."
        );
        if (journal.Phase == SaveRecoveryUndonePhase)
        {
            await TryDeleteLegacyRecoveryHoldAsync(local, cancellationToken)
                .ConfigureAwait(false);
            return new SaveRecoveryOperationResult(
                "The last local restore is already undone byte-for-byte.",
                InspectSaveRecoveryStatus(local)
            );
        }

        var selection = RecoverySelectionFromJournal(journal);
        var undo = await ReadRecoveryOrRetainedSnapshotAsync(
            local,
            journal.UndoSnapshotPath,
            cancellationToken
        ).ConfigureAwait(false);
        RequireSnapshotDocumentHash(
            journal.UndoSnapshotPath,
            journal.UndoSnapshotSha256
        );

        RequireRecoverySnapshotMatchesSelection(
            undo,
            selection,
            allowUnknownContext: false,
            "Undo"
        );

        AutomaticSaveSnapshot preUndo;
        if (journal.Phase is SaveRecoveryValidationPhase
            or SaveRecoveryApprovedPhase)
        {
            preUndo = await ReadRecoveryOrRetainedSnapshotAsync(
                local,
                journal.AppliedSnapshotPath,
                cancellationToken
            ).ConfigureAwait(false);
            RequireSnapshotDocumentHash(
                journal.AppliedSnapshotPath,
                journal.AppliedSnapshotSha256
            );
            RequireRecoverySnapshotMatchesSelection(
                preUndo,
                selection,
                allowUnknownContext: false,
                "applied"
            );
            var actual = await CaptureRecoveryLocalSnapshotAsync(
                local,
                selection.ExactContext,
                selection,
                "pre-undo-drift-check",
                cancellationToken
            ).ConfigureAwait(false);
            if (!RecoverySnapshotsByteEqual(actual, preUndo))
            {
                throw new InvalidOperationException(
                    "Undo stopped because Android saves changed after the restore. Export them before choosing another recovery action."
                );
            }
        }
        else if (journal.Phase == SaveRecoveryRestoringPhase)
        {
            var source = await ReadRecoveryOrRetainedSnapshotAsync(
                local,
                journal.SourceSnapshotPath,
                cancellationToken
            ).ConfigureAwait(false);
            RequireSnapshotDocumentHash(
                journal.SourceSnapshotPath,
                journal.SourceSnapshotSha256
            );
            RequireRecoverySnapshotMatchesSelection(
                source,
                selection,
                allowUnknownContext: true,
                "source"
            );
            preUndo = await CaptureRecoveryLocalSnapshotAsync(
                local,
                selection.ExactContext,
                selection,
                "interrupted-restore-pre-undo",
                cancellationToken
            ).ConfigureAwait(false);
            if (!RecoverySnapshotIsPathwiseBlend(preUndo, undo, source))
            {
                throw new InvalidOperationException(
                    "Undo stopped because Android saves contain changes that are neither the pre-restore bytes nor the interrupted Restore bytes. Export them before choosing another recovery action."
                );
            }

            var retainedPreUndo = await WriteImportedSnapshotAsync(
                local,
                SaveRecoveryAppliedDirectory,
                preUndo,
                cancellationToken
            ).ConfigureAwait(false);
            preUndo = retainedPreUndo.Snapshot;
            journal.AppliedSnapshotPath = retainedPreUndo.Path;
            journal.AppliedSnapshotSha256 = retainedPreUndo.Sha256;
            journal.AppliedManifest = AutomaticSaveManifest.Create(
                preUndo.Manifest.Entries
            );
        }
        else if (journal.Phase == SaveRecoveryUndoingPhase)
        {
            preUndo = await ReadRecoveryOrRetainedSnapshotAsync(
                local,
                journal.AppliedSnapshotPath,
                cancellationToken
            ).ConfigureAwait(false);
            RequireSnapshotDocumentHash(
                journal.AppliedSnapshotPath,
                journal.AppliedSnapshotSha256
            );
            RequireRecoverySnapshotMatchesSelection(
                preUndo,
                selection,
                allowUnknownContext: false,
                "pre-Undo"
            );
            var current = await CaptureRecoveryLocalSnapshotAsync(
                local,
                selection.ExactContext,
                selection,
                "interrupted-undo-retry-check",
                cancellationToken
            ).ConfigureAwait(false);
            if (!RecoverySnapshotIsPathwiseBlend(current, preUndo, undo))
            {
                throw new InvalidOperationException(
                    "Undo retry stopped because Android saves contain changes outside the interrupted Undo. Export them before choosing another recovery action."
                );
            }
        }
        else
        {
            throw new InvalidDataException(
                "Recovery journal cannot be undone from its current phase"
            );
        }

        if (journal.Phase != SaveRecoveryUndoingPhase)
        {
            journal.Phase = SaveRecoveryUndoingPhase;
            journal.UpdatedUtc = DateTimeOffset.UtcNow.ToString("O");
            await WriteRecoveryJournalAsync(
                local,
                journal,
                cancellationToken
            ).ConfigureAwait(false);

            var immediatelyBeforeUndo = await CaptureRecoveryLocalSnapshotAsync(
                local,
                selection.ExactContext,
                selection,
                "immediate-pre-undo-mutation-check",
                cancellationToken
            ).ConfigureAwait(false);
            if (!RecoverySnapshotsByteEqual(immediatelyBeforeUndo, preUndo))
            {
                throw new InvalidOperationException(
                    "Undo stopped because Android saves changed immediately before the first Undo write."
                );
            }
        }

        await ApplyRecoverySnapshotAsync(
            local,
            undo,
            selection.Namespace,
            cancellationToken
        ).ConfigureAwait(false);
        var restored = await CaptureRecoveryLocalSnapshotAsync(
            local,
            selection.ExactContext,
            selection,
            "post-undo-verification",
            cancellationToken
        ).ConfigureAwait(false);
        if (!RecoverySnapshotsByteEqual(restored, undo))
        {
            throw new IOException(
                "Undo verification failed; Steam sync remains blocked"
            );
        }

        journal.Phase = SaveRecoveryUndonePhase;
        journal.AppliedManifest = AutomaticSaveManifest.Create(
            restored.Manifest.Entries
        );
        journal.UpdatedUtc = DateTimeOffset.UtcNow.ToString("O");
        await WriteRecoveryJournalAsync(
            local,
            journal,
            cancellationToken
        ).ConfigureAwait(false);
        await TryDeleteLegacyRecoveryHoldAsync(local, cancellationToken)
            .ConfigureAwait(false);

        var status = InspectSaveRecoveryStatus(local);
        SaveEvidenceEvents.RecoveryTerminal("undo");
        return new SaveRecoveryOperationResult(
            "Undo completed byte-for-byte on Android. Steam was not contacted or changed.",
            status
        );
    }

    internal static Task<SaveRecoveryOperationResult>
        ApproveSaveRecoveryForSyncAsync(
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            ulong steamId64,
            CancellationToken cancellationToken = default
        )
        => ApproveSaveRecoveryForSyncAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            steamId64,
            cancellationToken
        );

    internal static Task<SaveRecoveryOperationResult>
        ApproveSaveRecoveryForSyncAsync(
            ISaveStore local,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            ulong steamId64,
            CancellationToken cancellationToken
        )
        => RunWithSaveTransferAndRecoveryGateAsync(
            () => ApproveSaveRecoveryForSyncCoreAsync(
                local,
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                steamId64,
                cancellationToken
            ),
            cancellationToken
        );

    private static async Task<SaveRecoveryOperationResult>
        ApproveSaveRecoveryForSyncCoreAsync(
            ISaveStore local,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            ulong steamId64,
            CancellationToken cancellationToken
        )
    {
        ArgumentNullException.ThrowIfNull(local);
        var selected = NormalizeRecoverySelection(
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            steamId64
        );
        var journal = await ReadRecoveryJournalAsync(
            local,
            cancellationToken
        ).ConfigureAwait(false) ?? throw new InvalidDataException(
            "The local recovery journal is missing"
        );
        var recorded = RecoverySelectionFromJournal(journal);
        var recordedContext = recorded.ExactContext
            ?? throw new InvalidOperationException(
                "This recovery has unknown Steam-account provenance and cannot be approved for synchronization."
            );
        var selectedContext = selected.ExactContext
            ?? throw new InvalidOperationException(
                "A verified Steam account is required before approval."
            );
        recordedContext.RequireExactMatch(selectedContext);
        if (journal.Phase == SaveRecoveryApprovedPhase)
        {
            await RequireSaveRecoverySyncReadyAsync(local, cancellationToken)
                .ConfigureAwait(false);
            await TryDeleteLegacyRecoveryHoldAsync(local, cancellationToken)
                .ConfigureAwait(false);
            return new SaveRecoveryOperationResult(
                "Recovered Android saves are already approved for this exact Steam account and save context.",
                InspectSaveRecoveryStatus(local)
            );
        }
        if (!CanLaunchLocalRecoveryValidation(
                local,
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                out var problem
            ))
        {
            throw new InvalidOperationException(problem);
        }

        if (journal.Phase != SaveRecoveryValidationPhase)
        {
            throw new InvalidOperationException(
                "Recovered saves are not ready for approval"
            );
        }

        await VerifyRecoveryJournalSnapshotAsync(
            local,
            journal.SourceSnapshotPath,
            journal.SourceSnapshotSha256,
            recordedContext,
            requireExactContext: false,
            cancellationToken
        ).ConfigureAwait(false);
        await VerifyRecoveryJournalSnapshotAsync(
            local,
            journal.UndoSnapshotPath,
            journal.UndoSnapshotSha256,
            recordedContext,
            requireExactContext: true,
            cancellationToken
        ).ConfigureAwait(false);
        await VerifyRecoveryJournalSnapshotAsync(
            local,
            journal.AppliedSnapshotPath,
            journal.AppliedSnapshotSha256,
            recordedContext,
            requireExactContext: true,
            cancellationToken
        ).ConfigureAwait(false);

        var previousApplied = await ReadRecoveryOrRetainedSnapshotAsync(
            local,
            journal.AppliedSnapshotPath,
            cancellationToken
        ).ConfigureAwait(false);
        var current = await CaptureRecoveryLocalSnapshotAsync(
            local,
            selected.ExactContext,
            selected,
            "approved-after-local-validation",
            cancellationToken
        ).ConfigureAwait(false);
        var approved = await WriteImportedSnapshotAsync(
            local,
            SaveRecoveryAppliedDirectory,
            current,
            cancellationToken
        ).ConfigureAwait(false);

        journal.Phase = SaveRecoveryApprovedPhase;
        journal.AppliedSnapshotPath = approved.Path;
        journal.AppliedSnapshotSha256 = approved.Sha256;
        journal.AppliedManifest = AutomaticSaveManifest.Create(
            approved.Snapshot.Manifest.Entries
        );
        journal.UpdatedUtc = DateTimeOffset.UtcNow.ToString("O");
        await WriteRecoveryJournalAsync(
            local,
            journal,
            cancellationToken
        ).ConfigureAwait(false);
        await TryDeleteLegacyRecoveryHoldAsync(local, cancellationToken)
            .ConfigureAwait(false);

        var status = InspectSaveRecoveryStatus(local);
        return new SaveRecoveryOperationResult(
            "Recovered Android saves were re-read and approved. A later sync may upload them only after the normal account/context checks pass.",
            status
        );
    }

    private static async Task<AutomaticSaveSnapshot>
        CaptureRecoveryLocalSnapshotAsync(
            ISaveStore local,
            SaveContext? exactContext,
            SaveRecoverySelection selection,
            string sourceLabel,
            CancellationToken cancellationToken
        )
    {
        var entries = new List<AutomaticSaveManifestEntry>();
        var files = new List<AutomaticSaveContentEntry>();
        Task<bool> Exists(string path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(local.FileExists(path));
        }
        Task<byte[]> ReadBytes(string path)
            => CancellableSaveStore.ReadBytesAsync(
                local,
                path,
                cancellationToken
            );

        await CaptureAutomaticPathAsync(
            SaveTransferAllowlist.SharedProfilePath,
            entries,
            files,
            Exists,
            ReadBytes
        ).ConfigureAwait(false);
        for (var profileId = 1;
            profileId <= SaveTransferAllowlist.ProfileCount;
            profileId++)
        {
            var directory = SaveTransferAllowlist.SaveDirectory(
                selection.Namespace,
                profileId
            );
            foreach (var filename in SaveTransferAllowlist.FixedProfileFiles)
            {
                await CaptureAutomaticPathAsync(
                    $"{directory}/{filename}",
                    entries,
                    files,
                    Exists,
                    ReadBytes
                ).ConfigureAwait(false);
            }

            var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
                selection.Namespace,
                profileId
            );
            var historyFiles = GetLocalSnapshotHistoryFiles(
                    local,
                    historyDirectory,
                    cancellationToken
                )
                .Where(SaveTransferAllowlist.IsAllowedHistoryFilename)
                .ToArray();
            RequireNoCaseFoldCollidingHistoryNames(
                historyDirectory,
                historyFiles
            );
            historyFiles = historyFiles
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(path => path, StringComparer.Ordinal)
                .ToArray();
            foreach (var filename in historyFiles)
            {
                await CaptureAutomaticPathAsync(
                    $"{historyDirectory}/{filename}",
                    entries,
                    files,
                    Exists,
                    ReadBytes,
                    required: true
                ).ConfigureAwait(false);
            }
        }

        return new AutomaticSaveSnapshot
        {
            ContextMarker = exactContext?.SerializeMarker() ?? "",
            Coverage = "full",
            SourceKind = "local-recovery",
            SourceLabel = sourceLabel,
            CapturedUtc = DateTimeOffset.UtcNow,
            Manifest = AutomaticSaveManifest.Create(entries),
            Files = files
                .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => file.Path, StringComparer.Ordinal)
                .ToList(),
        };
    }

    private static async Task ApplyRecoverySnapshotAsync(
        ISaveStore local,
        AutomaticSaveSnapshot snapshot,
        SaveNamespace saveNamespace,
        CancellationToken cancellationToken
    )
    {
        var localHistoryFiles = new Dictionary<int, IReadOnlyList<string>>();
        for (var profileId = 1;
            profileId <= SaveTransferAllowlist.ProfileCount;
            profileId++)
        {
            var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
                saveNamespace,
                profileId
            );
            var historyFiles = GetLocalSnapshotHistoryFiles(
                    local,
                    historyDirectory,
                    cancellationToken
                )
                .Where(SaveTransferAllowlist.IsAllowedHistoryFilename)
                .ToArray();
            RequireNoCaseFoldCollidingHistoryNames(
                historyDirectory,
                historyFiles
            );
            localHistoryFiles.Add(profileId, historyFiles);

            var historyPrefix = historyDirectory + "/";
            var sourceHistoryFiles = snapshot.Manifest.Entries
                .Where(entry => entry.Exists
                    && entry.Path.StartsWith(
                        historyPrefix,
                        StringComparison.OrdinalIgnoreCase
                    ))
                .Select(entry => entry.Path[historyPrefix.Length..])
                .Where(SaveTransferAllowlist.IsAllowedHistoryFilename);
            RequireNoCaseFoldCollidingHistoryNames(
                historyDirectory,
                historyFiles.Concat(sourceHistoryFiles).ToArray()
            );
        }

        var full = string.Equals(
            snapshot.Coverage,
            "full",
            StringComparison.Ordinal
        );
        foreach (var entry in snapshot.Manifest.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!SaveTransferAllowlist.IsAllowedPath(saveNamespace, entry.Path))
            {
                throw new InvalidDataException(
                    $"Recovery snapshot path does not belong to the selected save namespace: {entry.Path}"
                );
            }

            if (!entry.Exists)
            {
                if (full)
                {
                    await DeleteAndVerifyRecoveryPathAsync(
                        local,
                        entry.Path,
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                continue;
            }

            var bytes = ReadSnapshotFileBytes(snapshot, entry.Path);
            await CancellableSaveStore.WriteRecoveryBytesAsync(
                local,
                entry.Path,
                bytes,
                cancellationToken
            ).ConfigureAwait(false);
            var verified = await CancellableSaveStore.ReadBytesAsync(
                local,
                entry.Path,
                cancellationToken
            ).ConfigureAwait(false);
            if (!string.Equals(
                    AutomaticSyncHash.Compute(bytes),
                    AutomaticSyncHash.Compute(verified),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                throw new IOException(
                    $"Recovered save failed byte-for-byte verification: {entry.Path}"
                );
            }
        }

        if (!full)
            return;
        var retainedPaths = snapshot.Manifest.Entries
            .Where(entry => entry.Exists)
            .Select(entry => entry.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var profileId = 1;
            profileId <= SaveTransferAllowlist.ProfileCount;
            profileId++)
        {
            var historyDirectory = SaveTransferAllowlist.HistoryDirectory(
                saveNamespace,
                profileId
            );
            foreach (var filename in localHistoryFiles[profileId])
            {
                var path = $"{historyDirectory}/{filename}";
                if (!retainedPaths.Contains(path))
                {
                    await DeleteAndVerifyRecoveryPathAsync(
                        local,
                        path,
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }
        }
    }

    private static async Task DeleteAndVerifyRecoveryPathAsync(
        ISaveStore local,
        string path,
        CancellationToken cancellationToken
    )
    {
        if (local.FileExists(path))
        {
            await CancellableSaveStore.DeleteRecoveryFileAsync(
                local,
                path,
                cancellationToken
            ).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (local.FileExists(path))
            throw new IOException($"Recovered save tombstone failed: {path}");
    }

    private static void RequireNoCaseFoldCollidingHistoryNames(
        string historyDirectory,
        IReadOnlyList<string> filenames
    )
    {
        var collision = filenames
            .GroupBy(filename => filename, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .Distinct(StringComparer.Ordinal)
                .OrderBy(filename => filename, StringComparer.Ordinal)
                .ToArray())
            .FirstOrDefault(names => names.Length > 1);
        if (collision is null)
            return;

        throw new InvalidDataException(
            $"Recovery stopped before changing saves because {historyDirectory} contains filenames that differ only by letter case: {string.Join(", ", collision)}"
        );
    }

    private static void RequireSnapshotMatchesRecoverySelection(
        AutomaticSaveSnapshot snapshot,
        SaveRecoverySelection selection
    )
    {
        SaveContext? exact = SnapshotExactContext(snapshot);
        if (exact.HasValue)
        {
            var targetContext = selection.ExactContext
                ?? throw new InvalidOperationException(
                    "This snapshot has exact Steam-account provenance, but no selected Steam account is known. Scan again after the launcher can identify the account."
                );
            try
            {
                targetContext.RequireExactMatch(exact.Value);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    "Recovery snapshot belongs to another Steam account, save namespace, game version, or mod set.",
                    ex
                );
            }
        }

        foreach (var entry in snapshot.Manifest.Entries)
        {
            if (!SaveTransferAllowlist.IsAllowedPath(
                    selection.Namespace,
                    entry.Path
                ))
            {
                throw new InvalidOperationException(
                    "Unknown-context recovery files do not belong to the selected vanilla/modded save paths."
                );
            }
            if (!IsCanonicalRecoveryPath(selection.Namespace, entry.Path))
            {
                throw new InvalidOperationException(
                    "Recovery file paths must use the launcher's canonical save-directory and fixed-file spelling."
                );
            }
        }
    }

    private static bool IsCanonicalRecoveryPath(
        SaveNamespace saveNamespace,
        string path
    )
    {
        var canonical = CloudSavePath.Relative(path);
        if (string.Equals(
                canonical,
                SaveTransferAllowlist.SharedProfilePath,
                StringComparison.Ordinal
            ))
        {
            return true;
        }

        for (var profileId = 1;
            profileId <= SaveTransferAllowlist.ProfileCount;
            profileId++)
        {
            var saveDirectory = SaveTransferAllowlist.SaveDirectory(
                saveNamespace,
                profileId
            );
            if (SaveTransferAllowlist.FixedProfileFiles.Any(filename =>
                    string.Equals(
                        canonical,
                        $"{saveDirectory}/{filename}",
                        StringComparison.Ordinal
                    )
                ))
            {
                return true;
            }

            var historyPrefix = SaveTransferAllowlist.HistoryDirectory(
                saveNamespace,
                profileId
            ) + "/";
            if (canonical.StartsWith(historyPrefix, StringComparison.Ordinal)
                && SaveTransferAllowlist.IsAllowedHistoryFilename(
                    canonical[historyPrefix.Length..]
                ))
            {
                return true;
            }
        }

        return false;
    }

    private static SaveContext? SnapshotExactContext(
        AutomaticSaveSnapshot snapshot
    )
        => string.IsNullOrWhiteSpace(snapshot.ContextMarker)
            ? null
            : SaveContext.ParseMarker(snapshot.ContextMarker);

    private static void RequireRecoveryAppliedAsRequested(
        AutomaticSaveSnapshot requested,
        AutomaticSaveSnapshot before,
        AutomaticSaveSnapshot actual
    )
    {
        if (requested.Coverage == "full")
        {
            if (!RecoverySnapshotsByteEqual(requested, actual))
            {
                throw new IOException(
                    "Restored Android saves do not match the full recovery snapshot"
                );
            }
            return;
        }

        if (!RecoverySnapshotMatchesDeterministicTarget(
                actual,
                before,
                requested
            ))
        {
            throw new IOException(
                "Restored Android saves do not match the exact partial recovery overlay; an unrelated local save changed during Restore"
            );
        }
    }

    private static bool RecoverySnapshotsByteEqual(
        AutomaticSaveSnapshot first,
        AutomaticSaveSnapshot second
    )
    {
        if (!first.Manifest.ContentEquals(second.Manifest))
            return false;
        foreach (var entry in first.Manifest.Entries.Where(entry => entry.Exists))
        {
            if (!ReadSnapshotFileBytes(first, entry.Path)
                    .AsSpan()
                    .SequenceEqual(ReadSnapshotFileBytes(second, entry.Path)))
            {
                return false;
            }
        }
        return true;
    }

    private readonly record struct RecoveryRawFileState(
        bool Exists,
        string Sha256
    )
    {
        internal static RecoveryRawFileState Missing => new(false, "");
    }

    private static bool RecoverySnapshotIsPathwiseBlend(
        AutomaticSaveSnapshot actual,
        AutomaticSaveSnapshot before,
        AutomaticSaveSnapshot target
    )
    {
        var paths = actual.Manifest.Entries.Select(entry => entry.Path)
            .Concat(before.Manifest.Entries.Select(entry => entry.Path))
            .Concat(target.Manifest.Entries.Select(entry => entry.Path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            var actualState = RecoveryRawState(actual, path);
            var beforeState = RecoveryRawState(before, path);
            var effectiveTarget = RecoveryEffectiveTargetState(
                before,
                target,
                path
            );
            if (!actualState.Equals(beforeState)
                && !actualState.Equals(effectiveTarget))
            {
                return false;
            }
        }

        return true;
    }

    private static bool RecoverySnapshotMatchesDeterministicTarget(
        AutomaticSaveSnapshot actual,
        AutomaticSaveSnapshot before,
        AutomaticSaveSnapshot target
    )
    {
        var paths = actual.Manifest.Entries.Select(entry => entry.Path)
            .Concat(before.Manifest.Entries.Select(entry => entry.Path))
            .Concat(target.Manifest.Entries.Select(entry => entry.Path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            var expected = RecoveryEffectiveTargetState(before, target, path);
            if (!RecoveryRawState(actual, path).Equals(expected))
                return false;
        }

        return true;
    }

    private static RecoveryRawFileState RecoveryEffectiveTargetState(
        AutomaticSaveSnapshot before,
        AutomaticSaveSnapshot target,
        string path
    )
    {
        var targetEntry = target.Manifest.Entries.FirstOrDefault(entry =>
            string.Equals(
                entry.Path,
                path,
                StringComparison.OrdinalIgnoreCase
            )
        );
        // Partial recovery is an overlay: absent paths and tombstones are
        // intentionally left alone by ApplyRecoverySnapshotAsync.
        if (string.Equals(
                target.Coverage,
                "partial",
                StringComparison.Ordinal
            )
            && (targetEntry is null || !targetEntry.Exists))
        {
            return RecoveryRawState(before, path);
        }
        return RecoveryRawState(target, path);
    }

    private static RecoveryRawFileState RecoveryRawState(
        AutomaticSaveSnapshot snapshot,
        string path
    )
    {
        var entry = snapshot.Manifest.Entries.FirstOrDefault(candidate =>
            string.Equals(
                candidate.Path,
                path,
                StringComparison.OrdinalIgnoreCase
            )
        );
        if (entry is null || !entry.Exists)
            return RecoveryRawFileState.Missing;
        return new RecoveryRawFileState(
            true,
            AutomaticSyncHash.Compute(ReadSnapshotFileBytes(snapshot, path))
        );
    }

    private static void RequireRecoverySnapshotMatchesSelection(
        AutomaticSaveSnapshot snapshot,
        SaveRecoverySelection selection,
        bool allowUnknownContext,
        string label
    )
    {
        var actual = SnapshotExactContext(snapshot);
        var target = selection.ExactContext;
        if (actual.HasValue)
        {
            if (!target.HasValue)
            {
                throw new InvalidDataException(
                    $"Recovery {label} snapshot has an exact account but the recovery target is unknown"
                );
            }
            try
            {
                target.Value.RequireExactMatch(actual.Value);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException(
                    $"Recovery {label} snapshot belongs to another account or save context",
                    ex
                );
            }
            return;
        }

        if (target.HasValue && !allowUnknownContext)
        {
            throw new InvalidDataException(
                $"Recovery {label} snapshot has no exact target account context"
            );
        }
    }

    private static string RequireRestorableSnapshotPath(string path)
    {
        var canonical = CloudSavePath.Relative(path);
        if (canonical.StartsWith(
                ".sts2-launcher/recovery/quarantine/",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            throw new InvalidOperationException(
                "This recovery copy belongs to another Steam account and is quarantined."
            );
        }
        if (!canonical.StartsWith(
                ".sts2-launcher/recovery/",
                StringComparison.OrdinalIgnoreCase
            )
            && !canonical.StartsWith(
                ".sts2-launcher/automatic-sync/contexts/",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            throw new InvalidDataException(
                "Restore source is outside verified launcher snapshot storage"
            );
        }
        _ = SnapshotShaFromPath(canonical);
        return canonical;
    }

    private static async Task<AutomaticSaveSnapshot>
        ReadRecoveryOrRetainedSnapshotAsync(
            ISaveStore local,
            string path,
            CancellationToken cancellationToken
        )
    {
        var canonical = RequireRestorableSnapshotPath(path);
        var untrusted = await ReadAutomaticSyncDocumentAsync<
            AutomaticSaveSnapshot
        >(
            local,
            canonical,
            cancellationToken
        ).ConfigureAwait(false) ?? throw new FileNotFoundException(
            "Recovery snapshot is missing",
            canonical
        );
        SaveContext? exact = string.IsNullOrWhiteSpace(untrusted.ContextMarker)
            ? null
            : SaveContext.ParseMarker(untrusted.ContextMarker);
        if (canonical.StartsWith(
                ".sts2-launcher/recovery/",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return await ReadVerifiedImportedSnapshotAsync(
                local,
                canonical,
                exact,
                cancellationToken
            ).ConfigureAwait(false);
        }

        if (!exact.HasValue)
        {
            throw new InvalidDataException(
                "Retained automatic snapshot has no exact save context"
            );
        }
        return await ReadVerifiedSnapshotAsync(
            local,
            canonical,
            exact,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static string SnapshotShaFromPath(string path)
    {
        var normalized = path.Replace('/', Path.DirectorySeparatorChar);
        var sha256 = Path.GetFileNameWithoutExtension(normalized);
        if (!IsSha256(sha256))
            throw new InvalidDataException("Recovery snapshot is not content-addressed");
        return sha256;
    }

    private static void RequireSnapshotDocumentHash(
        string path,
        string expectedSha256
    )
    {
        var pathSha = SnapshotShaFromPath(path);
        if (!string.Equals(
                pathSha,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            throw new InvalidDataException(
                "Recovery journal snapshot hash does not match its path"
            );
        }
    }
}
