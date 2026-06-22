using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam.Workshop;

internal sealed class SteamWorkshopDownloader : IDisposable
{
    private const long MaxWorkshopDownloadBytes = 2L * 1024L * 1024L * 1024L;
    private static readonly Regex StaleDownloadFileNamePattern = new(
        @"^\.\d+\.[0-9a-fA-F]{32}\.download$",
        RegexOptions.CultureInvariant
    );
    private static readonly Regex StaleDownloadDirectoryNamePattern = new(
        @"^(?<id>\d+)\.(?<kind>tmp|old)-[0-9a-fA-F]{32}$",
        RegexOptions.CultureInvariant
    );

    private readonly SteamConnection _connection;
    private readonly string _downloadsDirectory;
    private readonly HttpClient _http;

    internal SteamWorkshopDownloader(SteamConnection connection, string downloadsDirectory)
    {
        _connection = connection;
        _downloadsDirectory = Path.GetFullPath(downloadsDirectory);
        _http = AndroidJavaHttpMessageHandler.CreateCdnClient();
    }

    internal event Action<string> LogMessage;

    internal async Task<IReadOnlyList<SteamWorkshopItem>> DownloadAsync(
        IReadOnlyList<PublishedFileDetails> details,
        IReadOnlyDictionary<ulong, CPublishedFile_GetItemInfo_Response.WorkshopItemInfo> itemInfo,
        IReadOnlyCollection<ulong> subscribedItemIds,
        IReadOnlyDictionary<ulong, IReadOnlyCollection<ulong>> dependencyParents,
        IReadOnlyDictionary<ulong, SteamWorkshopSyncManifestItem> previousItems = null,
        CancellationToken ct = default
    )
    {
        Directory.CreateDirectory(_downloadsDirectory);
        CleanupStaleDownloadArtifacts();

        var items = new List<SteamWorkshopItem>();
        foreach (var detail in details ?? Array.Empty<PublishedFileDetails>())
        {
            ct.ThrowIfCancellationRequested();
            var item = await DownloadItemAsync(
                detail,
                itemInfo,
                subscribedItemIds,
                dependencyParents,
                previousItems,
                ct
            ).ConfigureAwait(false);
            items.Add(item);
        }

        return items;
    }

    private async Task<SteamWorkshopItem> DownloadItemAsync(
        PublishedFileDetails detail,
        IReadOnlyDictionary<ulong, CPublishedFile_GetItemInfo_Response.WorkshopItemInfo> itemInfo,
        IReadOnlyCollection<ulong> subscribedItemIds,
        IReadOnlyDictionary<ulong, IReadOnlyCollection<ulong>> dependencyParents,
        IReadOnlyDictionary<ulong, SteamWorkshopSyncManifestItem> previousItems,
        CancellationToken ct
    )
    {
        var id = detail.publishedfileid;
        var title = DetailTitle(detail);
        var manifestId = itemInfo != null && itemInfo.TryGetValue(id, out var info)
            ? info.manifest_id
            : 0;
        var itemDirectory = Path.Combine(_downloadsDirectory, id.ToString());
        var isDependency = subscribedItemIds == null || !subscribedItemIds.Contains(id);
        var requiredByPublishedFileIds = dependencyParents != null && dependencyParents.TryGetValue(id, out var parents)
            ? parents
            : Array.Empty<ulong>();

        var source = DownloadSource.Unknown;
        try
        {
            source = await ResolveDownloadSourceAsync(detail, manifestId).ConfigureAwait(false);
            if (source.UnsupportedReason != null)
                return Skipped(
                    detail,
                    manifestId,
                    source.UnsupportedReason,
                    isDependency,
                    requiredByPublishedFileIds,
                    source
                );

            if (CanReuseCachedDownload(
                    itemDirectory,
                    detail,
                    manifestId,
                    source,
                    previousItems
                ))
            {
                Log($"Using cached Workshop download for {title} ({id})");
                return DownloadedItem(
                    detail,
                    title,
                    itemDirectory,
                    manifestId,
                    source,
                    isDependency,
                    requiredByPublishedFileIds,
                    reusedCachedDownload: true
                );
            }

            if (string.Equals(source.Kind, "depot-manifest", StringComparison.OrdinalIgnoreCase))
            {
                await DownloadDepotManifestAsync(
                    detail,
                    title,
                    manifestId,
                    itemDirectory,
                    ct
                ).ConfigureAwait(false);
                return DownloadedItem(
                    detail,
                    title,
                    itemDirectory,
                    manifestId,
                    source,
                    isDependency,
                    requiredByPublishedFileIds,
                    reusedCachedDownload: false
                );
            }

            Log($"Downloading {title} ({id})");
            var tempFile = await DownloadToTempFileAsync(
                id,
                source.Url,
                source.ExpectedSize,
                ct
            ).ConfigureAwait(false);

            ReplaceDownloadedContent(tempFile, source.FileName, itemDirectory);
            Log($"Downloaded {title} ({id}) to {itemDirectory}");

            return DownloadedItem(
                detail,
                title,
                itemDirectory,
                manifestId,
                source,
                isDependency,
                requiredByPublishedFileIds,
                reusedCachedDownload: false
            );
        }
        catch (Exception ex)
        {
            Log($"Failed to download {title} ({id}): {ex.Message}");
            return new SteamWorkshopItem(
                id,
                title,
                itemDirectory,
                detail.time_updated,
                manifestId,
                "download-failed",
                ex.Message,
                "",
                source.Kind,
                source.ExpectedSize,
                source.HContentFile,
                isDependency,
                requiredByPublishedFileIds
            );
        }
    }

    private async Task<DownloadSource> ResolveDownloadSourceAsync(
        PublishedFileDetails detail,
        ulong manifestId
    )
    {
        if (!string.IsNullOrWhiteSpace(detail.file_url))
        {
            return new DownloadSource(
                detail.file_url,
                FileNameFromDetail(detail),
                DeclaredDownloadSize(detail.file_size),
                "direct-url",
                detail.hcontent_file,
                null
            );
        }

        if (detail.hcontent_file != 0 && manifestId != 0)
        {
            try
            {
                var ugc = await _connection.GetWorkshopUgcDetailsAsync(detail.hcontent_file)
                    .ConfigureAwait(false);
                return new DownloadSource(
                    ugc.URL,
                    FileNameFromUgcOrDetail(ugc.FileName, detail),
                    ugc.FileSize,
                    "ugc-hcontent",
                    detail.hcontent_file,
                    null
                );
            }
            catch (Exception ex) when (manifestId != 0)
            {
                PatchHelper.Log(
                    $"[Workshop] UGC details unavailable for {detail.publishedfileid}; using depot-manifest classification: {ex.Message}"
                );
                return new DownloadSource(
                    "",
                    "",
                    0,
                    "depot-manifest",
                    detail.hcontent_file,
                    null
                );
            }
        }

        if (manifestId != 0)
        {
            return new DownloadSource(
                "",
                "",
                0,
                "depot-manifest",
                detail.hcontent_file,
                null
            );
        }

        if (detail.hcontent_file != 0)
        {
            return new DownloadSource(
                "",
                "",
                0,
                "no-download-source",
                detail.hcontent_file,
                "Steam exposed a legacy Workshop UGC handle but no direct URL or depot manifest"
            );
        }

        return new DownloadSource(
            "",
            "",
            0,
            "no-download-source",
            detail.hcontent_file,
            "Steam did not expose a Workshop download URL or UGC handle"
        );
    }

    private async Task DownloadDepotManifestAsync(
        PublishedFileDetails detail,
        string title,
        ulong manifestId,
        string itemDirectory,
        CancellationToken ct
    )
    {
        Log($"Downloading depot-manifest Workshop content for {title} ({detail.publishedfileid}) manifest={manifestId}");
        using var depotDownloader = new DepotDownloader(
            _connection,
            _downloadsDirectory,
            SteamGameBranch.Public
        );
        depotDownloader.LogMessage += Log;
        await depotDownloader.DownloadWorkshopDepotManifestAsync(
            manifestId,
            itemDirectory,
            title,
            ct
        ).ConfigureAwait(false);
    }

    private static SteamWorkshopItem DownloadedItem(
        PublishedFileDetails detail,
        string title,
        string itemDirectory,
        ulong manifestId,
        DownloadSource source,
        bool isDependency,
        IReadOnlyCollection<ulong> requiredByPublishedFileIds,
        bool reusedCachedDownload
    )
        => new(
            detail.publishedfileid,
            title,
            itemDirectory,
            detail.time_updated,
            manifestId,
            "downloaded",
            "",
            source.Url,
            source.Kind,
            source.ExpectedSize,
            source.HContentFile,
            isDependency,
            requiredByPublishedFileIds,
            reusedCachedDownload
        );

    private static bool CanReuseCachedDownload(
        string itemDirectory,
        PublishedFileDetails detail,
        ulong manifestId,
        DownloadSource source,
        IReadOnlyDictionary<ulong, SteamWorkshopSyncManifestItem> previousItems
    )
    {
        if (previousItems == null
            || detail.publishedfileid == 0
            || !previousItems.TryGetValue(detail.publishedfileid, out var previousItem))
        {
            return false;
        }

        return IsReusablePreviousStatus(previousItem.Status)
            && previousItem.TimeUpdated == detail.time_updated
            && previousItem.ManifestId == manifestId
            && string.Equals(previousItem.DownloadSourceKind, source.Kind, StringComparison.OrdinalIgnoreCase)
            && previousItem.HContentFile == source.HContentFile
            && previousItem.ExpectedDownloadBytes == source.ExpectedSize
            && CachedDownloadStillPresent(itemDirectory, previousItem.Status);
    }

    private static bool IsReusablePreviousStatus(string status)
        => string.Equals(status, "staged", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "staged-no-pck", StringComparison.OrdinalIgnoreCase);

    private static bool CachedDownloadStillPresent(string itemDirectory, string previousStatus)
        => string.Equals(previousStatus, "staged-no-pck", StringComparison.OrdinalIgnoreCase)
            ? CachedDownloadHasEntries(itemDirectory)
            : CachedDownloadHasFiles(itemDirectory);

    private static bool CachedDownloadHasEntries(string itemDirectory)
    {
        try
        {
            return Directory.EnumerateFileSystemEntries(itemDirectory, "*", SearchOption.AllDirectories).Any();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to inspect cached Workshop download {itemDirectory}: {ex.Message}");
            return false;
        }
    }

    private static bool CachedDownloadHasFiles(string itemDirectory)
    {
        try
        {
            return Directory.EnumerateFiles(itemDirectory, "*", SearchOption.AllDirectories).Any();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to inspect cached Workshop download {itemDirectory}: {ex.Message}");
            return false;
        }
    }

    private void CleanupStaleDownloadArtifacts()
    {
        foreach (var file in Directory.EnumerateFiles(_downloadsDirectory).ToArray())
        {
            var name = Path.GetFileName(file);
            if (!StaleDownloadFileNamePattern.IsMatch(name))
                continue;

            DeleteDownloadFile(file);
        }

        foreach (var directory in Directory.EnumerateDirectories(_downloadsDirectory).ToArray())
        {
            var name = Path.GetFileName(directory);
            var match = StaleDownloadDirectoryNamePattern.Match(name);
            if (!match.Success)
                continue;

            if (string.Equals(match.Groups["kind"].Value, "old", StringComparison.OrdinalIgnoreCase))
                RestoreOrDeleteDownloadBackup(directory, match.Groups["id"].Value);
            else
                DeleteDownloadDirectory(directory);
        }
    }

    private void DeleteDownloadFile(string file)
    {
        var fullFile = Path.GetFullPath(file);
        if (!IsPathInsideDirectory(fullFile, _downloadsDirectory))
            throw new InvalidOperationException(
                $"Workshop download temp file escapes downloads root: {fullFile}"
            );

        DeleteFileQuietly(fullFile);
        Log($"Removed stale Workshop download temp file {Path.GetFileName(fullFile)}");
    }

    private void DeleteDownloadDirectory(string directory)
    {
        var fullDirectory = Path.GetFullPath(directory);
        if (!IsPathInsideDirectory(fullDirectory, _downloadsDirectory))
            throw new InvalidOperationException(
                $"Workshop download temp directory escapes downloads root: {fullDirectory}"
            );

        DeleteQuietly(fullDirectory);
        Log($"Removed stale Workshop download temp directory {Path.GetFileName(fullDirectory)}");
    }

    private void RestoreOrDeleteDownloadBackup(string directory, string publishedFileId)
    {
        var fullDirectory = Path.GetFullPath(directory);
        if (!IsPathInsideDirectory(fullDirectory, _downloadsDirectory))
            throw new InvalidOperationException(
                $"Workshop download temp directory escapes downloads root: {fullDirectory}"
            );

        var activeDirectory = Path.Combine(_downloadsDirectory, publishedFileId);
        if (TryMoveDirectory(fullDirectory, activeDirectory))
        {
            Log($"Restored stale Workshop download backup for {publishedFileId}");
            return;
        }

        DeleteDownloadDirectory(fullDirectory);
    }

    private static bool IsPathInsideDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory);
        if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            fullDirectory += Path.DirectorySeparatorChar;

        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> DownloadToTempFileAsync(
        ulong publishedFileId,
        string url,
        long expectedSize,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Workshop download URL is missing");

        if (expectedSize > MaxWorkshopDownloadBytes)
            throw new InvalidOperationException(
                $"Workshop item is too large ({expectedSize} bytes)"
            );

        var tempFile = Path.Combine(
            _downloadsDirectory,
            $".{publishedFileId}.{Guid.NewGuid():N}.download"
        );
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            ).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > MaxWorkshopDownloadBytes)
                throw new InvalidOperationException(
                    $"Workshop item is too large ({contentLength.Value} bytes)"
                );

            await using var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var output = new FileStream(
                tempFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            var downloadedBytes = await CopyWithSizeLimitAsync(input, output, ct)
                .ConfigureAwait(false);
            if (expectedSize > 0 && downloadedBytes != expectedSize)
                throw new InvalidOperationException(
                    $"Workshop item downloaded size mismatch: expected {expectedSize} bytes but received {downloadedBytes} bytes"
                );

            return tempFile;
        }
        catch
        {
            DeleteFileQuietly(tempFile);
            throw;
        }
    }

    private static async Task<long> CopyWithSizeLimitAsync(
        Stream input,
        Stream output,
        CancellationToken ct
    )
    {
        var buffer = new byte[128 * 1024];
        long total = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0)
                return total;

            total += read;
            if (total > MaxWorkshopDownloadBytes)
                throw new InvalidOperationException(
                    $"Workshop item is too large (over {MaxWorkshopDownloadBytes} bytes)"
                );

            await output.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
        }
    }

    private static void ReplaceDownloadedContent(
        string tempFile,
        string fileName,
        string itemDirectory
    )
    {
        var tempDirectory = itemDirectory + ".tmp-" + Guid.NewGuid().ToString("N");
        var backupDirectory = itemDirectory + ".old-" + Guid.NewGuid().ToString("N");

        Directory.CreateDirectory(tempDirectory);
        try
        {
            if (IsZipFile(fileName, tempFile))
                ExtractZipSafely(tempFile, tempDirectory);
            else
                File.Move(tempFile, Path.Combine(tempDirectory, SafeFileName(fileName)), overwrite: true);

            var backupCreated = TryMoveDirectory(itemDirectory, backupDirectory);

            Directory.Move(tempDirectory, itemDirectory);
            if (backupCreated)
                DeleteQuietly(backupDirectory);
        }
        catch
        {
            DeleteQuietly(tempDirectory);
            TryMoveDirectory(backupDirectory, itemDirectory);
            throw;
        }
        finally
        {
            DeleteFileQuietly(tempFile);
        }
    }

    private static bool IsZipFile(string fileName, string path)
    {
        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return true;

        Span<byte> header = stackalloc byte[4];
        using var file = File.OpenRead(path);
        return file.Read(header) == 4
            && header[0] == 0x50
            && header[1] == 0x4B
            && header[2] == 0x03
            && header[3] == 0x04;
    }

    private static void ExtractZipSafely(string zipPath, string targetDirectory)
    {
        var root = Path.GetFullPath(targetDirectory);
        if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            root += Path.DirectorySeparatorChar;

        long extractedBytes = 0;
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName))
                continue;

            var destination = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unsafe Workshop zip entry: {entry.FullName}");

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            extractedBytes += entry.Length;
            if (extractedBytes > MaxWorkshopDownloadBytes)
                throw new InvalidOperationException(
                    $"Workshop zip expands over {MaxWorkshopDownloadBytes} bytes"
                );

            var parent = Path.GetDirectoryName(destination);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            entry.ExtractToFile(destination, overwrite: true);
        }
    }

    private SteamWorkshopItem Skipped(
        PublishedFileDetails detail,
        ulong manifestId,
        string reason,
        bool isDependency,
        IReadOnlyCollection<ulong> requiredBy,
        DownloadSource source
    )
    {
        Log($"Skipping {DetailTitle(detail)} ({detail.publishedfileid}): {reason}");
        return new SteamWorkshopItem(
            detail.publishedfileid,
            DetailTitle(detail),
            Path.Combine(_downloadsDirectory, detail.publishedfileid.ToString()),
            detail.time_updated,
            manifestId,
            "unsupported",
            reason,
            "",
            source.Kind,
            source.ExpectedSize,
            source.HContentFile,
            isDependency,
            requiredBy
        );
    }

    private static string FileNameFromUgcOrDetail(string fileName, PublishedFileDetails detail)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
            return SafeFileName(fileName);

        return FileNameFromDetail(detail);
    }

    private static string FileNameFromDetail(PublishedFileDetails detail)
    {
        if (!string.IsNullOrWhiteSpace(detail.filename))
            return SafeFileName(detail.filename);

        if (Uri.TryCreate(detail.file_url, UriKind.Absolute, out var uri))
        {
            var name = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(name))
                return SafeFileName(name);
        }

        return $"{detail.publishedfileid}.bin";
    }

    private static long DeclaredDownloadSize(ulong size)
        => size > MaxWorkshopDownloadBytes
            ? MaxWorkshopDownloadBytes + 1
            : (long)size;

    private static string SafeFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safe) ? "content.bin" : safe;
    }

    private static string DetailTitle(PublishedFileDetails detail)
        => string.IsNullOrWhiteSpace(detail.title)
            ? detail.publishedfileid.ToString()
            : detail.title.Trim();

    private static void DeleteQuietly(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return;

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (IOException ex) when (IsMissingPathFailure(ex))
        {
        }
        catch
        {
        }
    }

    private static void DeleteFileQuietly(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
            return;

        try
        {
            File.Delete(file);
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch
        {
        }
    }

    private static bool TryMoveDirectory(string sourceDirectory, string targetDirectory)
    {
        try
        {
            Directory.Move(sourceDirectory, targetDirectory);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch (IOException ex) when (IsMissingPathFailure(ex))
        {
            return false;
        }
    }

    private static bool IsMissingPathFailure(IOException ex)
        => ex.Message.IndexOf("not find", StringComparison.OrdinalIgnoreCase) >= 0
            || ex.Message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0
            || ex.Message.IndexOf("no such file", StringComparison.OrdinalIgnoreCase) >= 0;

    private void Log(string message)
    {
        PatchHelper.Log($"[Workshop] {message}");
        try
        {
            LogMessage?.Invoke(message);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Log callback failed: {ex.Message}");
        }
    }

    void IDisposable.Dispose()
        => Dispose();

    internal void Dispose()
        => _http.Dispose();

    private readonly struct DownloadSource
    {
        internal static DownloadSource Unknown { get; } = new(
            "",
            "",
            0,
            "unknown",
            0,
            null
        );

        internal DownloadSource(
            string url,
            string fileName,
            long expectedSize,
            string kind,
            ulong hContentFile,
            string unsupportedReason
        )
        {
            Url = url ?? "";
            FileName = fileName ?? "";
            ExpectedSize = expectedSize;
            Kind = kind ?? "";
            HContentFile = hContentFile;
            UnsupportedReason = unsupportedReason;
        }

        internal string Url { get; }
        internal string FileName { get; }
        internal long ExpectedSize { get; }
        internal string Kind { get; }
        internal ulong HContentFile { get; }
        internal string UnsupportedReason { get; }
    }
}
