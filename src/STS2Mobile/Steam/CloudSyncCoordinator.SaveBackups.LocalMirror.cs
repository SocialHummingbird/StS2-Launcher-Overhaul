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
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_localBackupEnabled)
                return LocalBackupRefreshResult.Skipped(
                    AppPaths.HasStoragePermission()
                );

            if (!AppPaths.HasStoragePermission())
            {
                PatchHelper.Log(
                    "[Cloud] Automatic local backup waiting for shared-storage permission"
                );
                return LocalBackupRefreshResult.StorageAccessMissing();
            }

            return RefreshLocalMirrorAtRoot(
                local,
                AppPaths.ExternalSaveBackupsDir,
                candidatePaths: null,
                cancellationToken
            );
        }

        internal static LocalBackupRefreshResult RefreshLocalMirrorAtRoot(
            ISaveStore local,
            string backupsRoot,
            IReadOnlyCollection<string> candidatePaths = null,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(local);
            ArgumentException.ThrowIfNullOrWhiteSpace(backupsRoot);
            cancellationToken.ThrowIfCancellationRequested();
            lock (LocalMirrorGate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return RefreshLocalMirrorLocked(
                    local,
                    backupsRoot,
                    candidatePaths,
                    cancellationToken
                );
            }
        }

        private static LocalBackupRefreshResult RefreshLocalMirrorLocked(
            ISaveStore local,
            string backupsRoot,
            IReadOnlyCollection<string> candidatePaths,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentRoot = Path.Combine(
                backupsRoot,
                LocalSaveBackupPlan.CurrentDirectoryName
            );
            var historyRoot = Path.Combine(
                backupsRoot,
                LocalSaveBackupPlan.HistoryDirectoryName
            );
            Directory.CreateDirectory(currentRoot);
            Directory.CreateDirectory(historyRoot);

            var errors = 0;
            var paths = candidatePaths
                ?? SavePathDiscovery.Get(
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
                    var content = CancellableSaveStore.ReadBytesAsync(
                        local,
                        path,
                        cancellationToken
                    ).GetAwaiter().GetResult();
                    if (content.Length == 0)
                        continue;

                    if (!LocalSaveBackupPlan.TryResolveUnderRoot(currentRoot, path, out var mirrorPath))
                    {
                        errors++;
                        PatchHelper.Log($"[Cloud] Automatic local backup rejected unsafe path: {path}");
                        continue;
                    }

                    if (File.Exists(mirrorPath))
                    {
                        var previousContent = File.ReadAllBytesAsync(
                            mirrorPath,
                            cancellationToken
                        ).GetAwaiter().GetResult();
                        if (previousContent.AsSpan().SequenceEqual(content))
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
