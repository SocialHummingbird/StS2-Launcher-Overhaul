using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static readonly object LocalMirrorGate = new();

        internal readonly struct LocalMirrorRefreshResult
        {
            internal LocalMirrorRefreshResult(
                int discovered,
                int mirrored,
                int archived,
                int restored,
                int errors
            )
            {
                Discovered = discovered;
                Mirrored = mirrored;
                Archived = archived;
                Restored = restored;
                Errors = errors;
            }

            internal int Discovered { get; }
            internal int Mirrored { get; }
            internal int Archived { get; }
            internal int Restored { get; }
            internal int Errors { get; }

            public override string ToString()
                => $"discovered={Discovered}; mirrored={Mirrored}; archived={Archived}; restored={Restored}; errors={Errors}";
        }

        internal static LocalMirrorRefreshResult RefreshLocalMirror(
            ISaveStore local,
            bool restoreMissing
        )
        {
            lock (LocalMirrorGate)
                return RefreshLocalMirrorLocked(local, restoreMissing);
        }

        private static LocalMirrorRefreshResult RefreshLocalMirrorLocked(
            ISaveStore local,
            bool restoreMissing
        )
        {
            if (!_localBackupEnabled)
                return default;

            if (!AppPaths.HasStoragePermission())
            {
                PatchHelper.Log("[Cloud] Automatic local backup waiting for shared-storage permission");
                return default;
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
                ? RestoreMissingStableFiles(local, currentRoot, ref errors)
                : 0;
            var paths = SavePathDiscovery.Get(local);
            var discovered = 0;
            var mirrored = 0;
            var archived = 0;
            var generation = HistoryGenerationName();

            foreach (var path in paths.Take(LocalSaveBackupPlan.MaxFiles))
            {
                if (!LocalSaveBackupPlan.IsBackupEligible(path) || !local.FileExists(path))
                    continue;

                discovered++;
                try
                {
                    var content = local.ReadFile(path);
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
                        var previousContent = File.ReadAllText(mirrorPath);
                        if (string.Equals(previousContent, content, StringComparison.Ordinal))
                            continue;

                        if (TryArchiveMirrorFile(historyRoot, generation, path, mirrorPath))
                            archived++;
                        else
                            errors++;
                    }

                    WriteMirrorFile(mirrorPath, content);
                    mirrored++;
                }
                catch (Exception ex)
                {
                    errors++;
                    PatchHelper.Log($"[Cloud] Automatic local backup failed for {path}: {ex.Message}");
                }
            }

            PruneLocalMirrorHistory(historyRoot);
            var result = new LocalMirrorRefreshResult(
                discovered,
                mirrored,
                archived,
                restored,
                errors
            );
            PatchHelper.Log($"[Cloud] Automatic local backup refresh: {result}");
            return result;
        }

        internal static void MirrorLocalWrite(string path, byte[] content)
        {
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

                    WriteMirrorFile(mirrorPath, content);
                    PruneLocalMirrorHistory(historyRoot);
                    PatchHelper.Log($"[Cloud] Mirrored local save write: {path} ({content.Length} bytes)");
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
            ref int errors
        )
        {
            if (!Directory.Exists(currentRoot))
                return 0;

            var restored = 0;
            foreach (var mirrorPath in EnumerateMirrorFiles(currentRoot))
            {
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

                    var content = File.ReadAllText(mirrorPath);
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
                catch (Exception ex)
                {
                    errors++;
                    PatchHelper.Log($"[Cloud] Automatic local backup restore failed for {relativePath}: {ex.Message}");
                }
            }

            return restored;
        }

        private static IEnumerable<string> EnumerateMirrorFiles(string currentRoot)
        {
            try
            {
                return Directory
                    .EnumerateFiles(currentRoot, "*", SearchOption.AllDirectories)
                    .Where(path => !path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path)
                    .Take(LocalSaveBackupPlan.MaxFiles)
                    .ToArray();
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

        private static void WriteMirrorFile(string mirrorPath, string content)
        {
            var parent = Path.GetDirectoryName(mirrorPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            var tempPath = mirrorPath + ".tmp";
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, mirrorPath, overwrite: true);
        }

        private static void WriteMirrorFile(string mirrorPath, byte[] content)
        {
            var parent = Path.GetDirectoryName(mirrorPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            var tempPath = mirrorPath + ".tmp";
            File.WriteAllBytes(tempPath, content);
            File.Move(tempPath, mirrorPath, overwrite: true);
        }

        private static string HistoryGenerationName()
            => DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmssfffffff'Z'");

        private static void PruneLocalMirrorHistory(string historyRoot)
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
                    Directory.Delete(oldGeneration, recursive: true);
                }
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Cloud] Automatic local backup history prune failed: {ex.Message}");
            }
        }
    }
}
