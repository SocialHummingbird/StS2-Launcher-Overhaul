#nullable enable

using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal enum CloudOperationTerminalOutcome
{
    Success,
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
        var counts = ResultCounts(result);
        string status;
        if (result.Kind == CloudOperationKind.Pull)
        {
            status =
                $"Pull succeeded. {counts} Android local saves now match the verified Steam snapshot.";
        }
        else
        {
            status =
                $"Upload succeeded. {counts} Steam Cloud was updated from Android local saves.";
        }

        return new CloudOperationTerminalPresentation(
            CloudOperationTerminalOutcome.Success,
            status.Trim(),
            AppendDetail(status.Trim(), result.Detail)
        );
    }

    internal static CloudOperationTerminalPresentation CreateTimeout(
        string operation,
        CloudOperationState state,
        string reason
    )
    {
        var status =
            $"{operation} timed out. {StateCounts(state)} "
            + $"{Unfinished(state)} {NextAction(state)}";
        return new CloudOperationTerminalPresentation(
            CloudOperationTerminalOutcome.Timeout,
            status.Trim(),
            $"{status.Trim()} Reason: {SingleLine(reason)}"
        );
    }

    internal static CloudOperationTerminalPresentation CreateFailure(
        string operation,
        CloudOperationState state,
        string reason
    )
    {
        var status =
            $"{operation} failed: {SingleLine(reason)}. "
            + $"{StateCounts(state)} {Unfinished(state)} "
            + NextAction(state);
        return new CloudOperationTerminalPresentation(
            CloudOperationTerminalOutcome.Failure,
            status.Trim(),
            status.Trim()
        );
    }

    internal static CloudOperationTerminalPresentation CreateCancelled(
        string operation,
        CloudOperationState state
    )
    {
        var status =
            $"{operation} cancelled. {StateCounts(state)} {Unfinished(state)} "
            + "No hidden cloud work remains. "
            + NextAction(state);
        return new CloudOperationTerminalPresentation(
            CloudOperationTerminalOutcome.Cancelled,
            status.Trim(),
            status.Trim()
        );
    }

    private static string ResultCounts(ManualCloudSyncResult result)
    {
        var transferred = $"{result.TransferredPathCount} verified";
        var backups = result.BackupCreatedCount > 0
            ? $", {result.BackupCreatedCount} safety backups created"
            : "";
        return transferred + backups + ".";
    }

    private static string StateCounts(CloudOperationState state)
        => $"{state.TransferCompletedCount} verified.";

    private static string Unfinished(CloudOperationState state)
    {
        var unfinished = Math.Max(
            0,
            state.TransferTotalCount - state.TransferProcessedCount
        );
        return $"{unfinished} unfinished.";
    }

    private static string NextAction(CloudOperationState state)
        => state.Kind == CloudOperationKind.Pull
            ? "Review the failure before retrying Pull."
            : "Review the result before retrying Upload.";

    private static string AppendDetail(string status, string detail)
        => string.IsNullOrWhiteSpace(detail)
            ? status
            : $"{status} {detail.Trim()}";

    private static string SingleLine(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "Unknown error"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
