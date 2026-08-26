using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class BranchInstallStateStore
{
    internal UpdateSession BeginOrResumeUpdate(
        string dataDir,
        string branch,
        string phase,
        IReadOnlyList<BranchInstallDepot> targetDepots,
        Func<BranchInstallState, bool> shouldBeginUpdate = null
    )
    {
        var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
        var normalizedDataDir = RequireDataDirectory(dataDir);
        var statePath = PathFor(normalizedDataDir, normalizedBranch);
        var lockPath = Path.GetFullPath(
            SteamGameInstallPaths.InstallationStateLockPath(
                normalizedDataDir,
                normalizedBranch
            )
        );
        var processLock = ProcessLocks.GetOrAdd(
            lockPath,
            _ => new SemaphoreSlim(1, 1)
        );
        if (!processLock.Wait(0))
        {
            throw Failure(
                BranchInstallStateFailureKind.Busy,
                $"Branch '{normalizedBranch}' is already being updated in this process.",
                lockPath
            );
        }

        FileStream fileLock = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
            try
            {
                fileLock = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None
                );
            }
            catch (IOException ex)
            {
                throw Failure(
                    BranchInstallStateFailureKind.Busy,
                    $"Branch '{normalizedBranch}' installation is locked by another updater.",
                    lockPath,
                    ex
                );
            }

            var current = ReadForRecovery(statePath, normalizedBranch);
            if (shouldBeginUpdate != null && !shouldBeginUpdate(current))
            {
                fileLock.Dispose();
                fileLock = null;
                processLock.Release();
                return null;
            }

            var transactionId = current?.Status == BranchInstallStatus.Updating
                ? current.TransactionId
                : Guid.NewGuid();
            var startedUtc = current?.Status == BranchInstallStatus.Updating
                ? current.TransitionUtc
                : _utcNow();
            var effectiveTargetDepots = targetDepots
                ?? current?.Depots
                ?? Array.Empty<BranchInstallDepot>();
            var updating = BranchInstallState.Updating(
                normalizedBranch,
                transactionId,
                phase,
                startedUtc,
                effectiveTargetDepots,
                current?.Status == BranchInstallStatus.Updating
                    ? current.LastError
                    : string.Empty
            );
            Write(statePath, updating);

            return new UpdateSession(
                this,
                normalizedDataDir,
                normalizedBranch,
                statePath,
                updating,
                fileLock,
                processLock
            );
        }
        catch
        {
            fileLock?.Dispose();
            processLock.Release();
            throw;
        }
    }

    internal sealed class UpdateSession : IDisposable
    {
        private readonly BranchInstallStateStore _store;
        private readonly string _statePath;
        private FileStream _fileLock;
        private SemaphoreSlim _processLock;
        private BranchInstallState _state;

        internal UpdateSession(
            BranchInstallStateStore store,
            string dataDir,
            string branch,
            string statePath,
            BranchInstallState state,
            FileStream fileLock,
            SemaphoreSlim processLock
        )
        {
            _store = store;
            DataDir = dataDir;
            Branch = branch;
            _statePath = statePath;
            _state = state;
            _fileLock = fileLock;
            _processLock = processLock;
        }

        internal string DataDir { get; }
        internal string Branch { get; }
        internal Guid TransactionId => _state.TransactionId;
        internal BranchInstallState State => _state;

        internal void RequireUpdating()
        {
            EnsureActive();
            if (_state.Status != BranchInstallStatus.Updating)
            {
                throw Failure(
                    BranchInstallStateFailureKind.InvalidTransition,
                    $"Branch '{Branch}' must be updating before installed files can change.",
                    _statePath
                );
            }
        }

        internal BranchInstallState UpdateProgress(
            string phase,
            string lastError = ""
        )
        {
            RequireUpdating();
            var updated = BranchInstallState.Updating(
                Branch,
                TransactionId,
                phase,
                _state.TransitionUtc,
                _state.Depots,
                lastError
            );
            _store.Write(_statePath, updated);
            _state = updated;
            return updated;
        }

        internal BranchInstallState CommitReady(
            GameIdentity identity,
            string pckPreparationVersion,
            BranchInstallRuntimePack runtimePack = null
        )
        {
            RequireUpdating();
            var ready = BranchInstallState.Ready(
                Branch,
                TransactionId,
                _store._utcNow(),
                _state.Depots,
                pckPreparationVersion,
                identity,
                runtimePack
            );
            _store.Write(_statePath, ready);
            _state = ready;
            return ready;
        }

        internal void RecordFailure(string phase, Exception exception)
        {
            if (_fileLock == null || _state.Status != BranchInstallStatus.Updating)
                return;

            var message = exception?.GetBaseException().Message ?? "Unknown update failure.";
            try
            {
                UpdateProgress(phase, message);
            }
            catch
            {
                // The previous durable updating state already fails closed.
            }
        }

        private void EnsureActive()
        {
            if (_fileLock == null)
                throw new ObjectDisposedException(nameof(UpdateSession));
        }

        public void Dispose()
        {
            var fileLock = Interlocked.Exchange(ref _fileLock, null);
            var processLock = Interlocked.Exchange(ref _processLock, null);
            fileLock?.Dispose();
            processLock?.Release();
        }
    }
}
