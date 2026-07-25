#nullable enable

using System;
using System.Collections.Generic;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal enum CloudOperationTerminalOutcome
{
    Success,
    PartialSuccess,
    Timeout,
    Cancelled,
    Failure,
}

internal readonly record struct CloudOperationTerminalPresentation(
    CloudOperationTerminalOutcome Outcome,
    string StatusText,
    string LogText
)
{
    internal static CloudOperationTerminalPresentation CreateCompletion(
        CloudPostOperationResolution resolution
    )
    {
        var result = resolution.Result;
        var outcome = result.Completion switch
        {
            ManualCloudSyncCompletion.Success
                when (
                    resolution.CompletionEvidenceRequired
                    && !resolution.CompletionEvidenceRecorded
                )
                => CloudOperationTerminalOutcome.PartialSuccess,
            ManualCloudSyncCompletion.Success
                => CloudOperationTerminalOutcome.Success,
            ManualCloudSyncCompletion.PartialSuccess
                => CloudOperationTerminalOutcome.PartialSuccess,
            _ => CloudOperationTerminalOutcome.Failure,
        };

        var operation = OperationName(result.Kind);
        var counts = ResultCounts(result);
        string status;
        if (
            resolution.CompletionEvidenceRequired
            && result.CanRecordCompletionEvidence
            && !resolution.CompletionEvidenceRecorded
        )
        {
            status =
                $"{operation} downloaded saves, but completion evidence could not be recorded. "
                + $"{counts} Upload remains locked.";
        }
        else
        {
            status = (result.Kind, outcome) switch
            {
                (
                    CloudOperationKind.Pull,
                    CloudOperationTerminalOutcome.Success
                )
                    => $"{operation} succeeded. {counts} "
                        + UploadState(resolution.Snapshot.UploadEligibility),
                (
                    CloudOperationKind.Pull,
                    CloudOperationTerminalOutcome.PartialSuccess
                )
                    => $"{operation} partially succeeded. {counts} "
                        + "Completion evidence was not recorded; Upload remains locked.",
                (CloudOperationKind.Pull, _)
                    => $"{operation} failed to import any saves. {counts} "
                    + "Completion evidence was not recorded; Upload remains locked.",
                (
                    CloudOperationKind.Push,
                    CloudOperationTerminalOutcome.Success
                )
                    => $"Upload succeeded. {counts} Steam Cloud was updated from Android local saves.",
                (
                    CloudOperationKind.Push,
                    CloudOperationTerminalOutcome.PartialSuccess
                )
                    => $"Upload partially succeeded. {counts} Some saves were not updated in Steam Cloud; review the counts before retrying.",
                _ => $"Upload failed to transfer any saves. {counts} Steam Cloud was not updated.",
            };
        }

        return new CloudOperationTerminalPresentation(
            outcome,
            status.Trim(),
            AppendDetail(status.Trim(), result.Detail)
        );
    }

    internal static CloudOperationTerminalPresentation CreateTimeout(
        string operation,
        CloudOperationState state,
        string reason,
        CloudPostOperationSnapshot snapshot
    )
    {
        var status =
            $"{operation} timed out. {StateCounts(state)} "
            + $"{Unfinished(state)} {NextAction(state, snapshot)}";
        return new CloudOperationTerminalPresentation(
            CloudOperationTerminalOutcome.Timeout,
            status.Trim(),
            $"{status.Trim()} Reason: {SingleLine(reason)}"
        );
    }

    internal static CloudOperationTerminalPresentation CreateFailure(
        string operation,
        CloudOperationState state,
        string reason,
        CloudPostOperationSnapshot snapshot
    )
    {
        var status =
            $"{operation} failed: {SingleLine(reason)}. "
            + $"{StateCounts(state)} {Unfinished(state)} "
            + NextAction(state, snapshot);
        return new CloudOperationTerminalPresentation(
            CloudOperationTerminalOutcome.Failure,
            status.Trim(),
            status.Trim()
        );
    }

    internal static CloudOperationTerminalPresentation CreateCancelled(
        string operation,
        CloudOperationState state,
        CloudPostOperationSnapshot snapshot
    )
    {
        var status =
            $"{operation} cancelled. {StateCounts(state)} {Unfinished(state)} "
            + "No hidden cloud work remains. "
            + NextAction(state, snapshot);
        return new CloudOperationTerminalPresentation(
            CloudOperationTerminalOutcome.Cancelled,
            status.Trim(),
            status.Trim()
        );
    }

    private static string ResultCounts(ManualCloudSyncResult result)
    {
        var terms = new List<string>
        {
            result.Kind == CloudOperationKind.Pull
                ? $"{result.CompletedPathCount} downloaded"
                : $"{result.CompletedPathCount} uploaded",
        };
        AddCount(terms, result.SkippedPathCount, "unavailable");
        AddCount(terms, result.FailedPathCount, "failed");
        AddCount(terms, result.TimedOutPathCount, "timed out");
        AddCount(terms, result.UnprocessedPathCount, "unfinished");
        AddCount(
            terms,
            result.PostProcessingErrorCount,
            "post-processing errors"
        );
        AddCount(terms, result.ProfileSeededCount, "modded profiles seeded");
        AddCount(
            terms,
            result.BackupCreatedCount + result.PrivateBackupCreatedCount,
            "safety backups created"
        );
        return string.Join(", ", terms) + ".";
    }

    private static string StateCounts(CloudOperationState state)
    {
        var terms = new List<string>
        {
            state.Kind == CloudOperationKind.Pull
                ? $"{state.TransferCompletedCount} downloaded"
                : $"{state.TransferCompletedCount} uploaded",
        };
        AddCount(terms, state.TransferSkippedCount, "unavailable");
        AddCount(terms, state.TransferFailedCount, "failed");
        AddCount(terms, state.TransferTimedOutCount, "timed out");
        return string.Join(", ", terms) + ".";
    }

    private static string Unfinished(CloudOperationState state)
    {
        var unfinished = Math.Max(
            0,
            state.TransferTotalCount - state.TransferProcessedCount
        );
        return $"{unfinished} unfinished.";
    }

    private static string UploadState(CloudPushEligibilityResult eligibility)
    {
        if (eligibility.IsEligible)
            return "Upload is now available.";

        if (eligibility.BlockingReasons.Count == 0)
            return "Upload remains locked.";

        return $"Upload remains locked: {eligibility.BlockingReasons[0].Reason}";
    }

    private static string NextAction(
        CloudOperationState state,
        CloudPostOperationSnapshot snapshot
    )
        => state.Kind == CloudOperationKind.Pull
            ? UploadState(snapshot.UploadEligibility)
            : "Review the result before retrying Upload.";

    private static string OperationName(CloudOperationKind kind)
        => kind == CloudOperationKind.Pull ? "Pull" : "Upload";

    private static void AddCount(
        ICollection<string> terms,
        int count,
        string label
    )
    {
        if (count > 0)
            terms.Add($"{count} {label}");
    }

    private static string AppendDetail(string status, string detail)
        => string.IsNullOrWhiteSpace(detail)
            ? status
            : $"{status} {detail.Trim()}";

    private static string SingleLine(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "Unknown error"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
