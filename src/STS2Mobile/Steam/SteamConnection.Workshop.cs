using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private const int WorkshopRpcTimeoutMs = 45_000;
    private const uint WorkshopPageSize = 100;
    private static readonly WorkshopSubscriptionQuery[] WorkshopSubscriptionQueries =
    {
        new("subscriptions:minimal", "subscriptions", false, false, false),
        new("subscribed:minimal", "subscribed", false, false, false),
        new("subscriptions:creator", "subscriptions", true, false, false),
        new("subscribed:creator", "subscribed", true, false, false),
        new("subscriptions:details", "subscriptions", false, false, true),
        new("subscribed:details", "subscribed", false, false, true),
        new("subscriptions:full", "subscriptions", true, true, true),
        new("subscribed:full", "subscribed", true, true, true),
    };

    internal string LastWorkshopSubscriptionQueryType { get; private set; } = "";
    internal string LastWorkshopSubscriptionQueryAttempts { get; private set; } = "";

    internal async Task<IReadOnlyList<PublishedFileDetails>> GetWorkshopSubscriptionsAsync()
    {
        Exception lastFailure = null;
        var attempts = new List<string>();
        foreach (var query in WorkshopSubscriptionQueries)
        {
            try
            {
                var details = await GetWorkshopUserFilesByQueryAsync(query).ConfigureAwait(false);
                attempts.Add($"{query.Label}:{details.Count}");
                PatchHelper.Log(
                    $"[Workshop] Steam GetUserFiles query={query.Label} type={query.Type} returned {details.Count} item(s)"
                );

                if (details.Count > 0 || query.Equals(WorkshopSubscriptionQueries[^1]))
                {
                    LastWorkshopSubscriptionQueryType = query.Label;
                    LastWorkshopSubscriptionQueryAttempts = string.Join(", ", attempts);
                    return details;
                }
            }
            catch (Exception ex)
            {
                lastFailure = ex;
                attempts.Add($"{query.Label}:failed:{ex.GetBaseException().Message}");
                PatchHelper.Log($"[Workshop] Steam GetUserFiles query={query.Label} failed: {ex.Message}");
            }
        }

        LastWorkshopSubscriptionQueryType = "";
        LastWorkshopSubscriptionQueryAttempts = string.Join(", ", attempts);
        if (lastFailure != null)
            return await GetWorkshopSubscriptionsFromCommunityAsync(attempts, lastFailure)
                .ConfigureAwait(false);

        return Array.Empty<PublishedFileDetails>();
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
            appid = SteamCloudApp.AppId,
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
            appid = SteamCloudApp.AppId,
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

    private async Task<IReadOnlyList<PublishedFileDetails>> GetWorkshopUserFilesByQueryAsync(
        WorkshopSubscriptionQuery query
    )
    {
        var results = new List<PublishedFileDetails>();
        uint page = 1;
        uint total = 0;

        do
        {
            var response = await GetWorkshopUserFilesPageAsync(query, page).ConfigureAwait(false);
            total = response.total;
            if (response.publishedfiledetails.Count == 0)
                break;

            results.AddRange(response.publishedfiledetails);
            page++;
        }
        while (results.Count < total);

        return results
            .Where(detail => detail.consumer_appid == 0 || detail.consumer_appid == SteamCloudApp.AppId)
            .GroupBy(detail => detail.publishedfileid)
            .Select(group => group.First())
            .ToArray();
    }

    private Task<CPublishedFile_GetUserFiles_Response> GetWorkshopUserFilesPageAsync(
        WorkshopSubscriptionQuery query,
        uint page
    )
    {
        var request = new CPublishedFile_GetUserFiles_Request
        {
            steamid = _steamUser.SteamID.ConvertToUInt64(),
            appid = SteamCloudApp.AppId,
            page = page,
            numperpage = WorkshopPageSize,
            type = query.Type,
        };
        if (query.IncludeCreatorAppId)
            request.creator_appid = SteamCloudApp.AppId;
        if (query.IncludeSortMethod)
            request.sortmethod = "lastupdated";
        if (query.IncludeDetails)
        {
            request.return_children = true;
            request.return_metadata = true;
            request.return_short_description = true;
            request.return_tags = true;
        }

        return SendPublishedFileAsync(
            $"GetUserFiles:{query.Label}:{page}",
            () => _publishedFile.GetUserFiles(request).ToTask()
        );
    }

    private readonly record struct WorkshopSubscriptionQuery(
        string Label,
        string Type,
        bool IncludeCreatorAppId,
        bool IncludeSortMethod,
        bool IncludeDetails
    );

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
