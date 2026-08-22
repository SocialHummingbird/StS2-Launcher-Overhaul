using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal enum SelectedBranchRecoveryMode
{
    DerivedArtifacts,
    FullRedownload,
}

internal sealed class SelectedBranchRecoveryResult
{
    internal SelectedBranchRecoveryResult(
        string branch,
        SelectedBranchRecoveryMode mode,
        int removedArtifactCount,
        IReadOnlyList<string> failures
    )
    {
        Branch = branch;
        Mode = mode;
        RemovedArtifactCount = removedArtifactCount;
        Failures = failures ?? Array.Empty<string>();
    }

    internal string Branch { get; }
    internal SelectedBranchRecoveryMode Mode { get; }
    internal int RemovedArtifactCount { get; }
    internal IReadOnlyList<string> Failures { get; }
    internal bool Succeeded => Failures.Count == 0;

    internal void RequireSuccess()
    {
        if (!Succeeded)
        {
            throw new IOException(
                $"Selected-branch recovery for '{Branch}' did not complete: "
                    + string.Join(" | ", Failures)
            );
        }
    }
}

/// <summary>
/// The sole managed owner of selected-branch cleanup. Every target is resolved
/// from the normalized branch slot; global derived evidence is removed only
/// when it belongs to that branch or is too corrupt to have a trustworthy owner.
/// </summary>
internal static class SelectedBranchRecovery
{
    private const string LegacyPatchValidationFileName = ".android_patch_validation.json";
    private const string LegacyPckPatchMarkerPrefix = ".android_pck_patch_v";
    private const string LegacyRedownloadMarkerFileName = "last_game_version_redownload.txt";
    private const string LegacyCacheCleanupMarkerFileName = "last_game_version_cache_cleanup.txt";

    internal static SelectedBranchRecoveryResult Execute(
        string dataDir,
        string branch,
        SelectedBranchRecoveryMode mode
    )
    {
        var context = new RecoveryContext(dataDir, branch, mode);
        return mode switch
        {
            SelectedBranchRecoveryMode.DerivedArtifacts => context.RemoveDerivedArtifacts(),
            SelectedBranchRecoveryMode.FullRedownload => context.RemoveForRedownload(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown recovery mode."),
        };
    }

    private sealed class RecoveryContext
    {
        private readonly string _dataDir;
        private readonly string _branch;
        private readonly SelectedBranchRecoveryMode _mode;
        private readonly List<string> _failures = new();
        private int _removed;

        internal RecoveryContext(
            string dataDir,
            string branch,
            SelectedBranchRecoveryMode mode
        )
        {
            if (string.IsNullOrWhiteSpace(dataDir))
                throw new ArgumentException("A launcher data directory is required.", nameof(dataDir));

            _dataDir = Path.GetFullPath(dataDir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            _branch = SteamGameBranch.StorageIdentity(branch);
            _mode = mode;
        }

        internal SelectedBranchRecoveryResult RemoveForRedownload()
        {
            using (var update = BranchInstallStateStore.Current.BeginOrResumeUpdate(
                _dataDir,
                _branch,
                "selected-branch-recovery",
                targetDepots: null
            ))
            {
                update.RequireUpdating();
                LauncherLaunchReadinessCache.Clear(
                    $"selected branch '{_branch}' entered full recovery"
                );

                RemoveSelectedInstallPayload();
                RemoveDerivedArtifactsCore();
                RemoveLegacyStateTemporaryFiles();

                // The durable updating record is deliberately the last owned
                // readiness artifact removed. Any earlier failure leaves the
                // selected branch durably non-launchable and retryable.
                if (_failures.Count == 0)
                    DeleteFile(BranchInstallStateStore.PathFor(_dataDir, _branch));
            }

            RemoveUnusedSlotScaffolding();
            return Finish();
        }

        internal SelectedBranchRecoveryResult RemoveDerivedArtifacts()
        {
            RequireUpdatingState();
            LauncherLaunchReadinessCache.Clear(
                $"selected branch '{_branch}' invalidated previous derived artifacts"
            );
            RemoveDerivedArtifactsCore();
            return Finish();
        }

        private void RequireUpdatingState()
        {
            var state = BranchInstallStateStore.Current.Read(_dataDir, _branch);
            if (state.Status != BranchInstallStatus.Updating)
            {
                throw new InvalidOperationException(
                    $"Derived-artifact invalidation for '{_branch}' requires the branch to be updating."
                );
            }
        }

        private void RemoveSelectedInstallPayload()
        {
            if (!string.Equals(_branch, SteamGameBranch.Public, StringComparison.Ordinal))
            {
                var slotDirectory = RequireOwnedPath(
                    SteamGameInstallPaths.VersionSlotDirectory(_dataDir, _branch)
                );
                if (!Directory.Exists(slotDirectory))
                    return;

                var stateName = SteamGameInstallPaths.InstallationStateFileName;
                var lockName = SteamGameInstallPaths.InstallationStateLockFileName;
                foreach (var path in Directory.EnumerateFileSystemEntries(
                    slotDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly
                ).ToArray())
                {
                    var name = Path.GetFileName(path);
                    if (string.Equals(name, stateName, StringComparison.Ordinal)
                        || string.Equals(name, lockName, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    DeletePath(path);
                }
                return;
            }

            DeleteDirectory(SteamGameInstallPaths.GameDirectory(_dataDir, _branch));
            DeleteDirectory(
                SteamGameInstallPaths.DownloadStateDirectoryPath(_dataDir, _branch)
            );
            DeleteFile(GameIdentityPckCache.PathFor(_dataDir, _branch));
            DeleteMatchingFiles(
                SteamGameInstallPaths.VersionSlotDirectory(_dataDir, _branch),
                GameIdentityPckCache.FileName + ".",
                ".tmp"
            );
        }

        private void RemoveDerivedArtifactsCore()
        {
            DeleteRuntimePackArtifacts();
            DeleteFile(
                Path.Combine(
                    SteamGameInstallPaths.GameDirectory(_dataDir, _branch),
                    LegacyPatchValidationFileName
                )
            );
            DeleteMatchingFiles(
                SteamGameInstallPaths.GameDirectory(_dataDir, _branch),
                LegacyPckPatchMarkerPrefix,
                string.Empty
            );

            DeleteJsonEvidenceWhenOwnedOrInvalid(
                LauncherRuntimeSlotEvidence.MarkerPath(_dataDir),
                "branch"
            );
            DeleteJsonEvidenceWhenOwnedOrInvalid(
                LauncherRuntimePatchValidationEvidence.MarkerPath(_dataDir),
                "selectedBranch"
            );
            DeleteTextEvidenceWhenOwnedOrInvalid(
                LauncherRuntimeCacheEvidence.MarkerPath(_dataDir),
                LauncherRuntimeCacheEvidence.ActiveBranchPrefix
            );

            DeleteTextEvidenceWhenOwnedOrInvalid(
                Path.Combine(_dataDir, LegacyRedownloadMarkerFileName),
                "Selected branch:"
            );
            DeleteTextEvidenceWhenOwnedOrInvalid(
                Path.Combine(_dataDir, LegacyCacheCleanupMarkerFileName),
                "Selected branch:"
            );
        }

        private void DeleteRuntimePackArtifacts()
        {
            var finalDirectory = RequireOwnedPath(
                GameRuntimeSlot.RuntimePackDirectoryPath(_dataDir, _branch)
            );
            var parent = Path.GetDirectoryName(finalDirectory);
            if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
                return;

            var finalName = Path.GetFileName(finalDirectory);
            foreach (var path in Directory.EnumerateFileSystemEntries(
                parent,
                finalName + "*",
                SearchOption.TopDirectoryOnly
            ).ToArray())
            {
                var name = Path.GetFileName(path);
                if (!IsOwnedRuntimePackArtifact(name, finalName))
                    continue;

                DeletePath(path);
            }
        }

        private static bool IsOwnedRuntimePackArtifact(string name, string finalName)
            => string.Equals(name, finalName, StringComparison.Ordinal)
                || name.StartsWith(finalName + ".staging.", StringComparison.Ordinal)
                || name.StartsWith(finalName + ".backup.", StringComparison.Ordinal)
                || name.StartsWith(finalName + ".rejected.", StringComparison.Ordinal)
                || string.Equals(
                    name,
                    finalName + ".promotion.lock",
                    StringComparison.Ordinal
                );

        private void RemoveLegacyStateTemporaryFiles()
        {
            var slotDirectory = SteamGameInstallPaths.VersionSlotDirectory(_dataDir, _branch);
            DeleteMatchingFiles(
                slotDirectory,
                SteamGameInstallPaths.InstallationStateFileName + ".",
                ".tmp"
            );
        }

        private void RemoveUnusedSlotScaffolding()
        {
            // The lock file is not readiness state and may already be opened by
            // a new updater after this operation releases its lock. Its removal
            // is opportunistic and never justifies touching new branch data.
            TryDeleteUnusedFile(
                SteamGameInstallPaths.InstallationStateLockPath(_dataDir, _branch)
            );

            if (string.Equals(_branch, SteamGameBranch.Public, StringComparison.Ordinal))
                return;

            var slot = RequireOwnedPath(
                SteamGameInstallPaths.VersionSlotDirectory(_dataDir, _branch)
            );
            try
            {
                if (Directory.Exists(slot)
                    && !Directory.EnumerateFileSystemEntries(slot).Any())
                {
                    Directory.Delete(slot, recursive: false);
                    _removed++;
                }
            }
            catch (IOException)
            {
                // Another updater may have populated the slot after the state
                // lock was released. Never recursively delete that new work.
            }
            catch (UnauthorizedAccessException)
            {
                // Empty scaffolding is harmless; owned payload removal already
                // determines recovery success.
            }
        }

        private void DeleteJsonEvidenceWhenOwnedOrInvalid(
            string path,
            string branchProperty
        )
        {
            DeleteEvidenceWhenOwnedOrInvalid(
                path,
                candidate => ReadJsonBranch(candidate, branchProperty)
            );
            DeleteEvidenceTemporaryFiles(
                path,
                candidate => ReadJsonBranch(candidate, branchProperty)
            );
        }

        private void DeleteTextEvidenceWhenOwnedOrInvalid(
            string path,
            string branchPrefix
        )
        {
            DeleteEvidenceWhenOwnedOrInvalid(
                path,
                candidate => ReadTextBranch(candidate, branchPrefix)
            );
        }

        private void DeleteEvidenceTemporaryFiles(
            string finalPath,
            Func<string, string> readOwner
        )
        {
            var parent = Path.GetDirectoryName(finalPath);
            if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
                return;

            var prefix = Path.GetFileName(finalPath) + ".";
            foreach (var candidate in Directory.EnumerateFiles(
                parent,
                prefix + "*.tmp",
                SearchOption.TopDirectoryOnly
            ).ToArray())
            {
                DeleteEvidenceWhenOwnedOrInvalid(candidate, readOwner);
            }
        }

        private void DeleteEvidenceWhenOwnedOrInvalid(
            string path,
            Func<string, string> readOwner
        )
        {
            if (!File.Exists(path))
                return;

            string owner = null;
            try
            {
                owner = readOwner(path);
            }
            catch
            {
                // An unreadable derived marker cannot safely authorize any
                // branch and is therefore invalid evidence, not foreign state.
            }

            if (owner == null
                || string.Equals(owner, _branch, StringComparison.Ordinal))
            {
                DeleteFile(path);
            }
        }

        private static string ReadJsonBranch(string path, string branchProperty)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(branchProperty, out var branch)
                || branch.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(branch.GetString()))
            {
                return null;
            }

            return NormalizeEvidenceBranch(branch.GetString());
        }

        private static string ReadTextBranch(string path, string branchPrefix)
        {
            foreach (var line in File.ReadLines(path))
            {
                if (!line.StartsWith(branchPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = line.Substring(branchPrefix.Length).Trim();
                return string.IsNullOrWhiteSpace(value)
                    ? null
                    : NormalizeEvidenceBranch(value);
            }
            return null;
        }

        private static string NormalizeEvidenceBranch(string branch)
        {
            try
            {
                return SteamGameBranch.StorageIdentity(branch);
            }
            catch
            {
                return null;
            }
        }

        private void DeleteMatchingFiles(
            string directory,
            string requiredPrefix,
            string requiredSuffix
        )
        {
            directory = RequireOwnedDirectoryOrDataRoot(directory);
            if (!Directory.Exists(directory))
                return;

            foreach (var path in Directory.EnumerateFiles(
                directory,
                requiredPrefix + "*" + requiredSuffix,
                SearchOption.TopDirectoryOnly
            ).ToArray())
            {
                var name = Path.GetFileName(path);
                if (name.StartsWith(requiredPrefix, StringComparison.Ordinal)
                    && name.EndsWith(requiredSuffix, StringComparison.Ordinal))
                {
                    DeleteFile(path);
                }
            }
        }

        private void DeletePath(string path)
        {
            path = RequireOwnedPath(path);
            if (Directory.Exists(path))
                DeleteDirectory(path);
            else
                DeleteFile(path);
        }

        private void DeleteDirectory(string path)
        {
            path = RequireOwnedPath(path);
            if (!Directory.Exists(path))
                return;

            try
            {
                Directory.Delete(path, recursive: true);
                _removed++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _failures.Add($"Could not remove directory '{path}': {ex.GetBaseException().Message}");
            }
        }

        private void DeleteFile(string path)
        {
            path = RequireOwnedPath(path);
            if (!File.Exists(path))
                return;

            try
            {
                File.Delete(path);
                _removed++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _failures.Add($"Could not remove file '{path}': {ex.GetBaseException().Message}");
            }
        }

        private void TryDeleteUnusedFile(string path)
        {
            try
            {
                path = RequireOwnedPath(path);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _removed++;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private string RequireOwnedDirectoryOrDataRoot(string path)
        {
            var target = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (PathEquals(target, _dataDir))
                return target;
            return RequireOwnedPath(target);
        }

        private string RequireOwnedPath(string path)
        {
            var target = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var rootPrefix = _dataDir + Path.DirectorySeparatorChar;
            if (PathEquals(target, _dataDir)
                || !target.StartsWith(rootPrefix, PathComparison()))
            {
                throw new IOException(
                    $"Refusing selected-branch recovery outside the launcher data directory: {target}."
                );
            }
            return target;
        }

        private static bool PathEquals(string left, string right)
            => string.Equals(left, right, PathComparison());

        private static StringComparison PathComparison()
            => OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        private SelectedBranchRecoveryResult Finish()
        {
            var result = new SelectedBranchRecoveryResult(
                _branch,
                _mode,
                _removed,
                _failures.ToArray()
            );
            var outcome = result.Succeeded ? "completed" : "failed";
            PatchHelper.Log(
                $"[Launcher] Selected-branch recovery {outcome}: branch='{_branch}' mode={_mode} removed={_removed} failures={_failures.Count}"
            );
            return result;
        }
    }
}
