using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly record struct CloudOperationPresentation(
    bool Visible,
    string PhaseText,
    string DetailText,
    double ProgressValue,
    bool IsComplete,
    bool IsFailed
)
{
    internal static CloudOperationPresentation Create(
        CloudOperationState state
    )
    {
        var operation = state.Kind == CloudOperationKind.Pull
            ? "Pull"
            : "Upload";
        var phaseText = state.Phase switch
        {
            CloudOperationPhase.Preparing => $"Preparing {operation}",
            CloudOperationPhase.Enumerating =>
                state.Kind == CloudOperationKind.Pull
                    ? "Finding Steam Cloud saves"
                    : "Finding Android saves",
            CloudOperationPhase.BackingUp =>
                state.Kind == CloudOperationKind.Pull
                    ? "Backing up Android saves"
                    : "Backing up Steam Cloud saves",
            CloudOperationPhase.Transferring =>
                state.Kind == CloudOperationKind.Pull
                    ? "Downloading Steam Cloud saves"
                    : "Uploading Android saves",
            CloudOperationPhase.Finalizing => $"Finishing {operation}",
            CloudOperationPhase.Completed => $"{operation} complete",
            CloudOperationPhase.Failed => $"{operation} stopped",
            _ => "",
        };
        var detailText = DetailFor(state);

        return new CloudOperationPresentation(
            state.Phase != CloudOperationPhase.Idle,
            phaseText,
            detailText,
            ProgressFor(state),
            state.Phase == CloudOperationPhase.Completed,
            state.Phase == CloudOperationPhase.Failed
        );
    }

    private static string DetailFor(CloudOperationState state)
        => state.Phase switch
        {
            CloudOperationPhase.Preparing =>
                $"Preparing cloud access. {state.CurrentItem}",
            CloudOperationPhase.Enumerating =>
                state.EnumeratedPathCount > 0
                    ? $"{state.EnumeratedPathCount} candidate save path(s) found."
                    : $"Checking the selected save namespace. {state.CurrentItem}",
            CloudOperationPhase.BackingUp =>
                $"{Count(state.BackupProcessedCount, state.BackupTotalCount)} checked | "
                    + $"{state.BackupCreatedCount} backup(s) created."
                    + Current(state.CurrentItem),
            CloudOperationPhase.Transferring =>
                $"{Count(state.TransferProcessedCount, state.TransferTotalCount)} checked | "
                    + $"{state.TransferCompletedCount} verified"
                    + "."
                    + Current(state.CurrentItem),
            CloudOperationPhase.Finalizing =>
                $"{Count(state.TransferProcessedCount, state.TransferTotalCount)} checked | "
                    + $"{state.TransferCompletedCount} verified. "
                    + state.CurrentItem,
            CloudOperationPhase.Completed => TerminalDetail(state),
            CloudOperationPhase.Failed =>
                string.IsNullOrWhiteSpace(state.ErrorMessage)
                    ? "The cloud operation could not continue. Open Help & Reports for details."
                    : $"The cloud operation could not continue: {state.ErrorMessage}",
            _ => "",
        };

    private static double ProgressFor(CloudOperationState state)
        => state.Phase switch
        {
            CloudOperationPhase.Preparing => 4,
            CloudOperationPhase.Enumerating => 8,
            CloudOperationPhase.BackingUp =>
                StageProgress(10, 20, state.BackupProcessedCount, state.BackupTotalCount),
            CloudOperationPhase.Transferring =>
                StageProgress(
                    30,
                    60,
                    state.TransferProcessedCount,
                    state.TransferTotalCount
                ),
            CloudOperationPhase.Finalizing => 97,
            CloudOperationPhase.Completed => 100,
            CloudOperationPhase.Failed => FailureProgress(state),
            _ => 0,
        };

    private static double FailureProgress(CloudOperationState state)
    {
        if (state.TransferTotalCount > 0)
        {
            return StageProgress(
                30,
                60,
                state.TransferProcessedCount,
                state.TransferTotalCount
            );
        }

        if (state.BackupTotalCount > 0)
        {
            return StageProgress(
                10,
                20,
                state.BackupProcessedCount,
                state.BackupTotalCount
            );
        }

        return 8;
    }

    private static double StageProgress(
        double start,
        double span,
        int completed,
        int total
    )
    {
        if (total <= 0)
            return start + span;

        return start + span * Math.Clamp((double)completed / total, 0, 1);
    }

    private static string Count(int completed, int total)
        => total > 0 ? $"{completed}/{total}" : $"{completed}/0";

    private static string Current(string currentItem)
        => string.IsNullOrWhiteSpace(currentItem)
            ? ""
            : $"\nCurrent: {currentItem}";

    private static string TerminalDetail(CloudOperationState state)
        => $"{Count(state.TransferProcessedCount, state.TransferTotalCount)} checked | "
            + $"{state.TransferCompletedCount} verified | "
            + (state.Kind == CloudOperationKind.Pull
                ? "Pull is finished."
                : "Upload is finished.");

}
