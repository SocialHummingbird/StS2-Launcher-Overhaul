using System;
using SteamKit2;
using NetHttpClient = System.Net.Http.HttpClient;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    internal static void Prime()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        _ = AndroidGodotAppBridge.TryGetInstance(
            out _,
            "[Auth] Java HTTP bridge unavailable"
        );
    }

    internal static NetHttpClient CreateClient(HttpClientPurpose purpose)
    {
        if (!OperatingSystem.IsAndroid())
            return new NetHttpClient();

        var timeout = purpose == HttpClientPurpose.CDN
            ? TimeSpan.FromMinutes(2)
            : TimeSpan.FromMilliseconds(DefaultTimeoutMs);

        return new NetHttpClient(new AndroidJavaHttpMessageHandler((int)timeout.TotalMilliseconds))
        {
            Timeout = timeout,
        };
    }

    internal static NetHttpClient CreateCdnClient()
        => CreateClient(HttpClientPurpose.CDN);
}
