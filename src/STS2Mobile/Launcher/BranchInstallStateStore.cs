using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class BranchInstallStateStore
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProcessLocks = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
    );

    internal static readonly BranchInstallStateStore Current = new(
        new AtomicFileWriter(),
        () => DateTimeOffset.UtcNow
    );

    private readonly AtomicFileWriter _atomicWriter;
    private readonly Func<DateTimeOffset> _utcNow;

    internal BranchInstallStateStore(
        AtomicFileWriter atomicWriter,
        Func<DateTimeOffset> utcNow = null
    )
    {
        _atomicWriter = atomicWriter ?? throw new ArgumentNullException(nameof(atomicWriter));
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    internal static string PathFor(string dataDir, string branch)
        => SteamGameInstallPaths.InstallationStatePath(
            RequireDataDirectory(dataDir),
            SteamGameBranch.StorageIdentity(branch)
        );

    internal BranchInstallState Read(string dataDir, string branch)
    {
        var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
        var path = PathFor(dataDir, normalizedBranch);
        if (!File.Exists(path))
        {
            var legacyPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, normalizedBranch);
            if (File.Exists(legacyPath))
            {
                throw Failure(
                    BranchInstallStateFailureKind.LegacyRequiresRecovery,
                    $"Branch '{normalizedBranch}' has only legacy install evidence and must be reinspected through recovery before launch.",
                    legacyPath
                );
            }

            throw Failure(
                BranchInstallStateFailureKind.Missing,
                $"Branch '{normalizedBranch}' has no installation state and is not ready.",
                path
            );
        }

        return ReadStateFile(path, normalizedBranch);
    }

    internal bool TryReadReady(
        string dataDir,
        string branch,
        GameIdentity currentIdentity,
        out BranchInstallState state,
        out string problem
    )
    {
        state = null;
        problem = string.Empty;
        if (currentIdentity == null)
        {
            problem = "Current authoritative game identity is unavailable.";
            return false;
        }

        try
        {
            state = Read(dataDir, branch);
        }
        catch (BranchInstallStateException ex)
        {
            problem = ex.Message;
            return false;
        }

        if (!state.IsReady)
        {
            problem = $"Branch '{state.Branch}' is updating in phase '{state.Phase}' and is not ready to launch.";
            return false;
        }
        if (!string.Equals(
                state.PckPreparationVersion,
                DepotDownloader.AndroidPckPreparationVersion,
                StringComparison.Ordinal
            ))
        {
            problem =
                $"Branch '{state.Branch}' requires Android PCK preparation "
                + $"'{DepotDownloader.AndroidPckPreparationVersion}' before launch; "
                + $"installed version is '{state.PckPreparationVersion}'. Update the selected version to repair it.";
            state = null;
            return false;
        }
        if (state.GameIdentity == currentIdentity)
            return true;

        problem = !string.Equals(
            state.GameIdentity.InstallGeneration,
            currentIdentity.InstallGeneration,
            StringComparison.Ordinal
        )
            ? $"Ready install generation mismatch for branch '{state.Branch}'."
            : $"Ready game identity mismatch for branch '{state.Branch}'.";
        state = null;
        return false;
    }

    internal BranchInstallState RequireReady(
        string dataDir,
        string branch,
        GameIdentity currentIdentity
    )
    {
        if (TryReadReady(dataDir, branch, currentIdentity, out var state, out var problem))
            return state;

        throw Failure(
            BranchInstallStateFailureKind.IdentityMismatch,
            problem,
            PathFor(dataDir, branch)
        );
    }

    internal BranchInstallState BeginUpdating(
        string dataDir,
        string branch,
        Guid transactionId,
        string phase,
        IReadOnlyList<BranchInstallDepot> targetDepots,
        string lastError = ""
    )
        => WithWriteLock(dataDir, branch, normalizedBranch =>
        {
            var path = PathFor(dataDir, normalizedBranch);
            var current = ReadForRecovery(path, normalizedBranch);
            if (current?.Status == BranchInstallStatus.Updating)
            {
                if (current.TransactionId != transactionId)
                {
                    throw Failure(
                        BranchInstallStateFailureKind.InvalidTransition,
                        $"Branch '{normalizedBranch}' is already updating under transaction '{current.TransactionId:D}'.",
                        path
                    );
                }

                var repeated = BranchInstallState.Updating(
                    normalizedBranch,
                    transactionId,
                    phase,
                    current.TransitionUtc,
                    targetDepots ?? current.Depots,
                    lastError
                );
                Write(path, repeated);
                return repeated;
            }
            if (current?.Status == BranchInstallStatus.Ready
                && current.TransactionId == transactionId)
            {
                throw Failure(
                    BranchInstallStateFailureKind.InvalidTransition,
                    $"Transaction '{transactionId:D}' for branch '{normalizedBranch}' is already ready and cannot be restarted.",
                    path
                );
            }

            var updating = BranchInstallState.Updating(
                normalizedBranch,
                transactionId,
                phase,
                _utcNow(),
                targetDepots,
                lastError
            );
            Write(path, updating);
            return updating;
        });

    internal BranchInstallState UpdateProgress(
        string dataDir,
        string branch,
        Guid transactionId,
        string phase,
        string lastError = ""
    )
        => WithWriteLock(dataDir, branch, normalizedBranch =>
        {
            var path = PathFor(dataDir, normalizedBranch);
            var current = ReadStateFile(path, normalizedBranch);
            RequireUpdatingTransaction(current, transactionId, path);
            var updated = BranchInstallState.Updating(
                normalizedBranch,
                transactionId,
                phase,
                current.TransitionUtc,
                current.Depots,
                lastError
            );
            Write(path, updated);
            return updated;
        });

    internal BranchInstallState CommitReady(
        string dataDir,
        string branch,
        Guid transactionId,
        GameIdentity gameIdentity,
        IReadOnlyList<BranchInstallDepot> depots,
        string pckPreparationVersion,
        BranchInstallRuntimePack runtimePack = null
    )
        => WithWriteLock(dataDir, branch, normalizedBranch =>
        {
            var path = PathFor(dataDir, normalizedBranch);
            var candidate = BranchInstallState.Ready(
                normalizedBranch,
                transactionId,
                _utcNow(),
                depots,
                pckPreparationVersion,
                gameIdentity,
                runtimePack
            );
            var current = ReadStateFile(path, normalizedBranch);
            if (current.IsReady)
            {
                if (current.HasSameReadyPayload(candidate))
                    return current;

                throw Failure(
                    BranchInstallStateFailureKind.InvalidTransition,
                    $"Branch '{normalizedBranch}' is already ready with different transaction or identity data.",
                    path
                );
            }

            RequireUpdatingTransaction(current, transactionId, path);
            Write(path, candidate);
            return candidate;
        });

    private BranchInstallState WithWriteLock(
        string dataDir,
        string branch,
        Func<string, BranchInstallState> operation
    )
    {
        var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
        var lockPath = SteamGameInstallPaths.InstallationStateLockPath(
            RequireDataDirectory(dataDir),
            normalizedBranch
        );
        var fullLockPath = Path.GetFullPath(lockPath);
        var processLock = ProcessLocks.GetOrAdd(fullLockPath, _ => new SemaphoreSlim(1, 1));
        processLock.Wait();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullLockPath)!);
            FileStream fileLock;
            try
            {
                fileLock = new FileStream(
                    fullLockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None
                );
            }
            catch (IOException ex)
            {
                throw Failure(
                    BranchInstallStateFailureKind.Busy,
                    $"Branch '{normalizedBranch}' installation state is locked by another updater.",
                    fullLockPath,
                    ex
                );
            }

            using (fileLock)
                return operation(normalizedBranch);
        }
        finally
        {
            processLock.Release();
        }
    }

    private BranchInstallState ReadForRecovery(string path, string branch)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            return ReadStateFile(path, branch);
        }
        catch (BranchInstallStateException ex) when (
            ex.Kind is BranchInstallStateFailureKind.Corrupt
                or BranchInstallStateFailureKind.UnknownSchema
                or BranchInstallStateFailureKind.BranchMismatch
                or BranchInstallStateFailureKind.LegacyRequiresRecovery
        )
        {
            return null;
        }
    }

    private void Write(string path, BranchInstallState state)
        => _atomicWriter.WriteAllBytes(path, Serialize(state));

    private static void RequireUpdatingTransaction(
        BranchInstallState state,
        Guid transactionId,
        string path
    )
    {
        if (state.Status == BranchInstallStatus.Updating
            && state.TransactionId == transactionId)
        {
            return;
        }

        throw Failure(
            BranchInstallStateFailureKind.InvalidTransition,
            $"Ready commit requires the matching updating transaction '{transactionId:D}'.",
            path
        );
    }

    private static string RequireDataDirectory(string dataDir)
        => string.IsNullOrWhiteSpace(dataDir)
            ? throw new ArgumentException("A launcher data directory is required.", nameof(dataDir))
            : dataDir;

    private static BranchInstallStateException Failure(
        BranchInstallStateFailureKind kind,
        string message,
        string path,
        Exception innerException = null
    ) => new(kind, message, path, innerException);
}
