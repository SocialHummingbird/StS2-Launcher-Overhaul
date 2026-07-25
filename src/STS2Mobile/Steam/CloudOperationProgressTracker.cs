#nullable enable

using System;

namespace STS2Mobile.Steam;

internal sealed class CloudOperationProgressTracker
{
    private readonly object _stateLock = new();
    private readonly Action<CloudOperationState>? _publish;
    private CloudOperationState _state;

    internal CloudOperationProgressTracker(
        CloudOperationKind kind,
        Action<CloudOperationState>? publish = null
    )
    {
        _state = CloudOperationState.Idle(kind);
        _publish = publish;
    }

    internal CloudOperationState State
    {
        get
        {
            lock (_stateLock)
                return _state;
        }
    }

    internal void Preparing(string currentItem)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.Preparing,
            CurrentItem = currentItem,
            ErrorMessage = "",
        });

    internal void EnumerationStarted(string currentItem)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.Enumerating,
            CurrentItem = currentItem,
        });

    internal void EnumerationCompleted(int pathCount)
        => Update(state => state with
        {
            EnumeratedPathCount = pathCount,
            CurrentItem = $"{pathCount} candidate save path(s) found",
        });

    internal void BackupStarted(int totalCount)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.BackingUp,
            CurrentItem = "Checking existing Android saves",
            BackupProcessedCount = 0,
            BackupTotalCount = totalCount,
            BackupCreatedCount = 0,
        });

    internal void BackupProcessed(string path, bool created)
        => Update(state => state with
        {
            CurrentItem = path,
            BackupProcessedCount = state.BackupProcessedCount + 1,
            BackupCreatedCount = state.BackupCreatedCount + (created ? 1 : 0),
        });

    internal void BackupPathStarted(string path)
        => Update(state => state with { CurrentItem = path });

    internal void ProfilePreparationStarted(int totalCount)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.PreparingProfiles,
            CurrentItem = "Protecting local modded save targets",
            ProfilePreparationProcessedCount = 0,
            ProfilePreparationTotalCount = totalCount,
            PrivateBackupCreatedCount = 0,
        });

    internal void ProfilePreparationProcessed(string path, bool backupCreated)
        => Update(state => state with
        {
            CurrentItem = path,
            ProfilePreparationProcessedCount =
                state.ProfilePreparationProcessedCount + 1,
            PrivateBackupCreatedCount =
                state.PrivateBackupCreatedCount + (backupCreated ? 1 : 0),
        });

    internal void ProfilePreparationPathStarted(string path)
        => Update(state => state with { CurrentItem = path });

    internal void TransferStarted(int totalCount)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.Transferring,
            CurrentItem = "Starting Steam Cloud downloads",
            TransferProcessedCount = 0,
            TransferTotalCount = totalCount,
            TransferCompletedCount = 0,
            TransferSkippedCount = 0,
            TransferFailedCount = 0,
        });

    internal void TransferPathStarted(string path)
        => Update(state => state with { CurrentItem = path });

    internal void TransferProcessed(
        string path,
        bool completed,
        bool skipped
    )
        => TransferProcessed(
            path,
            completed
                ? CloudTransferPathOutcome.Completed
                : skipped
                    ? CloudTransferPathOutcome.Skipped
                    : CloudTransferPathOutcome.Failed
        );

    internal void TransferProcessed(
        string path,
        CloudTransferPathOutcome outcome
    )
        => Update(state => state with
        {
            CurrentItem = path,
            TransferProcessedCount = state.TransferProcessedCount + 1,
            TransferCompletedCount =
                state.TransferCompletedCount
                    + (outcome == CloudTransferPathOutcome.Completed ? 1 : 0),
            TransferSkippedCount =
                state.TransferSkippedCount
                    + (outcome == CloudTransferPathOutcome.Skipped ? 1 : 0),
            TransferFailedCount =
                state.TransferFailedCount
                    + (outcome == CloudTransferPathOutcome.Failed ? 1 : 0),
            TransferTimedOutCount =
                state.TransferTimedOutCount
                    + (outcome == CloudTransferPathOutcome.TimedOut ? 1 : 0),
        });

    internal void ProfileSeedingStarted(int totalCount)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.SeedingProfiles,
            CurrentItem = "Preparing modded profiles from fresh downloads",
            ProfileSeedProcessedCount = 0,
            ProfileSeedTotalCount = totalCount,
            ProfileSeededCount = 0,
        });

    internal void ProfileSeedProcessed(string path, bool seeded)
        => Update(state => state with
        {
            CurrentItem = path,
            ProfileSeedProcessedCount = state.ProfileSeedProcessedCount + 1,
            ProfileSeededCount = state.ProfileSeededCount + (seeded ? 1 : 0),
        });

    internal void ProfileSeedPathStarted(string path)
        => Update(state => state with { CurrentItem = path });

    internal void Finalizing(string currentItem)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.Finalizing,
            CurrentItem = currentItem,
        });

    internal void Completed(string currentItem)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.Completed,
            CurrentItem = currentItem,
            ErrorMessage = "",
        });

    internal void PartiallyCompleted(string currentItem)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.PartiallyCompleted,
            CurrentItem = currentItem,
            ErrorMessage = "",
        });

    internal void Failed(string errorMessage)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.Failed,
            CurrentItem = "",
            ErrorMessage = errorMessage,
        });

    private void Update(
        Func<CloudOperationState, CloudOperationState> update
    )
    {
        lock (_stateLock)
        {
            if (_state.IsTerminal)
                return;

            _state = update(_state);
            _publish?.Invoke(_state);
        }
    }
}
