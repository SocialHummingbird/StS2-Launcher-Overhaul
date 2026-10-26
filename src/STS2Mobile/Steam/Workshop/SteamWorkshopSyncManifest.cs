using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Mobile.Steam.Workshop;

internal sealed class SteamWorkshopSyncManifest
{
    internal const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public string GeneratedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string DownloadsDirectory { get; set; } = "";
    public string StagedDirectory { get; set; } = "";
    public string SubscriptionQueryType { get; set; } = "";
    public string SubscriptionQueryAttempts { get; set; } = "";
    public string SyncStatus { get; set; } = "";
    public string SyncError { get; set; } = "";
    public string ClearedAtUtc { get; set; } = "";
    public string ClearReason { get; set; } = "";
    public int SubscribedItemCount { get; set; }
    public int DependencyItemCount { get; set; }
    public int MissingDependencyItemCount { get; set; }
    public int TotalItemCount { get; set; }
    public List<ulong> MissingDependencyIds { get; set; } = new();
    public List<string> GameBranchOrder { get; set; } = new();
    public string GameBranchOrderProvenance { get; set; } = "";
    public List<SteamWorkshopSyncManifestItem> Items { get; set; } = new();

    internal static SteamWorkshopSyncManifest Empty(string downloadsDirectory, string stagedDirectory)
        => new()
        {
            Version = CurrentVersion,
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            DownloadsDirectory = downloadsDirectory ?? "",
            StagedDirectory = stagedDirectory ?? "",
            Items = new List<SteamWorkshopSyncManifestItem>(),
        };

    internal void ApplyMetadata(SteamWorkshopSyncMetadata metadata)
    {
        if (metadata == null)
            return;

        SubscriptionQueryType = metadata.SubscriptionQueryType ?? "";
        SubscriptionQueryAttempts = metadata.SubscriptionQueryAttempts ?? "";
        SyncStatus = metadata.SyncStatus ?? "";
        SyncError = metadata.SyncError ?? "";
        ClearedAtUtc = metadata.ClearedAtUtc ?? "";
        ClearReason = metadata.ClearReason ?? "";
        SubscribedItemCount = metadata.SubscribedItemCount;
        DependencyItemCount = metadata.DependencyItemCount;
        MissingDependencyItemCount = metadata.MissingDependencyItemCount;
        TotalItemCount = metadata.TotalItemCount;
        MissingDependencyIds = metadata.MissingDependencyIds?
            .Where(id => id != 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList() ?? new List<ulong>();
        if (metadata.GameBranchOrder != null)
        {
            GameBranchOrder = metadata.GameBranchOrder.ToList();
            GameBranchOrderProvenance = metadata.GameBranchOrderProvenance ?? "";
        }
    }
}

internal sealed class SteamWorkshopSyncMetadata
{
    internal string SubscriptionQueryType { get; init; } = "";
    internal string SubscriptionQueryAttempts { get; init; } = "";
    internal string SyncStatus { get; init; } = "";
    internal string SyncError { get; init; } = "";
    internal string ClearedAtUtc { get; init; } = "";
    internal string ClearReason { get; init; } = "";
    internal int SubscribedItemCount { get; init; }
    internal int DependencyItemCount { get; init; }
    internal int MissingDependencyItemCount { get; init; }
    internal int TotalItemCount { get; init; }
    internal IReadOnlyCollection<ulong> MissingDependencyIds { get; init; } = Array.Empty<ulong>();
    internal IReadOnlyList<string> GameBranchOrder { get; init; }
    internal string GameBranchOrderProvenance { get; init; } = "";
}

internal sealed class SteamWorkshopSyncManifestItem
{
    public SteamWorkshopInstallation Installed { get; set; }
    public ulong PublishedFileId { get; set; }
    public string Title { get; set; } = "";
    public long TimeUpdated { get; set; }
    public string SourceDirectory { get; set; } = "";
    public string StagedDirectory { get; set; } = "";
    public ulong ManifestId { get; set; }
    public bool DownloadUrlPresent { get; set; }
    public string DownloadUrlHost { get; set; } = "";
    public string DownloadSourceKind { get; set; } = "";
    public long ExpectedDownloadBytes { get; set; }
    public ulong HContentFile { get; set; }
    public bool ReusedCachedDownload { get; set; }
    public string ContentSha256 { get; set; } = "";
    public int FileCount { get; set; }
    public bool HasPck { get; set; }
    public bool IsDependency { get; set; }
    public List<ulong> RequiredByPublishedFileIds { get; set; } = new();
    public string Status { get; set; } = "";
    public string Error { get; set; } = "";
}

internal sealed class SteamWorkshopInstallation
{
    public string Generation { get; set; } = "";
    public string Directory { get; set; } = "";
    public ulong ManifestId { get; set; }
    public ulong HContentFile { get; set; }
    public long TimeUpdated { get; set; }
    // These hashes describe the downloaded publisher revision; Directory remains writable by mods.
    public string ContentSha256 { get; set; } = "";
    public bool LegacyUnverified { get; set; }
    public string SelectedGameBranch { get; set; } = "";
    public string RevisionProvenance { get; set; } = "";
    public SteamWorkshopBranchSupport BranchSupport { get; set; }
    public List<SteamWorkshopInstalledFile> Files { get; set; } = new();
    public List<ulong> RequiredPublishedFileIds { get; set; } = new();
}

internal sealed class SteamWorkshopBranchSupport
{
    public string Provenance { get; set; } = "";
    public List<string> BranchOrder { get; set; } = new();
    public List<SteamWorkshopGameVersionRange> Ranges { get; set; } = new();
}

internal sealed class SteamWorkshopGameVersionRange
{
    public string MinimumBranch { get; set; } = "";
    public string MaximumBranch { get; set; } = "";
}

internal sealed class SteamWorkshopInstalledFile
{
    public string Path { get; set; } = "";
    public long Length { get; set; }
    public string Sha256 { get; set; } = "";
}
