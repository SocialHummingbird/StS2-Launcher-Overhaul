using System;
using System.Collections.Generic;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudPostOperationRefreshTest
{
    private static int _passed;

    private static int Main()
    {
        Run("complete Pull permits completion evidence", CompletePullPermitsEvidence);
        Run("transfer faults classify partial success", TransferFaultsClassifyPartial);
        Run("zero-download Pull classifies failure", ZeroDownloadPullClassifiesFailure);
        Run("timeout without progress classifies failure", TimeoutWithoutProgressClassifiesFailure);
        Run("evidence precedes authoritative recapture", EvidencePrecedesRecapture);
        Run("partial Pull never records completion evidence", PartialPullSkipsEvidence);
        Run("evidence-write failure downgrades success", EvidenceFailureDowngradesSuccess);
        Run("eligible recapture unlocks Upload immediately", EligibleRecaptureUnlocksUpload);
        Run("remaining blocker is named after success", RemainingBlockerIsNamed);
        Run("partial Pull summary preserves safety lock", PartialPullSummaryIsPrecise);
        Run("Upload summaries use transfer language", UploadSummaryUsesTransferLanguage);
        Run("operation timeout has a distinct summary", TimeoutHasDistinctSummary);
        Run("operation failure retains precise progress", FailureRetainsProgress);
        Run("recovery outcomes are precise", RecoveryOutcomesArePrecise);

        AssertEqual("post-operation refresh tests passed", 14, _passed);
        Console.WriteLine("Launcher cloud post-operation refresh tests passed 14/14.");
        return 0;
    }

    private static void CompletePullPermitsEvidence()
    {
        var result = PullResult(completed: 3, skipped: 2);

        AssertEqual(
            "completion",
            ManualCloudSyncCompletion.Success,
            result.Completion
        );
        AssertTrue("completion evidence", result.CanRecordCompletionEvidence);
    }

    private static void TransferFaultsClassifyPartial()
    {
        var result = PullResult(
            completed: 2,
            skipped: 1,
            failed: 1,
            timedOut: 1,
            unprocessed: 2
        );

        AssertEqual(
            "completion",
            ManualCloudSyncCompletion.PartialSuccess,
            result.Completion
        );
        AssertFalse("completion evidence", result.CanRecordCompletionEvidence);
    }

    private static void ZeroDownloadPullClassifiesFailure()
    {
        var result = PullResult(completed: 0, skipped: 5);

        AssertEqual(
            "completion",
            ManualCloudSyncCompletion.Failure,
            result.Completion
        );
        AssertFalse("completion evidence", result.CanRecordCompletionEvidence);
    }

    private static void TimeoutWithoutProgressClassifiesFailure()
    {
        var result = PullResult(
            completed: 0,
            timedOut: 1,
            unprocessed: 4
        );

        AssertEqual(
            "completion",
            ManualCloudSyncCompletion.Failure,
            result.Completion
        );
    }

    private static void EvidencePrecedesRecapture()
    {
        var calls = new List<string>();
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            PullResult(completed: 2),
            evidenceRequired: true,
            () =>
            {
                calls.Add("evidence");
                return true;
            },
            () =>
            {
                calls.Add("capture");
                return Snapshot(Eligible());
            }
        );

        AssertSequence(
            "completion order",
            new[] { "evidence", "capture" },
            calls
        );
        AssertTrue("evidence recorded", resolution.CompletionEvidenceRecorded);
    }

    private static void PartialPullSkipsEvidence()
    {
        var evidenceCalls = 0;
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            PullResult(completed: 1, failed: 1),
            evidenceRequired: true,
            () =>
            {
                evidenceCalls++;
                return true;
            },
            () => Snapshot(Ineligible("Complete a fully successful Pull."))
        );

        AssertEqual("evidence calls", 0, evidenceCalls);
        AssertFalse("evidence recorded", resolution.CompletionEvidenceRecorded);
    }

    private static void EvidenceFailureDowngradesSuccess()
    {
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            PullResult(completed: 3),
            evidenceRequired: true,
            () => false,
            () => Snapshot(Ineligible("Pull completion evidence is missing."))
        );
        var presentation =
            CloudOperationTerminalPresentation.CreateCompletion(resolution);

        AssertEqual(
            "outcome",
            CloudOperationTerminalOutcome.PartialSuccess,
            presentation.Outcome
        );
        AssertContains(
            "evidence failure",
            presentation.StatusText,
            "evidence"
        );
    }

    private static void EligibleRecaptureUnlocksUpload()
    {
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            PullResult(completed: 4, skipped: 1),
            evidenceRequired: true,
            () => true,
            () => Snapshot(Eligible())
        );
        var presentation =
            CloudOperationTerminalPresentation.CreateCompletion(resolution);

        AssertEqual(
            "outcome",
            CloudOperationTerminalOutcome.Success,
            presentation.Outcome
        );
        AssertTrue(
            "captured eligibility",
            resolution.Snapshot.UploadEligibility.IsEligible
        );
        AssertContains(
            "unlock summary",
            presentation.StatusText,
            "Upload is now available"
        );
    }

    private static void RemainingBlockerIsNamed()
    {
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            PullResult(completed: 4),
            evidenceRequired: true,
            () => true,
            () => Snapshot(Ineligible("Deselect all mods before Upload."))
        );
        var presentation =
            CloudOperationTerminalPresentation.CreateCompletion(resolution);

        AssertEqual(
            "Pull outcome",
            CloudOperationTerminalOutcome.Success,
            presentation.Outcome
        );
        AssertContains(
            "remaining blocker",
            presentation.StatusText,
            "Deselect all mods"
        );
    }

    private static void TimeoutHasDistinctSummary()
    {
        var state = TransferState(
            total: 5,
            completed: 2,
            skipped: 1,
            failed: 0,
            timedOut: 0
        );
        var presentation = CloudOperationTerminalPresentation.CreateTimeout(
            "Pull",
            state,
            "Pull timed out after 180000ms",
            Snapshot(Ineligible("Complete Pull from Steam Cloud."))
        );

        AssertEqual(
            "outcome",
            CloudOperationTerminalOutcome.Timeout,
            presentation.Outcome
        );
        AssertContains("timeout title", presentation.StatusText, "timed out");
        AssertContains("retained completed count", presentation.StatusText, "2 downloaded");
        AssertContains("unfinished count", presentation.StatusText, "2 unfinished");
    }

    private static void PartialPullSummaryIsPrecise()
    {
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            PullResult(completed: 2, failed: 1, unprocessed: 1),
            evidenceRequired: true,
            () => throw new InvalidOperationException(
                "partial Pull must not write completion evidence"
            ),
            () => Snapshot(Ineligible("Retry Pull from Steam Cloud."))
        );
        var presentation =
            CloudOperationTerminalPresentation.CreateCompletion(resolution);

        AssertEqual(
            "partial outcome",
            CloudOperationTerminalOutcome.PartialSuccess,
            presentation.Outcome
        );
        AssertContains(
            "partial summary",
            presentation.StatusText,
            "partially succeeded"
        );
        AssertContains(
            "partial safety lock",
            presentation.StatusText,
            "Upload remains locked"
        );
    }

    private static void UploadSummaryUsesTransferLanguage()
    {
        var result = new ManualCloudSyncResult(
            CloudOperationKind.Push,
            CandidatePathCount: 3,
            CompletedPathCount: 1,
            SkippedPathCount: 0,
            FailedPathCount: 1,
            TimedOutPathCount: 0,
            UnprocessedPathCount: 1,
            BackupCreatedCount: 1,
            PrivateBackupCreatedCount: 0,
            ProfileSeededCount: 0,
            PostProcessingErrorCount: 0,
            Detail: ""
        );
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            result,
            evidenceRequired: false,
            recordCompletionEvidence: null,
            () => Snapshot(Eligible())
        );
        var presentation =
            CloudOperationTerminalPresentation.CreateCompletion(resolution);

        AssertEqual(
            "Upload partial outcome",
            CloudOperationTerminalOutcome.PartialSuccess,
            presentation.Outcome
        );
        AssertContains(
            "Upload partial summary",
            presentation.StatusText,
            "Some saves were not updated in Steam Cloud"
        );
        AssertFalse(
            "Upload summary mentions Pull evidence",
            presentation.StatusText.Contains(
                "Completion evidence",
                StringComparison.Ordinal
            )
        );
    }

    private static void FailureRetainsProgress()
    {
        var state = TransferState(
            total: 4,
            completed: 1,
            skipped: 0,
            failed: 1,
            timedOut: 0
        );
        var presentation = CloudOperationTerminalPresentation.CreateFailure(
            "Pull",
            state,
            "Steam connection failed",
            Snapshot(Ineligible("Retry Pull from Steam Cloud."))
        );

        AssertEqual(
            "outcome",
            CloudOperationTerminalOutcome.Failure,
            presentation.Outcome
        );
        AssertContains("failure reason", presentation.StatusText, "Steam connection failed");
        AssertContains("retained completed count", presentation.StatusText, "1 downloaded");
        AssertContains("failed count", presentation.StatusText, "1 failed");
    }

    private static void RecoveryOutcomesArePrecise()
    {
        var success = LocalBackupRecoveryPresentation.Create(
            new LocalBackupRefreshResult(
                Attempted: true,
                StorageAccessAvailable: true,
                Discovered: 3,
                Mirrored: 2,
                Archived: 1,
                Restored: 2,
                Errors: 0,
                FailureMessage: ""
            )
        );
        var partial = LocalBackupRecoveryPresentation.Create(
            new LocalBackupRefreshResult(
                Attempted: true,
                StorageAccessAvailable: true,
                Discovered: 3,
                Mirrored: 1,
                Archived: 0,
                Restored: 1,
                Errors: 2,
                FailureMessage: ""
            )
        );
        var failure = LocalBackupRecoveryPresentation.Create(
            LocalBackupRefreshResult.Failed("Backup directory is unavailable")
        );

        AssertEqual(
            "success outcome",
            LocalBackupRefreshCompletion.Success,
            success.Completion
        );
        AssertContains("success restored count", success.StatusText, "2 restored");
        AssertEqual(
            "partial outcome",
            LocalBackupRefreshCompletion.PartialSuccess,
            partial.Completion
        );
        AssertContains("partial errors", partial.StatusText, "2 errors");
        AssertEqual(
            "failure outcome",
            LocalBackupRefreshCompletion.Failure,
            failure.Completion
        );
        AssertContains(
            "failure reason",
            failure.StatusText,
            "Backup directory is unavailable"
        );
    }

    private static ManualCloudSyncResult PullResult(
        int completed,
        int skipped = 0,
        int failed = 0,
        int timedOut = 0,
        int unprocessed = 0
    )
        => new(
            CloudOperationKind.Pull,
            completed + skipped + failed + timedOut + unprocessed,
            completed,
            skipped,
            failed,
            timedOut,
            unprocessed,
            BackupCreatedCount: 1,
            PrivateBackupCreatedCount: 0,
            ProfileSeededCount: 1,
            PostProcessingErrorCount: 0,
            Detail: ""
        );

    private static CloudOperationState TransferState(
        int total,
        int completed,
        int skipped,
        int failed,
        int timedOut
    )
        => CloudOperationState.Idle(CloudOperationKind.Pull) with
        {
            Phase = CloudOperationPhase.Transferring,
            TransferTotalCount = total,
            TransferProcessedCount = completed + skipped + failed + timedOut,
            TransferCompletedCount = completed,
            TransferSkippedCount = skipped,
            TransferFailedCount = failed,
            TransferTimedOutCount = timedOut,
        };

    private static CloudPostOperationSnapshot Snapshot(
        CloudPushEligibilityResult eligibility
    )
        => new(
            ImportantLocalSaveEvidenceCount: eligibility.IsEligible ? 2 : 0,
            CurrentMirrorSaveCount: eligibility.IsEligible ? 2 : 0,
            eligibility
        );

    private static CloudPushEligibilityResult Eligible()
        => new(Array.Empty<CloudPushEligibilityBlock>());

    private static CloudPushEligibilityResult Ineligible(string reason)
        => new(
            new[]
            {
                new CloudPushEligibilityBlock(
                    CloudPushEligibilityBlockCode.ManualPullNotCompleted,
                    reason,
                    new CloudPushRequiredAction(
                        CloudPushRequiredActionCode.CompletePullForSelectedVersion,
                        reason
                    )
                ),
            }
        );

    private static void Run(string name, Action test)
    {
        test();
        _passed++;
        Console.WriteLine($"[PASS] {name}");
    }

    private static void AssertTrue(string name, bool actual)
    {
        if (!actual)
            throw new InvalidOperationException($"{name}: expected true.");
    }

    private static void AssertFalse(string name, bool actual)
    {
        if (actual)
            throw new InvalidOperationException($"{name}: expected false.");
    }

    private static void AssertEqual<T>(string name, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected {expected}, actual {actual}."
            );
        }
    }

    private static void AssertContains(
        string name,
        string actual,
        string expected
    )
    {
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}' in '{actual}'."
            );
        }
    }

    private static void AssertSequence<T>(
        string name,
        IReadOnlyCollection<T> expected,
        IReadOnlyCollection<T> actual
    )
    {
        if (!System.Linq.Enumerable.SequenceEqual(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected [{string.Join(", ", expected)}], "
                    + $"actual [{string.Join(", ", actual)}]."
            );
        }
    }
}
