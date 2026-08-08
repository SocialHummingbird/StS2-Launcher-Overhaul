#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed class LauncherSaveRecoveryCoordinator
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherCloudSyncCoordinator _cloud;
    private readonly Action<Action> _runOnMainThread;
    private readonly Dictionary<string, CloudSyncCoordinator.LegacySaveRecoveryCandidate>
        _candidates = new(StringComparer.Ordinal);
    private readonly HashSet<string> _exportedCandidateIds = new(
        StringComparer.Ordinal
    );
    private CloudSyncCoordinator.LegacySaveRecoveryScanResult? _lastScan;

    internal LauncherSaveRecoveryCoordinator(
        LauncherModel model,
        LauncherView view,
        LauncherCloudSyncCoordinator cloud,
        Action<Action> runOnMainThread
    )
    {
        _model = model;
        _view = view;
        _cloud = cloud;
        _runOnMainThread = runOnMainThread;
    }

    internal void RefreshState()
    {
        var status = CloudSyncCoordinator.InspectSaveRecoveryStatus();
        _view.SetSaveRecoveryState(
            status.Message,
            status.CanUndo,
            status.CanApprove
        );
        _view.SetPushPullDisabled(status.SyncHeld);
        _view.RefreshCloudPushEligibility();
    }

    internal void ScanPressed()
        => RunExclusive(
            "Scanning known local save and backup locations...",
            ScanAsync
        );

    internal void CurrentExportPressed()
        => RunExclusive(
            "Capturing current Android saves byte-for-byte...",
            ExportCurrentAsync
        );

    internal void ExportPressed(string candidateId)
        => RunExclusive(
            "Building a verified recovery bundle...",
            token => ExportAsync(candidateId, token)
        );

    internal void RestorePressed(string candidateId)
    {
        if (!_candidates.TryGetValue(candidateId, out var candidate))
        {
            _view.SetSaveRecoveryState(
                "That recovery copy is no longer in the verified scan. Scan again.",
                CloudSyncCoordinator.InspectSaveRecoveryStatus().CanUndo,
                CloudSyncCoordinator.InspectSaveRecoveryStatus().CanApprove
            );
            return;
        }
        if (!CanRestoreInCurrentLocalContext(candidate)
            || !_exportedCandidateIds.Contains(candidateId))
        {
            _view.SetSaveRecoveryState(
                "Export and read-back verify the selected recovery bundle before restoring it.",
                CloudSyncCoordinator.InspectSaveRecoveryStatus().CanUndo,
                CloudSyncCoordinator.InspectSaveRecoveryStatus().CanApprove
            );
            return;
        }

        _view.ShowConfirmation(
            "Restore this verified snapshot on Android?\n\n"
                + CandidateDetail(candidate)
                + (candidate.ExactSteamId64.HasValue
                    ? "\n\nThe launcher will first create a byte-for-byte Undo snapshot. Steam will not be contacted, and all Steam syncing will remain blocked until you validate and approve the restored save for this exact account and context."
                    : "\n\nThe launcher will first create a byte-for-byte Undo snapshot. Steam will not be contacted. Because this copy's account provenance is unknown, it will remain local-only and cannot be approved for synchronization."),
            () => RunExclusive(
                "Creating Undo snapshot and restoring Android saves...",
                token => RestoreAsync(candidateId, token)
            ),
            "Restore on Android",
            "Cancel"
        );
    }

    internal void UndoPressed()
        => _view.ShowConfirmation(
            "Undo the last Android save restore byte-for-byte? Steam will not be contacted.",
            () => RunExclusive(
                "Undoing the last Android save restore...",
                UndoAsync
            ),
            "Undo Restore",
            "Cancel"
        );

    internal void ApprovePressed()
        => _view.ShowConfirmation(
            "Approve the currently recovered Android saves for later synchronization?\n\nOnly continue after opening the game locally and checking the recovered profiles and progress. This approval does not upload now; normal account, branch, vanilla/modded, and mod-set checks still apply.",
            () => RunExclusive(
                "Re-reading recovered saves before approval...",
                ApproveAsync
            ),
            "I Validated Them",
            "Keep Steam Blocked"
        );

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        var context = CurrentContext();
        var scan = await CloudSyncCoordinator.ScanAndImportLegacySavesAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            context.Namespace,
            context.RuntimeIdentity,
            context.ModSetFingerprint,
            knownSteamId64: null,
            AppPaths.ExternalSaveBackupsDir,
            cancellationToken
        ).ConfigureAwait(false);

        _lastScan = scan;
        _candidates.Clear();
        _exportedCandidateIds.Clear();
        foreach (var candidate in scan.Candidates)
        {
            var id = CandidateId(candidate);
            while (_candidates.ContainsKey(id))
                id = AutomaticSyncHash.Compute(id + ":duplicate");
            _candidates[id] = candidate;
        }

        var unknown = scan.Candidates.Count(candidate =>
            candidate.Classification
                == CloudSyncCoordinator.LegacyRecoveryClassification.UnknownContext
        );
        var quarantined = scan.Candidates.Count(candidate =>
            candidate.Classification
                == CloudSyncCoordinator.LegacyRecoveryClassification.QuarantinedForeignAccount
        );
        RunOnMainThread(() =>
        {
            _view.SetSaveRecoveryCandidates(BuildPresentations());
            _view.SetSaveRecoveryBusy(
                false,
                $"Copied and verified {scan.Candidates.Count} recovery candidate(s). Unknown context: {unknown}. Quarantined other-account copies: {quarantined}. Original files were not moved, changed, or deleted."
            );
            RefreshState();
        });
    }

    private async Task ExportAsync(
        string candidateId,
        CancellationToken cancellationToken
    )
    {
        var scan = _lastScan ?? throw new InvalidOperationException(
            "Scan and copy recovery sources before exporting a bundle."
        );
        if (!_candidates.TryGetValue(candidateId, out var selected))
            throw new InvalidOperationException("Selected recovery copy is no longer available.");
        var context = CurrentContext();
        var json = await CloudSyncCoordinator.BuildSaveRecoveryExportBundleAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            scan,
            selected.SnapshotSha256,
            context.Namespace,
            context.RuntimeIdentity,
            context.ModSetFingerprint,
            LauncherCloudSaveState.CloudSyncEnabled,
            cancellationToken
        ).ConfigureAwait(false);
        var path = LauncherDiagnostics.WriteSaveRecoveryBundle(
            json,
            _model.DataDir
        );
        _exportedCandidateIds.Add(candidateId);
        RunOnMainThread(() =>
        {
            _view.SetSaveRecoveryCandidates(
                BuildPresentations(),
                candidateId
            );
            _view.SetSaveRecoveryBusy(
                false,
                $"Recovery bundle written and read-back verified: {path}. The selected copy can now be restored on Android."
            );
            _view.AppendLog(
                "Recovery bundle contains raw saves and account/context metadata. Share it only with trusted support staff."
            );
            if (OperatingSystem.IsAndroid())
            {
                _view.AppendLog(
                    LauncherSharedTextFile.Share(path)
                        .AndroidShareSheetLogMessage()
                );
            }
        });
    }

    private async Task ExportCurrentAsync(
        CancellationToken cancellationToken
    )
    {
        var context = CurrentContext();
        var json = await CloudSyncCoordinator.BuildCurrentSaveExportBundleAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            context.Namespace,
            context.RuntimeIdentity,
            context.ModSetFingerprint,
            LauncherCloudSaveState.CloudSyncEnabled,
            cancellationToken
        ).ConfigureAwait(false);
        var path = LauncherDiagnostics.WriteSaveRecoveryBundle(
            json,
            _model.DataDir
        );
        RunOnMainThread(() =>
        {
            _view.SetSaveRecoveryBusy(
                false,
                $"Current Android saves written and read-back verified: {path}. Steam was not contacted."
            );
            _view.AppendLog(
                "Current save bundle contains raw saves and account/context metadata. Share it only with trusted support staff."
            );
            if (OperatingSystem.IsAndroid())
            {
                _view.AppendLog(
                    LauncherSharedTextFile.Share(path)
                        .AndroidShareSheetLogMessage()
                );
            }
        });
    }

    private async Task RestoreAsync(
        string candidateId,
        CancellationToken cancellationToken
    )
    {
        if (!_candidates.TryGetValue(candidateId, out var candidate)
            || !CanRestoreInCurrentLocalContext(candidate)
            || !_exportedCandidateIds.Contains(candidateId))
        {
            throw new InvalidOperationException(
                "Recovery copy is no longer eligible. Scan again."
            );
        }
        var context = CurrentContext();
        var result = await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            candidate.RecoverySnapshotPath,
            context.Namespace,
            context.RuntimeIdentity,
            context.ModSetFingerprint,
            candidate.ExactSteamId64,
            cancellationToken
        ).ConfigureAwait(false);

        LauncherPreferences.SaveCloudSyncEnabled(false);
        LauncherCloudSaveState.SetCloudSyncEnabled(false);
        RunOnMainThread(() =>
        {
            _view.SetActionPreferences(
                LauncherPreferences.ReadActionPreferences()
            );
            _view.SetSaveRecoveryBusy(false, result.Message);
            ApplyRecoveryStatus(result.Status);
        });
    }

    private async Task UndoAsync(CancellationToken cancellationToken)
    {
        var result = await CloudSyncCoordinator.UndoSaveRecoveryAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            cancellationToken
        ).ConfigureAwait(false);
        RunOnMainThread(() =>
        {
            _view.SetSaveRecoveryBusy(false, result.Message);
            ApplyRecoveryStatus(result.Status);
        });
    }

    private async Task ApproveAsync(CancellationToken cancellationToken)
    {
        var context = CurrentContext();
        var status = CloudSyncCoordinator.InspectSaveRecoveryStatus();
        var steamId64 = status.SteamId64
            ?? throw new InvalidOperationException(
                "The recovered files have unknown Steam-account provenance and cannot be approved for synchronization."
            );
        var result = await CloudSyncCoordinator.ApproveSaveRecoveryForSyncAsync(
            CloudSaveStoreFactory.CreateLocalStore(),
            context.Namespace,
            context.RuntimeIdentity,
            context.ModSetFingerprint,
            steamId64,
            cancellationToken
        ).ConfigureAwait(false);
        RunOnMainThread(() =>
        {
            _view.SetSaveRecoveryBusy(false, result.Message);
            ApplyRecoveryStatus(result.Status);
        });
    }

    private void ApplyRecoveryStatus(SaveRecoveryStatus status)
    {
        _view.SetSaveRecoveryState(
            status.Message,
            status.CanUndo,
            status.CanApprove
        );
        _view.SetPushPullDisabled(status.SyncHeld);
        _view.RefreshCloudPushEligibility();
    }

    private void RunExclusive(
        string busyMessage,
        Func<CancellationToken, Task> operation
    )
    {
        _view.SetSaveRecoveryBusy(true, busyMessage);
        if (!_cloud.TryRunExclusiveSaveOperation(
                async token =>
                {
                    try
                    {
                        await operation(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                        when (token.IsCancellationRequested)
                    {
                        RunOnMainThread(() =>
                            _view.SetSaveRecoveryBusy(
                                false,
                                "Recovery operation cancelled. Any durable recovery hold remains in place."
                            )
                        );
                    }
                    catch (Exception ex)
                    {
                        RunOnMainThread(() =>
                        {
                            _view.SetSaveRecoveryBusy(
                                false,
                                "Recovery operation stopped safely: "
                                    + ex.GetBaseException().Message
                            );
                            RefreshState();
                        });
                    }
                },
                out _
            ))
        {
            _view.SetSaveRecoveryBusy(
                false,
                "Another save operation is already running. Wait for it to finish or cancel it first."
            );
        }
    }

    private static string CandidateId(
        CloudSyncCoordinator.LegacySaveRecoveryCandidate candidate
    )
        => AutomaticSyncHash.Compute(
            candidate.SnapshotSha256
                + "|"
                + candidate.SourceKind
                + "|"
                + string.Join("|", candidate.SourcePaths)
        );

    private IReadOnlyList<SaveRecoveryCandidatePresentation>
        BuildPresentations()
        => _candidates.Select(entry =>
            new SaveRecoveryCandidatePresentation(
                entry.Key,
                CandidateTitle(entry.Value),
                CandidateDetail(entry.Value)
                    + (CanRestoreInCurrentLocalContext(entry.Value)
                        && !_exportedCandidateIds.Contains(entry.Key)
                            ? "\nRequired first: export this selected recovery bundle."
                            : ""),
                CanRestoreInCurrentLocalContext(entry.Value)
                    && _exportedCandidateIds.Contains(entry.Key)
            )
        ).ToArray();

    private static string CandidateTitle(
        CloudSyncCoordinator.LegacySaveRecoveryCandidate candidate
    )
        => $"{SourceName(candidate.SourceKind)} | {candidate.CapturedUtc:yyyy-MM-dd HH:mm} UTC | {candidate.FileCount} file(s)";

    private static string CandidateDetail(
        CloudSyncCoordinator.LegacySaveRecoveryCandidate candidate
    )
    {
        var provenance = candidate.Provenance;
        var scope = candidate.OverwriteOnly
            ? "Partial; overwrites listed files only"
            : "Full snapshot; explicit missing files are restored as missing";
        var classification = candidate.Classification switch
        {
            CloudSyncCoordinator.LegacyRecoveryClassification.QuarantinedForeignAccount
                => "QUARANTINED -- another Steam account",
            CloudSyncCoordinator.LegacyRecoveryClassification.UnknownContext
                => "Unknown context -- validate manually",
            CloudSyncCoordinator.LegacyRecoveryClassification.ExactSelectedContext
                => "Exact selected context",
            CloudSyncCoordinator.LegacyRecoveryClassification.ExactOtherContext
                when CanRestoreInCurrentLocalContext(candidate)
                => "Exact recorded context -- account shown below",
            CloudSyncCoordinator.LegacyRecoveryClassification.ExactOtherContext
                => "Exact different branch/mod context",
            _ => "Invalid",
        };
        var problems = candidate.Problems.Count == 0
            ? ""
            : "\nNotes: " + string.Join("; ", candidate.Problems);
        return $"{classification}\nCoverage: {scope}\n"
            + $"Steam account: {ProvenanceValue(provenance.SteamId64)}\n"
            + $"Save type: {ProvenanceValue(provenance.SaveNamespace)}\n"
            + $"Game version: {ProvenanceValue(provenance.RuntimeIdentity)}\n"
            + $"Mod set: {ProvenanceValue(provenance.ModSetFingerprint, none: true)}"
            + problems;
    }

    private static string ProvenanceValue(
        CloudSyncCoordinator.LegacyRecoveryProvenanceField field,
        bool none = false
    )
        => field.Knowledge
            == CloudSyncCoordinator.LegacyRecoveryProvenanceKnowledge.Unknown
                ? "Unknown"
                : none && string.IsNullOrWhiteSpace(field.Value)
                    ? "None"
                    : field.Value;

    private static bool CanRestoreInCurrentLocalContext(
        CloudSyncCoordinator.LegacySaveRecoveryCandidate candidate
    )
        => candidate.CanRestore
            && candidate.Provenance.SaveNamespace.MatchesSelection != false
            && candidate.Provenance.RuntimeIdentity.MatchesSelection != false
            && candidate.Provenance.ModSetFingerprint.MatchesSelection != false;

    private static string SourceName(
        CloudSyncCoordinator.LegacyRecoverySourceKind kind
    )
        => kind switch
        {
            CloudSyncCoordinator.LegacyRecoverySourceKind.Stage3Snapshot
                => "Automatic snapshot",
            CloudSyncCoordinator.LegacyRecoverySourceKind.Stage2TransferBackup
                => "Transfer backup",
            CloudSyncCoordinator.LegacyRecoverySourceKind.LiveSidecar
                => "Temporary/local backup",
            CloudSyncCoordinator.LegacyRecoverySourceKind.ManualPullBackup
                => "Older Pull backup",
            CloudSyncCoordinator.LegacyRecoverySourceKind.ExternalCurrent
                => "External current copy",
            CloudSyncCoordinator.LegacyRecoverySourceKind.ExternalHistory
                => "External history copy",
            _ => "Timestamped backup",
        };

    private static RecoverySelection CurrentContext()
        => new(
            LauncherModSelectionState.IsModdedMode
                ? SaveNamespace.Modded
                : SaveNamespace.Vanilla,
            SteamGameBranch.StorageIdentity(
                LauncherPreferences.ReadGameBranch()
            ),
            LauncherModSelectionState.EnabledModSetFingerprint() ?? ""
        );

    private void RunOnMainThread(Action action)
        => _runOnMainThread(action);

    private readonly record struct RecoverySelection(
        SaveNamespace Namespace,
        string RuntimeIdentity,
        string ModSetFingerprint
    );
}
