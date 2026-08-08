#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    // A Stage 4 development build briefly wrote this second state file. It is
    // retained only so async entry points can remove it opportunistically; the
    // journal below is the sole authoritative recovery state machine.
    internal const string SaveRecoveryHoldPath =
        ".sts2-launcher/recovery/sync-hold.json";
    internal const string SaveRecoveryJournalPath =
        ".sts2-launcher/recovery/last-restore.json";

    private const int SaveRecoveryDocumentVersion = 2;
    private const string SaveRecoveryRestoringPhase = "restoring";
    private const string SaveRecoveryValidationPhase = "validation-required";
    private const string SaveRecoveryUndoingPhase = "undoing";
    private const string SaveRecoveryApprovedPhase = "approved";
    private const string SaveRecoveryUndonePhase = "undone";

    private static readonly SemaphoreSlim SaveTransferAndRecoveryGate =
        new(1, 1);

    private readonly record struct SaveRecoverySelection(
        SaveNamespace Namespace,
        string RuntimeIdentity,
        string ModSetFingerprint,
        ulong? SteamId64
    )
    {
        internal SaveContext? ExactContext
            => SteamId64.HasValue
                ? SaveContext.Create(
                    SteamId64.Value,
                    Namespace,
                    RuntimeIdentity,
                    ModSetFingerprint
                )
                : null;
    }

    private sealed class SaveRecoveryJournalDocument
    {
        public int Version { get; set; } = SaveRecoveryDocumentVersion;
        public string Phase { get; set; } = SaveRecoveryRestoringPhase;
        public string SaveNamespace { get; set; } = "";
        public string RuntimeIdentity { get; set; } = "";
        public string ModSetFingerprint { get; set; } = "";
        public string TargetContextMarker { get; set; } = "";
        public string SourceSnapshotPath { get; set; } = "";
        public string SourceSnapshotSha256 { get; set; } = "";
        public string UndoSnapshotPath { get; set; } = "";
        public string UndoSnapshotSha256 { get; set; } = "";
        public string AppliedSnapshotPath { get; set; } = "";
        public string AppliedSnapshotSha256 { get; set; } = "";
        public AutomaticSaveManifest AppliedManifest { get; set; } = new();
        public string CreatedUtc { get; set; } = "";
        public string UpdatedUtc { get; set; } = "";
    }

    internal static bool HasSaveRecoverySyncHold()
        => HasSaveRecoverySyncHold(CloudSaveStoreFactory.CreateLocalStore());

    internal static bool HasSaveRecoverySyncHold(ISaveStore local)
    {
        ArgumentNullException.ThrowIfNull(local);
        return InspectSaveRecoveryStatus(local).SyncHeld;
    }

    internal static SaveRecoveryStatus InspectSaveRecoveryStatus()
        => InspectSaveRecoveryStatus(CloudSaveStoreFactory.CreateLocalStore());

    internal static SaveRecoveryStatus InspectSaveRecoveryStatus(
        ISaveStore local
    )
    {
        ArgumentNullException.ThrowIfNull(local);
        var hasJournal = local.FileExists(SaveRecoveryJournalPath);
        if (!hasJournal)
            return SaveRecoveryStatus.None;

        try
        {
            var journal = ReadRecoveryJournal(local);
            var selection = RecoverySelectionFromJournal(journal);
            RequireRecoverySnapshotReferencePresent(
                local,
                journal.SourceSnapshotPath,
                journal.SourceSnapshotSha256,
                "source"
            );
            RequireRecoverySnapshotReferencePresent(
                local,
                journal.UndoSnapshotPath,
                journal.UndoSnapshotSha256,
                "Undo"
            );
            if (!string.IsNullOrWhiteSpace(journal.AppliedSnapshotPath))
            {
                RequireRecoverySnapshotReferencePresent(
                    local,
                    journal.AppliedSnapshotPath,
                    journal.AppliedSnapshotSha256,
                    "applied"
                );
            }

            var syncHeld = journal.Phase is not (
                SaveRecoveryApprovedPhase or SaveRecoveryUndonePhase
            );
            var validationRequired = string.Equals(
                journal.Phase,
                SaveRecoveryValidationPhase,
                StringComparison.Ordinal
            );
            var canApprove = validationRequired
                && selection.ExactContext.HasValue;
            var canUndo = journal.Phase != SaveRecoveryUndonePhase;
            var message = journal.Phase switch
            {
                SaveRecoveryRestoringPhase
                    => "A local restore was interrupted. Steam sync is blocked; Undo can safely restore the pre-restore snapshot.",
                SaveRecoveryUndoingPhase
                    => "A local Undo was interrupted. Steam sync is blocked; retry Undo to finish it idempotently.",
                SaveRecoveryValidationPhase when canApprove
                    => "Recovered saves are Android-only until they are validated locally and explicitly approved for this Steam account and save context.",
                SaveRecoveryValidationPhase
                    => "Recovered saves are Android-only. Their Steam account is unknown, so they can be validated locally but cannot be approved or synchronized.",
                SaveRecoveryApprovedPhase
                    => "The recovered saves are approved only for their recorded Steam account, game version, save type, and mod set; Undo remains available.",
                SaveRecoveryUndonePhase
                    => "The last local restore was undone byte-for-byte.",
                _ => throw new InvalidDataException(
                    "Recovery journal has an invalid phase"
                ),
            };
            return new SaveRecoveryStatus(
                syncHeld,
                canUndo,
                validationRequired,
                canApprove,
                selection.SteamId64,
                selection.Namespace,
                selection.RuntimeIdentity,
                selection.ModSetFingerprint,
                message
            );
        }
        catch (Exception ex) when (IsRecoveryMetadataException(ex))
        {
            return UnreadableRecoveryStatus(
                "Recovery metadata is unreadable or incomplete. Steam sync is blocked until it is repaired: "
                    + ex.Message
            );
        }
    }

    internal static bool CanLaunchLocalRecoveryValidation(
        ISaveStore local,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        out string message
    )
    {
        ArgumentNullException.ThrowIfNull(local);
        if (!local.FileExists(SaveRecoveryJournalPath))
        {
            message = "No local recovery validation is pending.";
            return false;
        }

        try
        {
            var journal = ReadRecoveryJournal(local);
            RequireRecoverySnapshotReferencePresent(
                local,
                journal.SourceSnapshotPath,
                journal.SourceSnapshotSha256,
                "source"
            );
            RequireRecoverySnapshotReferencePresent(
                local,
                journal.UndoSnapshotPath,
                journal.UndoSnapshotSha256,
                "Undo"
            );
            RequireRecoverySnapshotReferencePresent(
                local,
                journal.AppliedSnapshotPath,
                journal.AppliedSnapshotSha256,
                "applied"
            );
            var selected = NormalizeRecoverySelection(
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                steamId64: null
            );
            var recorded = RecoverySelectionFromJournal(journal);
            if (!SameLocalRecoverySelection(recorded, selected))
            {
                message =
                    "Recovery validation must use the same vanilla/modded, game-version, and mod-set context as the restore.";
                return false;
            }

            if (journal.Phase != SaveRecoveryValidationPhase)
            {
                message =
                    "The local recovery has not reached the validation phase. Steam sync remains blocked.";
                return false;
            }

            message =
                "Starting with recovered Android saves only. Steam will not be contacted and no automatic sync record will be created.";
            return true;
        }
        catch (Exception ex) when (IsRecoveryMetadataException(ex))
        {
            message =
                "Recovery validation cannot start because its local metadata is unreadable: "
                    + ex.Message;
            return false;
        }
    }

    internal static bool CanLaunchLocalRecoveryValidation(
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        out string message
    )
        => CanLaunchLocalRecoveryValidation(
            CloudSaveStoreFactory.CreateLocalStore(),
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            out message
        );

    private static void RequireSaveRecoverySyncReleased(ISaveStore local)
    {
        var status = InspectSaveRecoveryStatus(local);
        if (status.SyncHeld)
        {
            throw new InvalidOperationException(
                "Steam save synchronization is blocked by local recovery state. "
                    + status.Message
            );
        }
    }

    private static async Task RequireSaveRecoverySyncReadyAsync(
        ISaveStore local,
        CancellationToken cancellationToken
    )
    {
        RequireSaveRecoverySyncReleased(local);
        var journal = await ReadRecoveryJournalAsync(
            local,
            cancellationToken
        ).ConfigureAwait(false);
        if (journal is null)
        {
            await TryDeleteLegacyRecoveryHoldAsync(local, cancellationToken)
                .ConfigureAwait(false);
            return;
        }
        if (journal.Phase == SaveRecoveryUndonePhase)
            return;
        if (journal.Phase != SaveRecoveryApprovedPhase)
        {
            throw new InvalidOperationException(
                "Steam save synchronization is blocked by incomplete recovery state."
            );
        }

        var selection = RecoverySelectionFromJournal(journal);
        var exact = selection.ExactContext
            ?? throw new InvalidDataException(
                "Approved recovery state has no verified Steam account"
            );
        await VerifyRecoveryJournalSnapshotAsync(
            local,
            journal.SourceSnapshotPath,
            journal.SourceSnapshotSha256,
            exact,
            requireExactContext: false,
            cancellationToken
        ).ConfigureAwait(false);
        await VerifyRecoveryJournalSnapshotAsync(
            local,
            journal.UndoSnapshotPath,
            journal.UndoSnapshotSha256,
            exact,
            requireExactContext: true,
            cancellationToken
        ).ConfigureAwait(false);
        await VerifyRecoveryJournalSnapshotAsync(
            local,
            journal.AppliedSnapshotPath,
            journal.AppliedSnapshotSha256,
            exact,
            requireExactContext: true,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static void RequireApprovedRecoveryTransferContext(
        ISaveStore local,
        SaveContext selectedContext
    )
    {
        if (!local.FileExists(SaveRecoveryJournalPath))
            return;
        var journal = ReadRecoveryJournal(local);
        if (journal.Phase != SaveRecoveryApprovedPhase)
            return;
        var recoveryContext = RecoverySelectionFromJournal(journal).ExactContext
            ?? throw new InvalidDataException(
                "Approved recovery state has no verified Steam account"
            );
        recoveryContext.RequireExactMatch(selectedContext);
    }

    private static async Task<T> RunWithSaveTransferAndRecoveryGateAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(operation);
        await SaveTransferAndRecoveryGate.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            SaveTransferAndRecoveryGate.Release();
        }
    }

    private static async Task WriteRecoveryJournalAsync(
        ISaveStore local,
        SaveRecoveryJournalDocument journal,
        CancellationToken cancellationToken
    )
    {
        NormalizeRecoveryJournal(journal);
        await WriteAtomicAutomaticSyncDocumentAsync(
            local,
            SaveRecoveryJournalPath,
            journal,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static async Task<SaveRecoveryJournalDocument?>
        ReadRecoveryJournalAsync(
            ISaveStore local,
            CancellationToken cancellationToken
        )
    {
        var journal = await ReadAutomaticSyncDocumentAsync<
            SaveRecoveryJournalDocument
        >(
            local,
            SaveRecoveryJournalPath,
            cancellationToken
        ).ConfigureAwait(false);
        if (journal is not null)
            NormalizeRecoveryJournal(journal);
        return journal;
    }

    private static SaveRecoveryJournalDocument ReadRecoveryJournal(
        ISaveStore local
    )
    {
        var content = local.ReadFile(SaveRecoveryJournalPath)
            ?? throw new InvalidDataException(
                "The local recovery journal could not be read"
            );
        var journal = JsonSerializer.Deserialize<SaveRecoveryJournalDocument>(
            content,
            AutomaticSyncJsonOptions
        ) ?? throw new InvalidDataException(
            "The local recovery journal is empty"
        );
        NormalizeRecoveryJournal(journal);
        return journal;
    }

    private static async Task TryDeleteLegacyRecoveryHoldAsync(
        ISaveStore local,
        CancellationToken cancellationToken
    )
    {
        if (!local.FileExists(SaveRecoveryHoldPath))
            return;
        try
        {
            await CancellableSaveStore.DeleteFileAsync(
                local,
                SaveRecoveryHoldPath,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException
            || !cancellationToken.IsCancellationRequested
        )
        {
            // A stale legacy hold is never authoritative when a valid terminal
            // journal exists, so cleanup failure must not reopen a two-file
            // state machine.
            PatchHelper.Log(
                "[Recovery] Could not remove obsolete sync-hold.json: "
                    + ex.Message
            );
        }
    }

    private static void NormalizeRecoveryJournal(
        SaveRecoveryJournalDocument journal
    )
    {
        if (journal.Version != SaveRecoveryDocumentVersion)
        {
            throw new InvalidDataException(
                "Recovery journal has an unsupported version"
            );
        }
        if (journal.Phase is not (
                SaveRecoveryRestoringPhase
                or SaveRecoveryValidationPhase
                or SaveRecoveryUndoingPhase
                or SaveRecoveryApprovedPhase
                or SaveRecoveryUndonePhase
            ))
        {
            throw new InvalidDataException("Recovery journal has an invalid phase");
        }

        var normalized = NormalizeRecoverySelection(
            ParseRecoveryNamespace(journal.SaveNamespace),
            journal.RuntimeIdentity,
            journal.ModSetFingerprint,
            steamId64: null
        );
        SaveContext? targetContext = null;
        if (!string.IsNullOrWhiteSpace(journal.TargetContextMarker))
        {
            targetContext = SaveContext.ParseMarker(journal.TargetContextMarker);
            if (!SameLocalRecoverySelection(
                    normalized,
                    NormalizeRecoverySelection(
                        targetContext.Value.Namespace,
                        targetContext.Value.RuntimeIdentity,
                        targetContext.Value.ModSetFingerprint,
                        targetContext.Value.SteamId64
                    )
                ))
            {
                throw new InvalidDataException(
                    "Recovery target account context does not match its local save selection"
                );
            }
            journal.TargetContextMarker = targetContext.Value.SerializeMarker();
        }

        if (journal.Phase == SaveRecoveryApprovedPhase
            && !targetContext.HasValue)
        {
            throw new InvalidDataException(
                "Unknown-account recovery cannot be approved for sync"
            );
        }

        journal.SaveNamespace = RecoveryNamespaceName(normalized.Namespace);
        journal.RuntimeIdentity = normalized.RuntimeIdentity;
        journal.ModSetFingerprint = normalized.ModSetFingerprint;
        RequireRecoverySnapshotReferenceMetadata(
            journal.SourceSnapshotPath,
            journal.SourceSnapshotSha256,
            "source"
        );
        RequireRecoverySnapshotRole(
            journal.SourceSnapshotPath,
            RecoveryImportedDirectory,
            "source"
        );
        RequireRecoverySnapshotReferenceMetadata(
            journal.UndoSnapshotPath,
            journal.UndoSnapshotSha256,
            "Undo"
        );
        RequireRecoverySnapshotRole(
            journal.UndoSnapshotPath,
            SaveRecoveryUndoDirectory,
            "Undo"
        );
        if (journal.Phase is SaveRecoveryValidationPhase
            or SaveRecoveryApprovedPhase
            or SaveRecoveryUndoingPhase)
        {
            RequireRecoverySnapshotReferenceMetadata(
                journal.AppliedSnapshotPath,
                journal.AppliedSnapshotSha256,
                "applied"
            );
            RequireRecoverySnapshotRole(
                journal.AppliedSnapshotPath,
                SaveRecoveryAppliedDirectory,
                "applied"
            );
        }
        else if (!string.IsNullOrWhiteSpace(journal.AppliedSnapshotPath)
            || !string.IsNullOrWhiteSpace(journal.AppliedSnapshotSha256))
        {
            RequireRecoverySnapshotReferenceMetadata(
                journal.AppliedSnapshotPath,
                journal.AppliedSnapshotSha256,
                "applied"
            );
            RequireRecoverySnapshotRole(
                journal.AppliedSnapshotPath,
                SaveRecoveryAppliedDirectory,
                "applied"
            );
        }
        Normalize(journal.AppliedManifest, normalized.Namespace);
    }

    private static SaveRecoverySelection RecoverySelectionFromJournal(
        SaveRecoveryJournalDocument journal
    )
    {
        ulong? steamId64 = string.IsNullOrWhiteSpace(journal.TargetContextMarker)
            ? null
            : SaveContext.ParseMarker(journal.TargetContextMarker).SteamId64;
        return NormalizeRecoverySelection(
            ParseRecoveryNamespace(journal.SaveNamespace),
            journal.RuntimeIdentity,
            journal.ModSetFingerprint,
            steamId64
        );
    }

    private static SaveRecoverySelection NormalizeRecoverySelection(
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        ulong? steamId64
    )
    {
        if (steamId64 == 0)
            throw new ArgumentOutOfRangeException(nameof(steamId64));
        var normalized = SaveContext.NormalizeLocalSelection(
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint
        );
        return new SaveRecoverySelection(
            normalized.Namespace,
            normalized.RuntimeIdentity,
            normalized.ModSetFingerprint,
            steamId64
        );
    }

    private static bool SameLocalRecoverySelection(
        SaveRecoverySelection first,
        SaveRecoverySelection second
    )
        => first.Namespace == second.Namespace
            && string.Equals(
                first.RuntimeIdentity,
                second.RuntimeIdentity,
                StringComparison.Ordinal
            )
            && string.Equals(
                first.ModSetFingerprint,
                second.ModSetFingerprint,
                StringComparison.Ordinal
            );

    private static void RequireRecoverySnapshotReferenceMetadata(
        string path,
        string sha256,
        string label
    )
    {
        if (string.IsNullOrWhiteSpace(path) || !IsSha256(sha256))
        {
            throw new InvalidDataException(
                $"Recovery journal {label} snapshot is incomplete"
            );
        }
        _ = RequireRestorableSnapshotPath(path);
        RequireSnapshotDocumentHash(path, sha256);
    }

    private static void RequireRecoverySnapshotRole(
        string path,
        string expectedDirectory,
        string label
    )
    {
        var canonical = RequireRestorableSnapshotPath(path);
        var separator = canonical.LastIndexOf('/');
        var actualDirectory = separator < 0 ? "" : canonical[..separator];
        var requiredDirectory = CloudSavePath.Relative(expectedDirectory)
            .TrimEnd('/');
        if (!string.Equals(
                actualDirectory,
                requiredDirectory,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            throw new InvalidDataException(
                $"Recovery journal {label} snapshot has the wrong storage role"
            );
        }
    }

    private static void RequireRecoverySnapshotReferencePresent(
        ISaveStore local,
        string path,
        string sha256,
        string label
    )
    {
        RequireRecoverySnapshotReferenceMetadata(path, sha256, label);
        if (!local.FileExists(path))
        {
            throw new FileNotFoundException(
                $"Recovery journal {label} snapshot is missing",
                path
            );
        }
    }

    private static async Task VerifyRecoveryJournalSnapshotAsync(
        ISaveStore local,
        string path,
        string sha256,
        SaveContext targetContext,
        bool requireExactContext,
        CancellationToken cancellationToken
    )
    {
        RequireRecoverySnapshotReferenceMetadata(path, sha256, "referenced");
        var snapshot = await ReadRecoveryOrRetainedSnapshotAsync(
            local,
            path,
            cancellationToken
        ).ConfigureAwait(false);
        var actualContext = SnapshotExactContext(snapshot);
        if (actualContext.HasValue)
            targetContext.RequireExactMatch(actualContext.Value);
        else if (requireExactContext)
        {
            throw new InvalidDataException(
                "Recovery journal snapshot has no exact target account context"
            );
        }
    }

    private static SaveRecoveryStatus UnreadableRecoveryStatus(string message)
        => new(
            true,
            false,
            false,
            false,
            null,
            SaveNamespace.Vanilla,
            "",
            "",
            message
        );

    private static bool IsRecoveryMetadataException(Exception ex)
        => ex is JsonException
            or InvalidDataException
            or InvalidOperationException
            or ArgumentException
            or IOException
            or UnauthorizedAccessException;

    private static SaveNamespace ParseRecoveryNamespace(string value)
        => value switch
        {
            "vanilla" => SaveNamespace.Vanilla,
            "modded" => SaveNamespace.Modded,
            _ => throw new InvalidDataException(
                "Recovery metadata has an invalid save namespace"
            ),
        };

    private static string RecoveryNamespaceName(SaveNamespace saveNamespace)
        => saveNamespace == SaveNamespace.Modded ? "modded" : "vanilla";
}
