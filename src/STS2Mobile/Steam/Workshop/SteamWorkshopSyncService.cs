using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam.Workshop;

internal sealed class SteamWorkshopSyncService
{
    private readonly SteamConnection _connection;
    private readonly string _downloadsDirectory;
    private readonly string _stagedDirectory;
    private readonly string _manifestPath;

    internal SteamWorkshopSyncService(SteamConnection connection)
        : this(
            connection,
            AppPaths.AppPrivateWorkshopDownloadsDir,
            AppPaths.AppPrivateWorkshopStagedModsDir,
            AppPaths.AppPrivateWorkshopManifestPath
        )
    {
    }

    internal SteamWorkshopSyncService(
        SteamConnection connection,
        string downloadsDirectory,
        string stagedDirectory,
        string manifestPath
    )
    {
        _connection = connection;
        _downloadsDirectory = downloadsDirectory;
        _stagedDirectory = stagedDirectory;
        _manifestPath = manifestPath;
    }

    internal event Action<string> LogMessage;

    internal async Task<SteamWorkshopSyncManifest> SyncAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_downloadsDirectory);
        Directory.CreateDirectory(_stagedDirectory);
        var stager = new SteamWorkshopStager(_downloadsDirectory, _stagedDirectory, _manifestPath);

        Log("Discovering subscribed Workshop items");
        IReadOnlyList<PublishedFileDetails> subscriptions;
        try
        {
            subscriptions = await _connection.GetWorkshopSubscriptionsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            stager.SaveFailure(new SteamWorkshopSyncMetadata
            {
                SubscriptionQueryType = _connection.LastWorkshopSubscriptionQueryType,
                SubscriptionQueryAttempts = _connection.LastWorkshopSubscriptionQueryAttempts,
                SyncStatus = "discovery-failed",
                SyncError = message,
            });
            throw;
        }

        var resolved = await ExpandDependenciesAsync(subscriptions, ct).ConfigureAwait(false);
        Log(
            $"Discovered {subscriptions.Count} subscribed item(s), {resolved.DiscoveredDependencyItemCount} dependency item(s), {resolved.MissingDependencyIds.Count} missing dependency item(s)"
        );

        var previousItems = stager.LoadManifest().Items
            .Where(item => item.PublishedFileId != 0)
            .GroupBy(item => item.PublishedFileId)
            .ToDictionary(group => group.Key, group => group.First());

        var itemInfo = await _connection.GetWorkshopItemInfoAsync(resolved.Details).ConfigureAwait(false);
        using var downloader = new SteamWorkshopDownloader(_connection, _downloadsDirectory);
        downloader.LogMessage += Log;
        var downloadedItems = await downloader.DownloadAsync(
            resolved.Details,
            itemInfo,
            resolved.SubscribedItemIds,
            resolved.DependencyParents,
            previousItems,
            ct
        ).ConfigureAwait(false);

        var manifest = stager.Stage(downloadedItems, new SteamWorkshopSyncMetadata
        {
            SubscriptionQueryType = _connection.LastWorkshopSubscriptionQueryType,
            SubscriptionQueryAttempts = _connection.LastWorkshopSubscriptionQueryAttempts,
            SubscribedItemCount = resolved.SubscribedItemIds.Count,
            DependencyItemCount = resolved.DiscoveredDependencyItemCount,
            TotalItemCount = resolved.Details.Count,
            MissingDependencyItemCount = resolved.MissingDependencyIds.Count,
            MissingDependencyIds = resolved.MissingDependencyIds,
        });
        Log(FormatSummary(manifest));
        return manifest;
    }

    private async Task<ResolvedWorkshopItems> ExpandDependenciesAsync(
        IReadOnlyList<PublishedFileDetails> subscriptions,
        CancellationToken ct
    )
    {
        var details = new Dictionary<ulong, PublishedFileDetails>();
        var subscribedIds = new HashSet<ulong>();
        var dependencyParents = new Dictionary<ulong, HashSet<ulong>>();
        var pending = new Queue<ulong>();
        var pendingIds = new HashSet<ulong>();

        foreach (var detail in subscriptions ?? Array.Empty<PublishedFileDetails>())
        {
            if (detail.publishedfileid == 0)
                continue;

            subscribedIds.Add(detail.publishedfileid);
            details[detail.publishedfileid] = detail;
            EnqueueChildren(detail, details, pending, pendingIds, dependencyParents);
        }

        while (pending.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var batch = DequeueBatch(pending, pendingIds, 100);
            Log($"Discovering {batch.Count} Workshop dependency item(s)");
            var dependencyDetails = await _connection.GetWorkshopDetailsAsync(batch).ConfigureAwait(false);
            foreach (var detail in dependencyDetails)
            {
                if (detail.publishedfileid == 0 || details.ContainsKey(detail.publishedfileid))
                    continue;

                details[detail.publishedfileid] = detail;
                EnqueueChildren(detail, details, pending, pendingIds, dependencyParents);
            }
        }

        var missingDependencyIds = dependencyParents.Keys
            .Where(id => !details.ContainsKey(id))
            .OrderBy(id => id)
            .ToArray();
        var orderedDetails = details.Values
            .OrderByDescending(detail => subscribedIds.Contains(detail.publishedfileid))
            .ThenBy(detail => detail.title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(detail => detail.publishedfileid)
            .ToArray();

        return new ResolvedWorkshopItems(
            orderedDetails,
            subscribedIds,
            dependencyParents.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyCollection<ulong>)pair.Value.OrderBy(id => id).ToArray()
            ),
            missingDependencyIds
        );
    }

    private static void EnqueueChildren(
        PublishedFileDetails detail,
        IReadOnlyDictionary<ulong, PublishedFileDetails> known,
        Queue<ulong> pending,
        HashSet<ulong> pendingIds,
        Dictionary<ulong, HashSet<ulong>> dependencyParents
    )
    {
        foreach (var child in detail.children ?? Enumerable.Empty<PublishedFileDetails.Child>())
        {
            if (child.publishedfileid == 0 || child.publishedfileid == detail.publishedfileid)
                continue;

            if (!dependencyParents.TryGetValue(child.publishedfileid, out var parents))
            {
                parents = new HashSet<ulong>();
                dependencyParents[child.publishedfileid] = parents;
            }

            parents.Add(detail.publishedfileid);
            if (!known.ContainsKey(child.publishedfileid) && pendingIds.Add(child.publishedfileid))
                pending.Enqueue(child.publishedfileid);
        }
    }

    private static IReadOnlyList<ulong> DequeueBatch(Queue<ulong> pending, HashSet<ulong> pendingIds, int count)
    {
        var ids = new List<ulong>(count);
        while (ids.Count < count && pending.Count > 0)
        {
            var id = pending.Dequeue();
            if (!ids.Contains(id))
                ids.Add(id);
        }

        foreach (var id in ids)
            pendingIds.Remove(id);

        return ids;
    }

    private static string FormatSummary(SteamWorkshopSyncManifest manifest)
    {
        var staged = manifest.Items.Count(item => item.Status == "staged");
        var noPck = manifest.Items.Count(item => item.Status == "staged-no-pck");
        var unsupported = manifest.Items.Count(item => item.Status == "unsupported");
        var failed = manifest.Items.Count(item =>
            !string.IsNullOrWhiteSpace(item.Status)
            && item.Status.EndsWith("failed", StringComparison.OrdinalIgnoreCase)
        );
        return $"Workshop sync complete: staged={staged}, stagedNoPck={noPck}, unsupported={unsupported}, failed={failed}";
    }

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

    private sealed class ResolvedWorkshopItems
    {
        internal ResolvedWorkshopItems(
            IReadOnlyList<PublishedFileDetails> details,
            IReadOnlyCollection<ulong> subscribedItemIds,
            IReadOnlyDictionary<ulong, IReadOnlyCollection<ulong>> dependencyParents,
            IReadOnlyCollection<ulong> missingDependencyIds
        )
        {
            Details = details;
            SubscribedItemIds = subscribedItemIds;
            DependencyParents = dependencyParents;
            MissingDependencyIds = missingDependencyIds;
        }

        internal IReadOnlyList<PublishedFileDetails> Details { get; }
        internal IReadOnlyCollection<ulong> SubscribedItemIds { get; }
        internal IReadOnlyDictionary<ulong, IReadOnlyCollection<ulong>> DependencyParents { get; }
        internal IReadOnlyCollection<ulong> MissingDependencyIds { get; }
        internal int DiscoveredDependencyItemCount
            => Details.Count(detail => !SubscribedItemIds.Contains(detail.publishedfileid));
    }
}
