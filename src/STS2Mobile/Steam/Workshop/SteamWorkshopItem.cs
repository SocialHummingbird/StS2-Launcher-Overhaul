using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Mobile.Steam.Workshop;

internal sealed class SteamWorkshopItem
{
    internal SteamWorkshopItem(
        ulong publishedFileId,
        string title,
        string sourceDirectory,
        long timeUpdated,
        ulong manifestId = 0,
        string status = "",
        string error = "",
        string downloadUrl = "",
        string downloadSourceKind = "",
        long expectedDownloadBytes = 0,
        ulong hContentFile = 0,
        bool isDependency = false,
        IReadOnlyCollection<ulong> requiredByPublishedFileIds = null,
        bool reusedCachedDownload = false
    )
    {
        PublishedFileId = publishedFileId;
        Title = string.IsNullOrWhiteSpace(title) ? publishedFileId.ToString() : title.Trim();
        SourceDirectory = sourceDirectory ?? "";
        TimeUpdated = timeUpdated;
        ManifestId = manifestId;
        Status = status ?? "";
        Error = error ?? "";
        DownloadUrl = downloadUrl ?? "";
        DownloadSourceKind = downloadSourceKind ?? "";
        ExpectedDownloadBytes = expectedDownloadBytes;
        HContentFile = hContentFile;
        IsDependency = isDependency;
        ReusedCachedDownload = reusedCachedDownload;
        RequiredByPublishedFileIds = requiredByPublishedFileIds?
            .Where(id => id != 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray() ?? Array.Empty<ulong>();
    }

    internal ulong PublishedFileId { get; }
    internal string Title { get; }
    internal string SourceDirectory { get; }
    internal long TimeUpdated { get; }
    internal ulong ManifestId { get; }
    internal string Status { get; }
    internal string Error { get; }
    internal string DownloadUrl { get; }
    internal string DownloadSourceKind { get; }
    internal long ExpectedDownloadBytes { get; }
    internal ulong HContentFile { get; }
    internal bool IsDependency { get; }
    internal bool ReusedCachedDownload { get; }
    internal IReadOnlyCollection<ulong> RequiredByPublishedFileIds { get; }

    internal string DirectoryName => PublishedFileId.ToString();
}
