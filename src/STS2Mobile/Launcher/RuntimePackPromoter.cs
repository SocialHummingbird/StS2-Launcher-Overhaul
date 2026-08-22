using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class RuntimePackPromoter
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProcessLocks = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
    );

    internal static RuntimePackPromotionResult PromoteOrValidate(
        string dataDir,
        GameIdentity expectedIdentity,
        RuntimePackCandidate candidate,
        RuntimePackPromotionHooks hooks = null
    )
    {
        if (expectedIdentity == null)
            return RuntimePackPromotionResult.Rejected(string.Empty, "An authoritative GameIdentity is required for runtime-pack promotion.");

        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(
            dataDir,
            expectedIdentity.Branch
        );
        var fullFinalDirectory = Path.GetFullPath(finalDirectory);
        var processLock = ProcessLocks.GetOrAdd(
            fullFinalDirectory,
            _ => new SemaphoreSlim(1, 1)
        );
        processLock.Wait();
        try
        {
            return WithInterprocessLock(
                dataDir,
                expectedIdentity,
                candidate,
                hooks,
                fullFinalDirectory
            );
        }
        finally
        {
            processLock.Release();
        }
    }

    private static RuntimePackPromotionResult WithInterprocessLock(
        string dataDir,
        GameIdentity expectedIdentity,
        RuntimePackCandidate candidate,
        RuntimePackPromotionHooks hooks,
        string finalDirectory
    )
    {
        var parent = Path.GetDirectoryName(finalDirectory);
        if (string.IsNullOrWhiteSpace(parent))
            return RuntimePackPromotionResult.Rejected(finalDirectory, "Cannot resolve the runtime-pack parent directory.");

        Directory.CreateDirectory(parent);
        var lockPath = finalDirectory + ".promotion.lock";
        try
        {
            using var lockStream = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None
            );
            return PromoteLocked(
                dataDir,
                expectedIdentity,
                candidate,
                hooks,
                finalDirectory
            );
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return RuntimePackPromotionResult.Rejected(
                finalDirectory,
                $"Runtime-pack promotion lock could not be acquired: {ex.GetBaseException().Message}"
            );
        }
    }

    private static RuntimePackPromotionResult PromoteLocked(
        string dataDir,
        GameIdentity expectedIdentity,
        RuntimePackCandidate candidate,
        RuntimePackPromotionHooks hooks,
        string finalDirectory
    )
    {
        string backupDirectory = null;
        var candidateMoved = false;
        var previousPackBackedUp = false;
        try
        {
            RequireCurrentReadyIdentity(dataDir, expectedIdentity, expectedTransactionId: null);
            RecoverInterruptedPromotion(finalDirectory, expectedIdentity);

            var existing = ValidateDirectory(finalDirectory, expectedIdentity);
            if (existing.Usable)
            {
                if (candidate != null && candidate.GameIdentity != expectedIdentity)
                    throw new InvalidDataException("The staged runtime-pack candidate belongs to a different GameIdentity.");

                RequireCurrentReadyIdentity(dataDir, expectedIdentity, expectedTransactionId: null);
                CleanupOwnedAttempts(finalDirectory, keepPath: null);
                return RuntimePackPromotionResult.Success(
                    existing.Manifest,
                    finalDirectory,
                    promoted: false
                );
            }

            if (candidate == null)
            {
                throw new InvalidDataException(
                    $"No valid staged candidate exists and the installed runtime pack is unusable: {existing.Problem}"
                );
            }
            RequireCandidate(finalDirectory, expectedIdentity, candidate);
            RequireCurrentReadyIdentity(
                dataDir,
                expectedIdentity,
                candidate.InstallTransactionId
            );

            var staged = ValidateDirectory(candidate.StagingDirectory, expectedIdentity);
            if (!staged.Usable)
                throw new InvalidDataException($"Staged runtime-pack validation failed: {staged.Problem}");

            hooks?.BeforePromotion?.Invoke();
            RequireCurrentReadyIdentity(
                dataDir,
                expectedIdentity,
                candidate.InstallTransactionId
            );

            backupDirectory = finalDirectory + $".backup.{candidate.InstallTransactionId:N}";
            if (Directory.Exists(backupDirectory) || File.Exists(backupDirectory))
                throw new IOException($"An unresolved runtime-pack backup already exists: {backupDirectory}.");

            if (File.Exists(finalDirectory))
                throw new IOException($"The runtime-pack target is a file, not a directory: {finalDirectory}.");
            if (Directory.Exists(finalDirectory))
            {
                Directory.Move(finalDirectory, backupDirectory);
                previousPackBackedUp = true;
                hooks?.AfterExistingPackBackedUp?.Invoke();
            }

            Directory.Move(candidate.StagingDirectory, finalDirectory);
            candidateMoved = true;
            hooks?.AfterCandidatePromoted?.Invoke();
            hooks?.BeforePromotedValidation?.Invoke();

            var promoted = ValidateDirectory(finalDirectory, expectedIdentity);
            if (!promoted.Usable)
                throw new InvalidDataException($"Promoted runtime-pack validation failed: {promoted.Problem}");

            RequireCurrentReadyIdentity(
                dataDir,
                expectedIdentity,
                candidate.InstallTransactionId
            );

            TryDeleteObsoleteDirectory(backupDirectory);
            backupDirectory = null;
            CleanupOwnedAttempts(finalDirectory, keepPath: null);
            PatchHelper.Log(
                $"[Launcher] Promoted validated runtime pack for '{expectedIdentity.Branch}' identity={expectedIdentity.Id} path={finalDirectory}"
            );
            return RuntimePackPromotionResult.Success(
                promoted.Manifest,
                finalDirectory,
                promoted: true
            );
        }
        catch (Exception ex)
        {
            var recoveryProblem = RestorePreviousPack(
                candidate,
                finalDirectory,
                backupDirectory,
                candidateMoved,
                previousPackBackedUp
            );
            var problem = ex.GetBaseException().Message;
            if (!string.IsNullOrWhiteSpace(recoveryProblem))
                problem += $" Recovery also failed: {recoveryProblem}";
            PatchHelper.Log(
                $"[Launcher] Runtime-pack promotion rejected for '{expectedIdentity.Branch}' identity={expectedIdentity.Id}: {problem}"
            );
            return RuntimePackPromotionResult.Rejected(finalDirectory, problem);
        }
    }

    private static void RequireCandidate(
        string finalDirectory,
        GameIdentity expectedIdentity,
        RuntimePackCandidate candidate
    )
    {
        if (candidate.GameIdentity != expectedIdentity)
            throw new InvalidDataException("The staged runtime-pack candidate belongs to a different GameIdentity.");
        if (candidate.InstallTransactionId == Guid.Empty || candidate.AttemptId == Guid.Empty)
            throw new InvalidDataException("The staged runtime-pack candidate has incomplete transaction identity.");

        var expectedPrefix = Path.GetFullPath(finalDirectory) + ".staging."
            + candidate.InstallTransactionId.ToString("N") + ".";
        var candidatePath = Path.GetFullPath(candidate.StagingDirectory);
        if (!candidatePath.StartsWith(expectedPrefix, PathComparison()))
        {
            throw new InvalidDataException(
                "The staged runtime-pack candidate path is not scoped to the selected branch and install transaction."
            );
        }
    }

    private static RuntimePackCandidateValidationResult ValidateDirectory(
        string directory,
        GameIdentity expectedIdentity
    ) => RuntimePackCandidateValidator.Validate(
        directory,
        expectedIdentity,
        PatchCompatibilityValidator.PatchSetVersion,
        PatchCompatibilityValidator.ValidationMode,
        PatchCompatibilityValidator.ValidationSurfaceVersion
    );

    private static void RequireCurrentReadyIdentity(
        string dataDir,
        GameIdentity expectedIdentity,
        Guid? expectedTransactionId
    )
    {
        var state = BranchInstallStateStore.Current.RequireReady(
            dataDir,
            expectedIdentity.Branch,
            expectedIdentity
        );
        if (expectedTransactionId.HasValue
            && state.TransactionId != expectedTransactionId.Value)
        {
            throw new InvalidDataException(
                "The selected branch ready transaction does not match the staged runtime-pack candidate."
            );
        }

        var actualIdentity = GameIdentityReader.ReadInstalled(
            dataDir,
            expectedIdentity.Branch
        );
        if (actualIdentity != expectedIdentity)
        {
            throw new InvalidDataException(
                $"The selected branch GameIdentity changed during runtime-pack promotion: expected={expectedIdentity.Id}; actual={actualIdentity.Id}."
            );
        }
    }

    private static void RecoverInterruptedPromotion(
        string finalDirectory,
        GameIdentity expectedIdentity
    )
    {
        var backups = EnumerateOwnedDirectories(finalDirectory, ".backup.").ToArray();
        if (backups.Length > 1)
        {
            throw new IOException(
                $"Multiple interrupted runtime-pack backups require recovery: {string.Join(", ", backups)}."
            );
        }
        if (backups.Length == 0)
            return;

        var backup = backups[0];
        if (!Directory.Exists(finalDirectory))
        {
            Directory.Move(backup, finalDirectory);
            return;
        }

        var finalValidation = ValidateDirectory(finalDirectory, expectedIdentity);
        if (finalValidation.Usable)
        {
            TryDeleteObsoleteDirectory(backup);
            return;
        }

        var rejected = finalDirectory + ".rejected." + Guid.NewGuid().ToString("N");
        Directory.Move(finalDirectory, rejected);
        try
        {
            Directory.Move(backup, finalDirectory);
            TryDeleteObsoleteDirectory(rejected);
        }
        catch
        {
            if (!Directory.Exists(finalDirectory) && Directory.Exists(rejected))
                Directory.Move(rejected, finalDirectory);
            throw;
        }
    }

    private static string RestorePreviousPack(
        RuntimePackCandidate candidate,
        string finalDirectory,
        string backupDirectory,
        bool candidateMoved,
        bool previousPackBackedUp
    )
    {
        try
        {
            if (candidateMoved && Directory.Exists(finalDirectory))
            {
                if (candidate != null && !Directory.Exists(candidate.StagingDirectory))
                    Directory.Move(finalDirectory, candidate.StagingDirectory);
                else
                    DeleteDirectory(finalDirectory);
            }

            if (previousPackBackedUp
                && !string.IsNullOrWhiteSpace(backupDirectory)
                && Directory.Exists(backupDirectory)
                && !Directory.Exists(finalDirectory))
            {
                Directory.Move(backupDirectory, finalDirectory);
            }
            return string.Empty;
        }
        catch (Exception recoveryError)
        {
            return recoveryError.GetBaseException().Message;
        }
    }

    private static void CleanupOwnedAttempts(string finalDirectory, string keepPath)
    {
        foreach (var directory in EnumerateOwnedDirectories(finalDirectory, ".staging.")
            .Concat(EnumerateOwnedDirectories(finalDirectory, ".backup.")))
        {
            if (!string.IsNullOrWhiteSpace(keepPath)
                && string.Equals(
                    Path.GetFullPath(directory),
                    Path.GetFullPath(keepPath),
                    PathComparison()
                ))
            {
                continue;
            }
            TryDeleteObsoleteDirectory(directory);
        }
    }

    private static System.Collections.Generic.IEnumerable<string> EnumerateOwnedDirectories(
        string finalDirectory,
        string suffix
    )
    {
        var parent = Path.GetDirectoryName(finalDirectory);
        if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
            return Array.Empty<string>();
        return Directory.EnumerateDirectories(
            parent,
            Path.GetFileName(finalDirectory) + suffix + "*",
            SearchOption.TopDirectoryOnly
        );
    }

    private static void DeleteDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private static void TryDeleteObsoleteDirectory(string path)
    {
        try
        {
            DeleteDirectory(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Launcher] Deferred cleanup of obsolete runtime-pack directory '{path}': {ex.GetBaseException().Message}"
            );
        }
    }

    private static StringComparison PathComparison()
        => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
