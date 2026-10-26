using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProtoBuf;
using SteamKit2.Internal;
using STS2Mobile.Steam;
using SubscriptionPage = SteamKit2.Internal.CPublishedFile_GetUserFiles_Response;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void WorkshopCmSubscriptionsCollectCompleteInventory()
    {
        // Captured from native Steam UnifiedMessages GetUserFiles#1 with
        // type=mysubscriptions, page size 2. Retain only the original wire
        // total/startindex/item-ID/app-ID fields, removing personal metadata.
        var pages = new[] { NativeSubscriptionPage(1), NativeSubscriptionPage(2) };
        var requestedPages = new List<uint>();
        var ids = SteamConnection.ReadWorkshopSubscriptionPagesAsync(2, page =>
        {
            requestedPages.Add(page);
            return Task.FromResult(pages[requestedPages.Count - 1]);
        }).GetAwaiter().GetResult();
        True(ids.SequenceEqual(new ulong[] { 3747501308, 3737335127, 3747503308, 3737322022 }), "Discovery must preserve the full four-item inventory proved by native Steam.");
        True(requestedPages.SequenceEqual(new uint[] { 1, 2 }), "The proved UM contract uses page numbers, not update offsets.");

        var request = SteamConnection.CreateWorkshopSubscriptionsRequest(76561198000000000, 2, 2);
        Equal("mysubscriptions", request.type, "The subscription selector must match the successful native UM request.");
        Equal(76561198000000000UL, request.steamid, "The query must belong to the authenticated account.");
        Equal(SteamGameApp.AppId, request.appid, "The query must target this game's subscriptions.");
        Equal(2u, request.page, "The request must retain the requested page.");
        Equal(2u, request.numperpage, "The collector and request must use the same page size.");

        var emptyCalls = 0;
        var empty = SteamConnection.ReadWorkshopSubscriptionPagesAsync(2, page =>
        {
            Equal(1u, page, "An empty inventory must start at page one.");
            emptyCalls++;
            return Task.FromResult(CmSubscriptionPage(0, 1));
        }).GetAwaiter().GetResult();
        Equal(0, empty.Count, "An explicit successful total of zero is an authoritative empty inventory.");
        Equal(1, emptyCalls, "An authoritative empty inventory needs only one response.");
    }

    private static void WorkshopCmSubscriptionsRejectIncompleteInventory()
    {
        Throws<InvalidOperationException>(() => SteamConnection.ReadWorkshopSubscriptionPagesAsync(2,
            _ => Task.FromException<SubscriptionPage>(new InvalidOperationException("CM request rejected"))).GetAwaiter().GetResult());
        AssertCmSubscriptionFailure(CmSubscriptionPage(null, 1));
        AssertCmSubscriptionFailure(CmSubscriptionPage(0, null));
        AssertCmSubscriptionFailure(CmSubscriptionPage(1, 1));
        AssertCmSubscriptionFailure(CmSubscriptionPage(3, 1, (42, SteamGameApp.AppId)));
        AssertCmSubscriptionFailure(CmSubscriptionPage(1, 1, (42, SteamGameApp.AppId), (43, SteamGameApp.AppId)));
        AssertCmSubscriptionFailure(
            CmSubscriptionPage(3, 1, (42, SteamGameApp.AppId), (43, SteamGameApp.AppId)),
            CmSubscriptionPage(4, 3, (44, SteamGameApp.AppId), (45, SteamGameApp.AppId)));
        AssertCmSubscriptionFailure(
            CmSubscriptionPage(3, 1, (42, SteamGameApp.AppId), (43, SteamGameApp.AppId)),
            CmSubscriptionPage(3, 3));
        AssertCmSubscriptionFailure(CmSubscriptionPage(1, 0, (42, SteamGameApp.AppId)));
        AssertCmSubscriptionFailure(
            CmSubscriptionPage(3, 1, (42, SteamGameApp.AppId), (43, SteamGameApp.AppId)),
            CmSubscriptionPage(3, 4, (44, SteamGameApp.AppId)));
    }

    private static void WorkshopCmSubscriptionsRejectInvalidIdentities()
    {
        AssertCmSubscriptionFailure(CmSubscriptionPage(1, 1, (0, SteamGameApp.AppId)));
        AssertCmSubscriptionFailure(CmSubscriptionPage(1, 1, (42, 0)));
        AssertCmSubscriptionFailure(CmSubscriptionPage(1, 1, (42, SteamGameApp.AppId + 1)));
        AssertCmSubscriptionFailure(CmSubscriptionPage(2, 1, (42, SteamGameApp.AppId), (42, SteamGameApp.AppId)));
        AssertCmSubscriptionFailure(
            CmSubscriptionPage(3, 1, (42, SteamGameApp.AppId), (43, SteamGameApp.AppId)),
            CmSubscriptionPage(3, 3, (42, SteamGameApp.AppId)));
    }

    private static SubscriptionPage NativeSubscriptionPage(int page)
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream($"STS2Mobile.GameIdentityTests.Fixtures.WorkshopSubscriptions.NativePage{page}.bin")
            ?? throw new InvalidOperationException("Native subscription response fixture is missing.");
        return Serializer.Deserialize<SubscriptionPage>(stream);
    }

    private static SubscriptionPage CmSubscriptionPage(uint? total, uint? startIndex, params (ulong Id, uint AppId)[] items)
    {
        var page = new SubscriptionPage();
        if (total.HasValue)
            page.total = total.Value;
        if (startIndex.HasValue)
            page.startindex = startIndex.Value;
        foreach (var item in items)
            page.publishedfiledetails.Add(new PublishedFileDetails { publishedfileid = item.Id, consumer_appid = item.AppId });
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, page);
        stream.Position = 0;
        return Serializer.Deserialize<SubscriptionPage>(stream);
    }

    private static void AssertCmSubscriptionFailure(params SubscriptionPage[] pages)
    {
        var calls = 0;
        Throws<InvalidOperationException>(() => SteamConnection.ReadWorkshopSubscriptionPagesAsync(2, _ =>
        {
            var index = calls++;
            if (index >= pages.Length)
                throw new InvalidOperationException("The inventory requested a page after a rejected response.");
            return Task.FromResult(pages[index]);
        }).GetAwaiter().GetResult());
        Equal(pages.Length, calls, "An inconsistent CM inventory must fail at the offending response.");
    }
}
