using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly record struct CloudOperationPresentation(
    bool Visible,
    string PhaseText,
    string DetailText,
    double ProgressValue,
    bool IsComplete,
    bool IsPartial,
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
            CloudOperationPhase.BackingUp => "Backing up Android saves",
            CloudOperationPhase.PreparingProfiles => "Protecting modded profiles",
            CloudOperationPhase.Transferring =>
                state.Kind == CloudOperationKind.Pull
                    ? "Downloading Steam Cloud saves"
                    : "Uploading Android saves",
            CloudOperationPhase.SeedingProfiles => "Preparing modded profiles",
            CloudOperationPhase.Finalizing => $"Finishing {operation}",
            CloudOperationPhase.Completed => $"{operation} complete",
            CloudOperationPhase.PartiallyCompleted =>
                $"{operation} partially complete",
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
            state.Phase == CloudOperationPhase.PartiallyCompleted,
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
                    : $"Checking vanilla and modded save locations. {state.CurrentItem}",
            CloudOperationPhase.BackingUp =>
                $"{Count(state.BackupProcessedCount, state.BackupTotalCount)} checked | "
                    + $"{state.BackupCreatedCount} backup(s) created."
                    + Current(state.CurrentItem),
            CloudOperationPhase.PreparingProfiles =>
                $"{Count(state.ProfilePreparationProcessedCount, state.ProfilePreparationTotalCount)} checked | "
                    + $"{state.PrivateBackupCreatedCount} private backup(s) created."
                    + Current(state.CurrentItem),
            CloudOperationPhase.Transferring =>
                $"{Count(state.TransferProcessedCount, state.TransferTotalCount)} checked | "
                    + TransferCounts(state)
                    + "."
                    + Current(state.CurrentItem),
            CloudOperationPhase.SeedingProfiles =>
                $"{Count(state.ProfileSeedProcessedCount, state.ProfileSeedTotalCount)} checked | "
                    + $"{state.ProfileSeededCount} seeded from fresh cloud saves."
                    + Current(state.CurrentItem),
            CloudOperationPhase.Finalizing =>
                $"{Count(state.TransferProcessedCount, state.TransferTotalCount)} checked | "
                    + $"{state.TransferCompletedCount} {TransferVerb(state)} | "
                    + $"{state.ProfileSeededCount} modded file(s) seeded. "
                    + state.CurrentItem,
            CloudOperationPhase.Completed =>
                TerminalDetail(state, partial: false),
            CloudOperationPhase.PartiallyCompleted =>
                TerminalDetail(state, partial: true),
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
                StageProgress(10, 15, state.BackupProcessedCount, state.BackupTotalCount),
            CloudOperationPhase.PreparingProfiles =>
                StageProgress(
                    25,
                    10,
                    state.ProfilePreparationProcessedCount,
                    state.ProfilePreparationTotalCount
                ),
            CloudOperationPhase.Transferring =>
                StageProgress(
                    35,
                    50,
                    state.TransferProcessedCount,
                    state.TransferTotalCount
                ),
            CloudOperationPhase.SeedingProfiles =>
                StageProgress(
                    85,
                    10,
                    state.ProfileSeedProcessedCount,
                    state.ProfileSeedTotalCount
                ),
            CloudOperationPhase.Finalizing => 97,
            CloudOperationPhase.Completed => 100,
            CloudOperationPhase.PartiallyCompleted => FailureProgress(state),
            CloudOperationPhase.Failed => FailureProgress(state),
            _ => 0,
        };

    private static double FailureProgress(CloudOperationState state)
    {
        if (state.TransferTotalCount > 0)
        {
            return StageProgress(
                35,
                50,
                state.TransferProcessedCount,
                state.TransferTotalCount
            );
        }

        if (state.ProfilePreparationTotalCount > 0)
        {
            return StageProgress(
                25,
                10,
                state.ProfilePreparationProcessedCount,
                state.ProfilePreparationTotalCount
            );
        }

        if (state.BackupTotalCount > 0)
        {
            return StageProgress(
                10,
                15,
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

    private static string TerminalDetail(
        CloudOperationState state,
        bool partial
    )
        => $"{Count(state.TransferProcessedCount, state.TransferTotalCount)} checked | "
            + TransferCounts(state)
            + (
                state.Kind == CloudOperationKind.Pull
                    ? $" | {state.ProfileSeededCount} modded file(s) seeded"
                    : ""
            )
            + (
                partial
                    ? ". The operation finished with incomplete or failed work."
                    : state.Kind == CloudOperationKind.Pull
                        ? ". Pull is finished."
                        : ". Upload is finished."
            );

    private static string TransferCounts(CloudOperationState state)
    {
        var text =
            $"{state.TransferCompletedCount} {TransferVerb(state)} | "
            + $"{state.TransferSkippedCount} {SkippedLabel(state)}";
        if (state.TransferFailedCount > 0)
            text += $" | {state.TransferFailedCount} failed";
        if (state.TransferTimedOutCount > 0)
            text += $" | {state.TransferTimedOutCount} timed out";
        return text;
    }

    private static string TransferVerb(CloudOperationState state)
        => state.Kind == CloudOperationKind.Pull
            ? "downloaded"
            : "uploaded";

    private static string SkippedLabel(CloudOperationState state)
        => state.Kind == CloudOperationKind.Pull
            ? "not in cloud"
            : "unavailable locally";
}
