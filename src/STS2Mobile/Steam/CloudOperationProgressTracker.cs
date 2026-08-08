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
            CurrentItem = state.Kind == CloudOperationKind.Pull
                ? "Checking the Android destination"
                : "Checking the Steam Cloud destination",
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

    internal void TransferStarted(int totalCount)
        => Update(state => state with
        {
            Phase = CloudOperationPhase.Transferring,
            CurrentItem = "Starting verified save transfer",
            TransferProcessedCount = 0,
            TransferTotalCount = totalCount,
            TransferCompletedCount = 0,
        });

    internal void TransferPathStarted(string path)
        => Update(state => state with { CurrentItem = path });

    internal void TransferProcessed(string path)
        => Update(state => state with
        {
            CurrentItem = path,
            TransferProcessedCount = state.TransferProcessedCount + 1,
            TransferCompletedCount = state.TransferCompletedCount + 1,
        });

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
