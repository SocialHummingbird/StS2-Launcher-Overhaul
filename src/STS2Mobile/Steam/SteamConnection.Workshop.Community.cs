using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private const int WorkshopCommunityPageSize = 30;
    private const int WorkshopCommunityMaxPages = 50;
    private static readonly Regex WorkshopCommunityFileIdRegex = new(
        @"(?:publishedfileid|data-publishedfileid|[?&]id)=""?(?<id>\d{5,})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    private async Task<IReadOnlyList<PublishedFileDetails>> GetWorkshopSubscriptionsFromCommunityAsync(
        List<string> attempts,
        Exception lastFailure
    )
    {
        try
        {
            var ids = await GetWorkshopSubscriptionIdsFromCommunityAsync().ConfigureAwait(false);
            attempts.Add($"community-web:{ids.Count}");
            LastWorkshopSubscriptionQueryType = "community-web";
            LastWorkshopSubscriptionQueryAttempts = string.Join(", ", attempts);
            PatchHelper.Log($"[Workshop] Steam Community subscriptions returned {ids.Count} item id(s)");
            return await GetWorkshopDetailsAsync(ids).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            attempts.Add($"community-web:failed:{ex.GetBaseException().Message}");
            LastWorkshopSubscriptionQueryType = "";
            LastWorkshopSubscriptionQueryAttempts = string.Join(", ", attempts);
            PatchHelper.Log($"[Workshop] Steam Community subscription discovery failed: {ex.Message}");
            throw new InvalidOperationException(
                $"Steam Workshop subscription discovery failed after attempts: {LastWorkshopSubscriptionQueryAttempts}",
                lastFailure
            );
        }
    }

    private async Task<IReadOnlyCollection<ulong>> GetWorkshopSubscriptionIdsFromCommunityAsync()
    {
        var token = await GenerateSteamCommunityAccessTokenAsync().ConfigureAwait(false);
        var steamId = _steamUser.SteamID.ConvertToUInt64();
        using var http = AndroidJavaHttpMessageHandler.CreateClient(HttpClientPurpose.WebAPI);
        var ids = new HashSet<ulong>();

        for (var page = 1; page <= WorkshopCommunityMaxPages; page++)
        {
            var html = await GetWorkshopSubscriptionsCommunityPageAsync(
                http,
                steamId,
                token,
                page
            ).ConfigureAwait(false);
            var pageIds = ParseWorkshopSubscriptionIds(html);
            if (page == 1 && pageIds.Count == 0 && LooksLikeSteamLoginPage(html))
                throw new InvalidOperationException("Steam Community returned a login page for Workshop subscriptions");

            var added = 0;
            foreach (var id in pageIds)
                if (ids.Add(id))
                    added++;

            PatchHelper.Log($"[Workshop] Steam Community subscription page {page} yielded {pageIds.Count} id(s), added {added}");
            if (added == 0 || !HasWorkshopCommunityNextPage(html, page + 1))
                break;
        }

        return ids.OrderBy(id => id).ToArray();
    }

    private async Task<string> GenerateSteamCommunityAccessTokenAsync()
    {
        var result = await RunConnectedAsync(
            async () => await _client.Authentication.GenerateAccessTokenForAppAsync(
                _steamUser.SteamID,
                _refreshToken,
                allowRenewal: false
            )
        ).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(result.AccessToken))
            throw new InvalidOperationException("Steam auth returned no web access token for Community Workshop discovery");

        return result.AccessToken;
    }

    private static async Task<string> GetWorkshopSubscriptionsCommunityPageAsync(
        HttpClient http,
        ulong steamId,
        string accessToken,
        int page
    )
    {
        var sessionId = Guid.NewGuid().ToString("N")[..24];
        var url =
            $"https://steamcommunity.com/profiles/{steamId}/myworkshopfiles/"
            + $"?appid={SteamCloudApp.AppId}"
            + "&browsefilter=mysubscriptions"
            + "&section=items"
            + $"&p={page}"
            + $"&numperpage={WorkshopCommunityPageSize}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "StS2MobileLauncher/1.0");
        request.Headers.TryAddWithoutValidation(
            "Cookie",
            $"steamLoginSecure={Uri.EscapeDataString($"{steamId}||{accessToken}")}; sessionid={sessionId}"
        );

        using var response = await http.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    private static IReadOnlyCollection<ulong> ParseWorkshopSubscriptionIds(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return Array.Empty<ulong>();

        return WorkshopCommunityFileIdRegex.Matches(html)
            .Select(match => match.Groups["id"].Value)
            .Where(value => ulong.TryParse(value, out _))
            .Select(ulong.Parse)
            .Where(id => id != 0)
            .Distinct()
            .ToArray();
    }

    private static bool HasWorkshopCommunityNextPage(string html, int nextPage)
        => !string.IsNullOrWhiteSpace(html)
            && (
                html.IndexOf($"> {nextPage} <", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf($"p={nextPage}", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf($"p={nextPage}&", StringComparison.OrdinalIgnoreCase) >= 0
            );

    private static bool LooksLikeSteamLoginPage(string html)
        => !string.IsNullOrWhiteSpace(html)
            && (
                html.IndexOf("class=\"loginbox\"", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf("id=\"loginForm\"", StringComparison.OrdinalIgnoreCase) >= 0
                || html.IndexOf("g_steamID = false", StringComparison.OrdinalIgnoreCase) >= 0
            );
}
