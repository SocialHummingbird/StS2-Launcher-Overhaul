using System.Collections.Generic;
using SteamKit2;
using System.Linq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    // Combines manifest changes with on-disk verification so interrupted writes
    // and corrupt files are repaired even without a new manifest.
    private IReadOnlyList<DepotManifest.FileData> BuildDepotDownloads(DepotManifest? oldManifest, DepotManifest newManifest, bool isUpdate)
    {
        var downloads = GetFilesNeedingDownload(oldManifest, newManifest, isUpdate);
        downloads = DeduplicateDownloads(downloads);
        ValidateDownloadFileSizes(downloads);
        return downloads;
    }

    private List<DepotManifest.FileData> DeduplicateDownloads(List<DepotManifest.FileData> filesToDownload)
    {
        if (filesToDownload.Count <= 1)
            return filesToDownload;
        var deduped = BuildManifestFileMap(filesToDownload);
        if (deduped.Count == filesToDownload.Count)
            return filesToDownload;
        Log($"Deduplicated duplicate download queue entries: {filesToDownload.Count - deduped.Count}");
        return deduped.Values.ToList();
    }

    private static List<string> GetFilesToDelete(DepotManifest? oldManifest, DepotManifest newManifest)
    {
        if (oldManifest == null)
            return new List<string>();
        var newFiles = new HashSet<string>(NormalizedManifestFileNames(newManifest.Files), StringComparer.Ordinal);
        return NormalizedManifestFileNames(oldManifest.Files).Where(f => !newFiles.Contains(f)).ToList();
    }

    private enum ManifestFileDownloadState
    {
        Skip,
        NeedsDownload,
        ExistingVerified,
        CorruptNeedsDownload,
    }

    private ManifestFileDownloadState GetManifestFileDownloadState(DepotManifest.FileData file, Dictionary<string, DepotManifest.FileData> oldFiles, bool isUpdate)
    {
        var fileName = GetDownloadFileName(file);
        if (fileName == null)
            return ManifestFileDownloadState.Skip;
        if (ManifestEntryChanged(file, oldFiles, fileName, isUpdate))
            return ManifestFileDownloadState.NeedsDownload;
        var filePath = ResolveGamePath(fileName);
        if (VerifyFileHash(filePath, file))
            return ManifestFileDownloadState.ExistingVerified;
        if (File.Exists(filePath))
        {
            Log($"File needs re-download (hash mismatch): {file.FileName}");
            return ManifestFileDownloadState.CorruptNeedsDownload;
        }

        return ManifestFileDownloadState.NeedsDownload;
    }

    private string? GetDownloadFileName(DepotManifest.FileData file)
    {
        var fileName = GetManifestFileName(file);
        if (fileName == null)
            return null;
        return file.Flags.HasFlag(EDepotFileFlag.Directory) ? null : fileName;
    }

    private static bool ManifestEntryChanged(DepotManifest.FileData file, Dictionary<string, DepotManifest.FileData> oldFiles, string fileName, bool isUpdate) => isUpdate && (!oldFiles.TryGetValue(fileName, out var oldFile) || !HashesEqual(file.FileHash, oldFile.FileHash));
    private sealed class ManifestDownloadSelection
    {
        private readonly List<DepotManifest.FileData> _downloads = new();
        private int _verified;
        private int _corrupt;
        private ManifestDownloadSelection()
        {
        }

        private static ManifestDownloadSelection Empty() => new();
        private void Include(DepotManifest.FileData file, ManifestFileDownloadState decision)
        {
            switch (decision)
            {
                case ManifestFileDownloadState.NeedsDownload:
                    _downloads.Add(file);
                    break;
                case ManifestFileDownloadState.ExistingVerified:
                    _verified++;
                    break;
                case ManifestFileDownloadState.CorruptNeedsDownload:
                    _downloads.Add(file);
                    _corrupt++;
                    break;
            }
        }

        private List<DepotManifest.FileData> Finish(DepotDownloader owner)
        {
            if (_verified > 0)
                owner.Log($"Verified {_verified} existing files");
            if (_corrupt > 0)
                owner.Log($"Found {_corrupt} corrupt files requiring re-download");
            return _downloads;
        }

        internal static List<DepotManifest.FileData> Build(DepotDownloader owner, DepotManifest newManifest, Dictionary<string, DepotManifest.FileData> oldFiles, bool isUpdate)
        {
            var selection = Empty();
            foreach (var file in newManifest.Files)
            {
                selection.Include(file, owner.GetManifestFileDownloadState(file, oldFiles, isUpdate));
            }

            return selection.Finish(owner);
        }
    }

    // Builds the list of files that need downloading. For manifest changes, uses
    // the hash diff. For all files in the target manifest, verifies the on-disk
    // copy against the expected SHA-1, catching corruption from interrupted
    // writes, disk errors, or missing files.
    private List<DepotManifest.FileData> GetFilesNeedingDownload(DepotManifest? oldManifest, DepotManifest newManifest, bool isUpdate)
    {
        var oldFiles = BuildManifestFileMap(oldManifest);
        return ManifestDownloadSelection.Build(this, newManifest, oldFiles, isUpdate);
    }

    private static Dictionary<string, DepotManifest.FileData> BuildManifestFileMap(DepotManifest? manifest) => manifest == null ? new Dictionary<string, DepotManifest.FileData>(StringComparer.Ordinal) : BuildManifestFileMap(manifest.Files);
    private static Dictionary<string, DepotManifest.FileData> BuildManifestFileMap(IEnumerable<DepotManifest.FileData> files)
    {
        var map = new Dictionary<string, DepotManifest.FileData>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (!TryGetNormalizedManifestFileName(file, out var fileName))
                continue;
            map[fileName] = file;
        }

        return map;
    }

    private static IEnumerable<string> NormalizedManifestFileNames(IEnumerable<DepotManifest.FileData> files)
    {
        foreach (var file in files)
        {
            if (TryGetNormalizedManifestFileName(file, out var fileName))
                yield return fileName;
        }
    }

    private static bool TryGetNormalizedManifestFileName(DepotManifest.FileData file, out string fileName)
    {
        fileName = NormalizeManifestPath(file.FileName);
        return !string.IsNullOrEmpty(fileName);
    }

    private void CleanupStaleDownloadTemps()
    {
        RequireUpdatingBeforeInstalledMutation();
        foreach (var temp in EnumerateDownloadingTempFiles())
            TryDeleteFileIfExists(temp, ex => $"Could not delete stale temp file {temp}: {ex.Message}");
    }

    private sealed class DepotFileDownloadQueue
    {
        private readonly IReadOnlyList<DepotManifest.FileData> _files;
        private int _nextFileIndex = -1;
        internal DepotFileDownloadQueue(IReadOnlyList<DepotManifest.FileData> files)
        {
            _files = files;
        }

        internal int WorkerCount => Math.Min(MaxConcurrentDownloads, _files.Count);

        internal DepotManifest.FileData? TakeNext()
        {
            var index = Interlocked.Increment(ref _nextFileIndex);
            return index < _files.Count ? _files[index] : null;
        }
    }

    private readonly struct DepotFileDownloadContext
    {
        private DepotFileDownloadContext(DepotDownloader owner, uint depotId, byte[] depotKey, CancellationToken ct)
        {
            Owner = owner;
            DepotId = depotId;
            DepotKey = depotKey;
            Cancellation = ct;
        }

        internal CancellationToken Cancellation { get; }
        private DepotDownloader Owner { get; }
        private uint DepotId { get; }
        private byte[] DepotKey { get; }

        internal async Task DownloadAsync(DepotManifest.FileData file)
        {
            await Owner.DownloadFileAsync(file, DepotId, DepotKey, Cancellation);
            Owner.ForceReportProgress();
        }

        internal static DepotFileDownloadContext Create(DepotDownloader owner, uint depotId, byte[] depotKey, CancellationToken ct) => new(owner, depotId, depotKey, ct);
    }

    private sealed class DepotFileDownloadWorkers
    {
        private readonly DepotFileDownloadQueue _queue;
        private readonly DepotFileDownloadContext _context;
        private DepotFileDownloadWorkers(DepotFileDownloadQueue queue, DepotFileDownloadContext context)
        {
            _queue = queue;
            _context = context;
        }

        private Task RunAsync()
        {
            var workers = Enumerable.Range(0, _queue.WorkerCount).Select(_ => Task.Run(RunWorkerAsync, _context.Cancellation)).ToArray();
            return Task.WhenAll(workers);
        }

        private async Task RunWorkerAsync()
        {
            while (true)
            {
                _context.Cancellation.ThrowIfCancellationRequested();
                var file = _queue.TakeNext();
                if (file == null)
                    return;
                await _context.DownloadAsync(file);
            }
        }

        internal static Task RunAsync(IReadOnlyList<DepotManifest.FileData> files, DepotFileDownloadContext context) => new DepotFileDownloadWorkers(new DepotFileDownloadQueue(files), context).RunAsync();
    }

    private async Task DownloadDepotFilesAsync(IReadOnlyList<DepotManifest.FileData> filesToDownload, uint depotId, byte[] depotKey, CancellationToken ct)
    {
        var context = DepotFileDownloadContext.Create(this, depotId, depotKey, ct);
        await DepotFileDownloadWorkers.RunAsync(filesToDownload, context);
    }

    private void DeleteObsoleteFiles(IEnumerable<string> fileNames)
    {
        RequireUpdatingBeforeInstalledMutation();
        foreach (var fileName in fileNames)
        {
            string path;
            try
            {
                path = ResolveGamePath(fileName);
            }
            catch (Exception ex)
            {
                Log($"Skipping obsolete file with invalid cached path {fileName}: {ex.Message}");
                continue;
            }

            TryDeleteFileIfExists(path, $"Deleted: {fileName}", ex => $"Could not delete obsolete file {fileName}: {ex.Message}");
        }
    }

    private void ResetDepotProgress(IReadOnlyCollection<DepotManifest.FileData> filesToDownload)
    {
        _totalDownloadBytes = ComputeTotalDownloadBytes(filesToDownload);
        _downloadedBytes = 0;
        ForceReportProgress();
    }

    private static void ValidateDownloadFileSizes(IEnumerable<DepotManifest.FileData> files)
    {
        foreach (var file in files)
        {
            if (file.TotalSize > (ulong)MaxDepotFileBytes)
            {
                throw new IOException($"Depot file is unexpectedly large for {file.FileName}: " + $"{file.TotalSize} bytes");
            }
        }
    }

    private static long ComputeTotalDownloadBytes(IEnumerable<DepotManifest.FileData> files)
    {
        long total = 0;
        foreach (var file in files)
        {
            try
            {
                total = checked(total + (long)file.TotalSize);
            }
            catch (OverflowException ex)
            {
                throw new IOException($"Depot download size is too large while adding {file.FileName}: " + $"{file.TotalSize} bytes", ex);
            }
        }

        return total;
    }
}
