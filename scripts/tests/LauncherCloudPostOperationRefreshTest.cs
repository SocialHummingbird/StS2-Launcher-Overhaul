using System;
using System.Collections.Generic;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudPostOperationRefreshTest
{
    private static int _passed;

    private static int Main()
    {
        Run("sync result contains verified-success data only", SyncResultContainsSuccessDataOnly);
        Run("verified completion recaptures current state", CompletionRecapturesCurrentState);
        Run("Pull success copy is independent of legacy evidence", PullSuccessIgnoresLegacyEvidence);
        Run("Upload summaries use verified transfer language", UploadSummaryUsesTransferLanguage);
        Run("operation timeout has a distinct summary", TimeoutHasDistinctSummary);
        Run("operation failure retains verified progress", FailureRetainsProgress);
        Run("operation cancellation names unfinished work", CancellationNamesUnfinishedWork);
        Run("local recovery outcomes remain precise", RecoveryOutcomesArePrecise);

        AssertEqual("post-operation refresh tests passed", 8, _passed);
        Console.WriteLine("Launcher cloud post-operation refresh tests passed 8/8.");
        return 0;
    }

    private static void SyncResultContainsSuccessDataOnly()
    {
        var result = PullResult(transferred: 3, backups: 2);
        AssertEqual("kind", CloudOperationKind.Pull, result.Kind);
        AssertEqual("transferred", 3, result.TransferredPathCount);
        AssertEqual("backups", 2, result.BackupCreatedCount);
    }

    private static void CompletionRecapturesCurrentState()
    {
        var captureCalls = 0;
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            PullResult(2),
            () =>
            {
                captureCalls++;
                return Snapshot(Eligible());
            }
        );

        AssertEqual("capture calls", 1, captureCalls);
        AssertTrue("captured eligibility", resolution.Snapshot.UploadEligibility.IsEligible);
    }

    private static void PullSuccessIgnoresLegacyEvidence()
    {
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            PullResult(3),
            () => Snapshot(MissingLocalSaves())
        );
        var presentation = CloudOperationTerminalPresentation.CreateCompletion(resolution);

        AssertEqual("Pull outcome", CloudOperationTerminalOutcome.Success, presentation.Outcome);
        AssertContains("verified snapshot", presentation.StatusText, "verified Steam snapshot");
        AssertFalse("no Upload lock claim", presentation.StatusText.Contains("locked", StringComparison.OrdinalIgnoreCase));
        AssertFalse("no evidence claim", presentation.StatusText.Contains("evidence", StringComparison.OrdinalIgnoreCase));
    }

    private static void UploadSummaryUsesTransferLanguage()
    {
        var result = new ManualCloudSyncResult(
            CloudOperationKind.Push,
            TransferredPathCount: 3,
            BackupCreatedCount: 1,
            Detail: "Remote hashes verified."
        );
        var resolution = CloudPostOperationRefresh.ResolveCompletion(
            result,
            () => Snapshot(Eligible())
        );
        var presentation = CloudOperationTerminalPresentation.CreateCompletion(resolution);

        AssertEqual("Upload outcome", CloudOperationTerminalOutcome.Success, presentation.Outcome);
        AssertContains("Upload summary", presentation.StatusText, "Steam Cloud was updated");
        AssertContains("Upload count", presentation.StatusText, "3 verified");
        AssertFalse("Upload summary mentions Pull evidence", presentation.StatusText.Contains("completion evidence", StringComparison.OrdinalIgnoreCase));
    }

    private static void TimeoutHasDistinctSummary()
    {
        var state = TransferState(total: 5, processed: 3, completed: 2);
        var presentation = CloudOperationTerminalPresentation.CreateTimeout(
            "Pull",
            state,
            "Pull timed out after 180000ms"
        );

        AssertEqual("outcome", CloudOperationTerminalOutcome.Timeout, presentation.Outcome);
        AssertContains("timeout title", presentation.StatusText, "timed out");
        AssertContains("retained completed count", presentation.StatusText, "2 verified");
        AssertContains("unfinished count", presentation.StatusText, "2 unfinished");
    }

    private static void FailureRetainsProgress()
    {
        var state = TransferState(total: 4, processed: 1, completed: 1);
        var presentation = CloudOperationTerminalPresentation.CreateFailure(
            "Pull",
            state,
            "Steam connection failed"
        );

        AssertEqual("outcome", CloudOperationTerminalOutcome.Failure, presentation.Outcome);
        AssertContains("failure reason", presentation.StatusText, "Steam connection failed");
        AssertContains("retained completed count", presentation.StatusText, "1 verified");
        AssertContains("unfinished count", presentation.StatusText, "3 unfinished");
    }

    private static void CancellationNamesUnfinishedWork()
    {
        var state = TransferState(total: 4, processed: 1, completed: 1);
        var presentation = CloudOperationTerminalPresentation.CreateCancelled(
            "Pull",
            state
        );

        AssertEqual("outcome", CloudOperationTerminalOutcome.Cancelled, presentation.Outcome);
        AssertContains("cancelled title", presentation.StatusText, "cancelled");
        AssertContains("unfinished count", presentation.StatusText, "3 unfinished");
        AssertContains("no hidden work", presentation.StatusText, "No hidden cloud work remains");
    }

    private static void RecoveryOutcomesArePrecise()
    {
        var success = LocalBackupRecoveryPresentation.Create(
            new LocalBackupRefreshResult(true, true, 3, 2, 1, 0, "")
        );
        var partial = LocalBackupRecoveryPresentation.Create(
            new LocalBackupRefreshResult(true, true, 3, 1, 0, 2, "")
        );
        var failure = LocalBackupRecoveryPresentation.Create(
            LocalBackupRefreshResult.Failed("Backup directory is unavailable")
        );

        AssertEqual("success outcome", LocalBackupRefreshCompletion.Success, success.Completion);
        AssertContains("success backup count", success.StatusText, "2 mirrored");
        AssertNotContains("success does not claim restore", success.StatusText, "restored");
        AssertEqual("partial outcome", LocalBackupRefreshCompletion.PartialSuccess, partial.Completion);
        AssertContains("partial errors", partial.StatusText, "2 errors");
        AssertContains("partial restore boundary", partial.StatusText, "No save files were restored automatically");
        AssertEqual("failure outcome", LocalBackupRefreshCompletion.Failure, failure.Completion);
        AssertContains("failure reason", failure.StatusText, "Backup directory is unavailable");
    }

    private static ManualCloudSyncResult PullResult(int transferred, int backups = 1)
        => new(CloudOperationKind.Pull, transferred, backups, "");

    private static CloudOperationState TransferState(int total, int processed, int completed)
        => CloudOperationState.Idle(CloudOperationKind.Pull) with
        {
            Phase = CloudOperationPhase.Transferring,
            TransferTotalCount = total,
            TransferProcessedCount = processed,
            TransferCompletedCount = completed,
        };

    private static CloudPostOperationSnapshot Snapshot(CloudPushEligibilityResult eligibility)
        => new(
            ImportantLocalSaveEvidenceCount: eligibility.IsEligible ? 2 : 0,
            CurrentMirrorSaveCount: eligibility.IsEligible ? 2 : 0,
            eligibility
        );

    private static CloudPushEligibilityResult Eligible()
        => new(Array.Empty<CloudPushEligibilityBlock>());

    private static CloudPushEligibilityResult MissingLocalSaves()
        => new(
            new[]
            {
                new CloudPushEligibilityBlock(
                    CloudPushEligibilityBlockCode.ImportantLocalSavesMissing,
                    "No transferable Android local save files were found.",
                    new CloudPushRequiredAction(
                        CloudPushRequiredActionCode.VerifyAndroidLocalSaves,
                        "Open the game and verify that Android local saves exist."
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
            throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}.");
    }

    private static void AssertContains(string name, string actual, string expected)
    {
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{name}: expected '{expected}' in '{actual}'.");
    }

    private static void AssertNotContains(
        string name,
        string actual,
        string rejected
    )
    {
        if (actual.Contains(rejected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"{name}: did not expect '{rejected}' in '{actual}'."
            );
    }

}
