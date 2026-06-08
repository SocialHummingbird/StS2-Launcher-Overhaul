using System;
using System.Threading;
using SteamKit2;
using NetHttpClient = System.Net.Http.HttpClient;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static int _cmWebSocketTransportLogged;
    private static int _javaHttpTransportLogged;

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

        if (purpose == HttpClientPurpose.CMWebSocket)
        {
            // Android CM traffic is configured for TCP to avoid the Mono/Godot
            // SslStream abort hit by System.Net.WebSockets.Client. This path is
            // retained only as a defensive fallback if SteamKit asks for a
            // CMWebSocket client despite the Android protocol configuration.
            LogCmWebSocketTransportOnce();
            return new NetHttpClient()
            {
                Timeout = TimeSpan.FromMilliseconds(DefaultTimeoutMs),
            };
        }

        LogJavaHttpTransportOnce();
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

    private static void LogCmWebSocketTransportOnce()
    {
        if (Interlocked.Exchange(ref _cmWebSocketTransportLogged, 1) == 0)
            PatchHelper.Log("[Auth] Unexpected Steam CM WebSocket using managed .NET transport");
    }

    private static void LogJavaHttpTransportOnce()
    {
        if (Interlocked.Exchange(ref _javaHttpTransportLogged, 1) == 0)
            PatchHelper.Log("[Auth] Steam WebAPI/CDN using Android Java HTTP bridge");
    }
}
