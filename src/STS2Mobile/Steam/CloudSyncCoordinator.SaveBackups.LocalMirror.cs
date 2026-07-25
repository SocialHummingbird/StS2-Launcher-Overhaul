using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static readonly object LocalMirrorGate = new();

        internal static LocalBackupRefreshResult RefreshLocalMirror(
            ISaveStore local,
            bool restoreMissing,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (LocalMirrorGate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return RefreshLocalMirrorLocked(
                    local,
                    restoreMissing,
                    cancellationToken
                );
            }
        }

        private static LocalBackupRefreshResult RefreshLocalMirrorLocked(
            ISaveStore local,
            bool restoreMissing,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_localBackupEnabled)
                return LocalBackupRefreshResult.Skipped(
                    AppPaths.HasStoragePermission()
                );

            if (!AppPaths.HasStoragePermission())
            {
                PatchHelper.Log("[Cloud] Automatic local backup waiting for shared-storage permission");
                return LocalBackupRefreshResult.StorageAccessMissing();
            }

            var currentRoot = Path.Combine(
                AppPaths.ExternalSaveBackupsDir,
                LocalSaveBackupPlan.CurrentDirectoryName
            );
            var historyRoot = Path.Combine(
                AppPaths.ExternalSaveBackupsDir,
                LocalSaveBackupPlan.HistoryDirectoryName
            );
            Directory.CreateDirectory(currentRoot);
            Directory.CreateDirectory(historyRoot);

            var errors = 0;
            var restored = restoreMissing
                ? RestoreMissingStableFiles(
                    local,
                    currentRoot,
                    ref errors,
                    cancellationToken
                )
                : 0;
            var paths = SavePathDiscovery.Get(
                local,
                cancellationToken
            );
            var discovered = 0;
            var mirrored = 0;
            var archived = 0;
            var generation = HistoryGenerationName();

            foreach (var path in paths.Take(LocalSaveBackupPlan.MaxFiles))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!LocalSaveBackupPlan.IsBackupEligible(path) || !local.FileExists(path))
                    continue;

                discovered++;
                try
                {
                    var content = CancellableSaveStore.ReadFileAsync(
                        local,
                        path,
                        cancellationToken
                    ).GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(content))
                        continue;

                    if (!LocalSaveBackupPlan.TryResolveUnderRoot(currentRoot, path, out var mirrorPath))
                    {
                        errors++;
                        PatchHelper.Log($"[Cloud] Automatic local backup rejected unsafe path: {path}");
                        continue;
                    }

                    if (File.Exists(mirrorPath))
                    {
                        var previousContent = File.ReadAllTextAsync(
                            mirrorPath,
                            cancellationToken
                        ).GetAwaiter().GetResult();
                        if (string.Equals(previousContent, content, StringComparison.Ordinal))
                            continue;

                        if (TryArchiveMirrorFile(historyRoot, generation, path, mirrorPath))
                            archived++;
                        else
                            errors++;
                    }

                    WriteMirrorFile(
                        mirrorPath,
                        content,
                        cancellationToken
                    );
                    cancellationToken.ThrowIfCancellationRequested();
                    mirrored++;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors++;
                    PatchHelper.Log($"[Cloud] Automatic local backup failed for {path}: {ex.Message}");
                }
            }

            PruneLocalMirrorHistory(
                historyRoot,
                cancellationToken
            );
            var result = new LocalBackupRefreshResult(
                Attempted: true,
                StorageAccessAvailable: true,
                Discovered: discovered,
                Mirrored: mirrored,
                Archived: archived,
                Restored: restored,
                Errors: errors,
                FailureMessage: ""
            );
            PatchHelper.Log($"[Cloud] Automatic local backup refresh: {result}");
            return result;
        }

        internal static void MirrorLocalWrite(
            string path,
            byte[] content,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (
                !_localBackupEnabled
                || content == null
                || content.Length == 0
                || !LocalSaveBackupPlan.IsBackupEligible(path)
                || !AppPaths.HasStoragePermission()
            )
                return;

            lock (LocalMirrorGate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var currentRoot = Path.Combine(
                        AppPaths.ExternalSaveBackupsDir,
                        LocalSaveBackupPlan.CurrentDirectoryName
                    );
                    var historyRoot = Path.Combine(
                        AppPaths.ExternalSaveBackupsDir,
                        LocalSaveBackupPlan.HistoryDirectoryName
                    );
                    if (!LocalSaveBackupPlan.TryResolveUnderRoot(currentRoot, path, out var mirrorPath))
                    {
                        PatchHelper.Log($"[Cloud] Automatic local backup rejected unsafe write path: {path}");
                        return;
                    }

                    Directory.CreateDirectory(currentRoot);
                    Directory.CreateDirectory(historyRoot);
                    if (File.Exists(mirrorPath))
                    {
                        var previous = File.ReadAllBytes(mirrorPath);
                        if (previous.AsSpan().SequenceEqual(content))
                            return;

                        if (!TryArchiveMirrorFile(
                            historyRoot,
                            HistoryGenerationName(),
                            path,
                            mirrorPath
                        ))
                        {
                            PatchHelper.Log($"[Cloud] Automatic local backup could not archive previous {path}");
                        }
                    }

                    WriteMirrorFile(
                        mirrorPath,
                        content,
                        cancellationToken
                    );
                    cancellationToken.ThrowIfCancellationRequested();
                    PruneLocalMirrorHistory(
                        historyRoot,
                        cancellationToken
                    );
                    PatchHelper.Log($"[Cloud] Mirrored local save write: {path} ({content.Length} bytes)");
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"[Cloud] Automatic local save-write backup failed for {path}: {ex.Message}");
                }
            }
        }

        private static int RestoreMissingStableFiles(
            ISaveStore local,
            string currentRoot,
            ref int errors,
            CancellationToken cancellationToken
        )
        {
            if (!Directory.Exists(currentRoot))
                return 0;

            var restored = 0;
            foreach (
                var mirrorPath in EnumerateMirrorFiles(
                    currentRoot,
                    cancellationToken
                )
            )
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = LocalSaveBackupPlan.NormalizeRelativePath(
                    Path.GetRelativePath(currentRoot, mirrorPath)
                );
                if (
                    !LocalSaveBackupPlan.IsBackupEligible(relativePath)
                    || !LocalSaveBackupPlan.ShouldRestoreMissing(relativePath)
                )
                    continue;

                try
                {
                    if (local.FileExists(relativePath) && local.GetFileSize(relativePath) > 0)
                        continue;

                    var content = File.ReadAllTextAsync(
                        mirrorPath,
                        cancellationToken
                    ).GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(content))
                        continue;

                    local.WriteFile(relativePath, content);
                    local.SetLastModifiedTime(
                        relativePath,
                        new DateTimeOffset(File.GetLastWriteTimeUtc(mirrorPath), TimeSpan.Zero)
                    );
                    restored++;
                    PatchHelper.Log($"[Cloud] Restored missing local save from automatic backup: {relativePath}");
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors++;
                    PatchHelper.Log($"[Cloud] Automatic local backup restore failed for {relativePath}: {ex.Message}");
                }
            }

            return restored;
        }

        private static IEnumerable<string> EnumerateMirrorFiles(
            string currentRoot,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var files = new List<string>();
                foreach (
                    var path in Directory.EnumerateFiles(
                        currentRoot,
                        "*",
                        SearchOption.AllDirectories
                    )
                )
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (
                        !path.EndsWith(
                            ".tmp",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        files.Add(path);
                    }

                    if (files.Count >= LocalSaveBackupPlan.MaxFiles)
                        break;
                }

                return files.OrderBy(path => path).ToArray();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Cloud] Automatic local backup enumeration failed: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static bool TryArchiveMirrorFile(
            string historyRoot,
            string generation,
            string relativePath,
            string mirrorPath
        )
        {
            var generationRoot = Path.Combine(historyRoot, generation);
            if (!LocalSaveBackupPlan.TryResolveUnderRoot(generationRoot, relativePath, out var archivePath))
                return false;

            var parent = Path.GetDirectoryName(archivePath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);
            File.Copy(mirrorPath, archivePath, overwrite: true);
            return true;
        }

        private static void WriteMirrorFile(
            string mirrorPath,
            string content,
            CancellationToken cancellationToken
        )
        {
            CancellableAtomicFile.WriteAllTextAsync(
                mirrorPath,
                content,
                overwrite: true,
                cancellationToken
            ).GetAwaiter().GetResult();
        }

        private static void WriteMirrorFile(
            string mirrorPath,
            byte[] content,
            CancellationToken cancellationToken
        )
        {
            CancellableAtomicFile.WriteAllBytesAsync(
                mirrorPath,
                content,
                overwrite: true,
                cancellationToken
            ).GetAwaiter().GetResult();
        }

        private static string HistoryGenerationName()
            => DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmssfffffff'Z'");

        private static void PruneLocalMirrorHistory(
            string historyRoot,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                foreach (
                    var oldGeneration in Directory
                        .GetDirectories(historyRoot)
                        .OrderByDescending(Path.GetFileName)
                        .Skip(LocalSaveBackupPlan.MaxHistoryGenerations)
                )
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Directory.Delete(oldGeneration, recursive: true);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Cloud] Automatic local backup history prune failed: {ex.Message}");
            }
        }
    }
}
