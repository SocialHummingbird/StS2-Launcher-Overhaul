using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private const int WorkshopRpcTimeoutMs = 45_000;
    private const uint WorkshopSubscriptionPageSize = 100;

    internal string LastWorkshopSubscriptionQueryType { get; private set; } = "";
    internal string LastWorkshopSubscriptionQueryAttempts { get; private set; } = "";

    internal static async Task<IReadOnlyList<ulong>> ReadWorkshopSubscriptionPagesAsync(
        uint pageSize,
        Func<uint, Task<CPublishedFile_GetUserFiles_Response>> fetchPage
    )
    {
        if (pageSize == 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        var ids = new List<ulong>();
        var seen = new HashSet<ulong>();
        uint? total = null;
        uint pageNumber = 1;
        do
        {
            var page = await fetchPage(pageNumber).ConfigureAwait(false);
            if (page == null || !page.ShouldSerializetotal())
                throw new InvalidOperationException($"Steam CM Workshop subscription page {pageNumber} omitted its authoritative total");
            if (total.HasValue && total.Value != page.total)
                throw new InvalidOperationException($"Steam CM Workshop subscription total changed from {total.Value} to {page.total} during pagination");
            total = page.total;
            var expectedStart = checked((uint)ids.Count + 1);
            if (!page.ShouldSerializestartindex() || page.startindex != expectedStart)
                throw new InvalidOperationException($"Steam CM Workshop subscription page {pageNumber} starts at {page.startindex}, expected {expectedStart}");
            var count = checked((uint)page.publishedfiledetails.Count);
            if (count > pageSize || (ulong)ids.Count + count > total.Value)
                throw new InvalidOperationException($"Steam CM Workshop subscription page {pageNumber} exceeds its page size or total of {total.Value} items");
            foreach (var item in page.publishedfiledetails)
            {
                if (item == null || item.publishedfileid == 0 || item.consumer_appid != SteamGameApp.AppId)
                    throw new InvalidOperationException($"Steam CM Workshop subscription page {pageNumber} contained a missing item ID or an unexpected app ID");
                if (!seen.Add(item.publishedfileid))
                    throw new InvalidOperationException($"Steam CM Workshop subscription inventory repeated item {item.publishedfileid}");
                ids.Add(item.publishedfileid);
            }
            if ((uint)ids.Count < total.Value && count != pageSize)
                throw new InvalidOperationException($"Steam CM returned an incomplete Workshop subscription page: received {ids.Count} of {total.Value} items");
            pageNumber++;
        }
        while ((uint)ids.Count < total.Value);
        return ids;
    }

    internal static CPublishedFile_GetUserFiles_Request CreateWorkshopSubscriptionsRequest(ulong steamId, uint page, uint pageSize)
        => new()
        {
            steamid = steamId,
            appid = SteamGameApp.AppId,
            type = "mysubscriptions",
            page = page,
            numperpage = pageSize,
        };

    internal async Task<SteamWorkshopBranchOrder> GetWorkshopGameBranchOrderAsync()
    {
        var app = await GetAppInfoAsync(SteamGameApp.AppId, 0).ConfigureAwait(false);
        var branches = app?.KeyValues?["depots"]?["branches"];
        if (branches == null || branches == KeyValue.Invalid || branches.Children.Count == 0)
            throw new InvalidOperationException("Steam app metadata did not expose a game branch list");
        // This provider's raw order was positively compared with this app's
        // native GetBetaInfo order; never substitute the sorted UI catalog.
        return new SteamWorkshopBranchOrder(
            branches.Children.Select(branch => branch.Name).ToArray(),
            $"steam-pics:depots.branches-raw-order;app={SteamGameApp.AppId};native-GetBetaInfo-protocol-order-corroborated-2026-10-25",
            isVerified: true
        );
    }

    internal async Task<IReadOnlyList<PublishedFileDetails>> GetWorkshopSubscriptionsAsync()
    {
        const string queryType = "cm-mysubscriptions";
        var attempts = new List<string>();
        try
        {
            var ids = await ReadWorkshopSubscriptionPagesAsync(WorkshopSubscriptionPageSize,
                page => GetWorkshopSubscriptionsPageAsync(page, WorkshopSubscriptionPageSize)).ConfigureAwait(false);
            attempts.Add($"{queryType}:{ids.Count}");
            LastWorkshopSubscriptionQueryType = queryType;
            LastWorkshopSubscriptionQueryAttempts = string.Join(", ", attempts);
            PatchHelper.Log($"[Workshop] Steam CM subscription inventory complete: {ids.Count} item(s)");
            return ids.Select(id => new PublishedFileDetails { publishedfileid = id, consumer_appid = SteamGameApp.AppId }).ToArray();
        }
        catch (Exception ex)
        {
            attempts.Add($"{queryType}:failed:{ex.GetBaseException().Message}");
            LastWorkshopSubscriptionQueryType = "";
            LastWorkshopSubscriptionQueryAttempts = string.Join(", ", attempts);
            PatchHelper.Log($"[Workshop] Steam CM subscription discovery failed: {ex.Message}");
            return await GetWorkshopSubscriptionsFromCommunityAsync(attempts, ex)
                .ConfigureAwait(false);
        }
    }

    internal async Task<IReadOnlyDictionary<ulong, CPublishedFile_GetItemInfo_Response.WorkshopItemInfo>>
        GetWorkshopItemInfoAsync(IEnumerable<PublishedFileDetails> details)
    {
        var itemIds = details?
            .Select(detail => detail.publishedfileid)
            .Where(id => id != 0)
            .Distinct()
            .ToArray() ?? Array.Empty<ulong>();
        if (itemIds.Length == 0)
            return new Dictionary<ulong, CPublishedFile_GetItemInfo_Response.WorkshopItemInfo>();

        var request = new CPublishedFile_GetItemInfo_Request
        {
            appid = SteamGameApp.AppId,
        };
        foreach (var id in itemIds)
        {
            request.workshop_items.Add(new CPublishedFile_GetItemInfo_Request.WorkshopItem
            {
                published_file_id = id,
            });
        }

        var response = await SendPublishedFileAsync(
            "GetItemInfo",
            () => _publishedFile.GetItemInfo(request).ToTask()
        ).ConfigureAwait(false);

        return response.workshop_items
            .Where(item => item.published_file_id != 0)
            .GroupBy(item => item.published_file_id)
            .ToDictionary(group => group.Key, group => group.First());
    }

    internal async Task<IReadOnlyList<PublishedFileDetails>> GetWorkshopDetailsAsync(IEnumerable<ulong> itemIds)
    {
        var ids = itemIds?
            .Where(id => id != 0)
            .Distinct()
            .ToArray() ?? Array.Empty<ulong>();
        if (ids.Length == 0)
            return Array.Empty<PublishedFileDetails>();

        var request = new CPublishedFile_GetDetails_Request
        {
            appid = SteamGameApp.AppId,
            includechildren = true,
            includemetadata = true,
            short_description = true,
            strip_description_bbcode = true,
        };
        request.publishedfileids.AddRange(ids);

        var response = await SendPublishedFileAsync(
            "GetDetails",
            () => _publishedFile.GetDetails(request).ToTask()
        ).ConfigureAwait(false);

        return response.publishedfiledetails
            .Where(detail => detail.publishedfileid != 0)
            .GroupBy(detail => detail.publishedfileid)
            .Select(group => group.First())
            .ToArray();
    }

    internal async Task<SteamCloud.UGCDetailsCallback> GetWorkshopUgcDetailsAsync(ulong hcontent)
    {
        if (hcontent == 0)
            throw new ArgumentException("Workshop UGC content handle is missing", nameof(hcontent));

        EnsureConnected();

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            ThrowIfDisposing();
            var callback = await WaitForSteamJobAsync(
                "SteamCloud.RequestUGCDetails",
                _steamCloud.RequestUGCDetails(new UGCHandle(hcontent)).ToTask(),
                WorkshopRpcTimeoutMs
            ).ConfigureAwait(false);
            if (callback.Result != EResult.OK)
                throw new InvalidOperationException(
                    $"SteamCloud.RequestUGCDetails failed: {callback.Result}"
                );

            return callback;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task<CPublishedFile_GetUserFiles_Response> GetWorkshopSubscriptionsPageAsync(uint page, uint pageSize)
    {
        var response = await SendPublishedFileAsync($"GetUserFiles:mysubscriptions:{page}",
            () => _publishedFile.GetUserFiles(CreateWorkshopSubscriptionsRequest(
                _steamUser.SteamID.ConvertToUInt64(), page, pageSize)).ToTask()).ConfigureAwait(false);
        var total = response.ShouldSerializetotal() ? response.total.ToString() : "missing";
        var start = response.ShouldSerializestartindex() ? response.startindex.ToString() : "missing";
        PatchHelper.Log($"[Workshop] Steam CM mysubscriptions page={page} start={start} items={response.publishedfiledetails.Count} total={total}");
        return response;
    }

    private async Task<TBody> SendPublishedFileAsync<TBody>(
        string method,
        Func<Task<SteamUnifiedMessages.ServiceMethodResponse<TBody>>> send
    )
        where TBody : ProtoBuf.IExtensible, new()
    {
        EnsureConnected();

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            ThrowIfDisposing();
            var response = await WaitForSteamJobAsync(
                $"PublishedFile.{method}",
                send(),
                WorkshopRpcTimeoutMs
            ).ConfigureAwait(false);
            if (response.Result != EResult.OK)
                throw new InvalidOperationException($"PublishedFile.{method} failed: {response.Result}");

            return response.Body;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static async Task<TResponse> WaitForSteamJobAsync<TResponse>(
        string operation,
        Task<TResponse> task,
        int timeoutMs
    )
    {
        var deadline = Environment.TickCount64 + timeoutMs;

        while (!task.IsCompleted)
        {
            if (Environment.TickCount64 >= deadline)
                throw new TimeoutException($"{operation} timed out after {timeoutMs}ms");

            if (OperatingSystem.IsAndroid())
                AndroidBridgeDispatcher.Pump();

            var remaining = Math.Max(1, deadline - Environment.TickCount64);
            await Task.WhenAny(task, Task.Delay((int)Math.Min(50, remaining))).ConfigureAwait(false);
        }

        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException($"{operation} was canceled before completion", ex);
        }
    }
}
