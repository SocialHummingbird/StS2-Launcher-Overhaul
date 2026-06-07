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
            // CM WebSocket traffic must stay on System.Net.WebSockets.Client.
            // The Java HTTP bridge is intentionally limited to normal HTTP(S)
            // requests and cannot service wss:// upgrade/socket traffic.
            // SteamKit passes this client as the HttpMessageInvoker argument to
            // ClientWebSocket.ConnectAsync, so the managed WebSocket assembly is
            // still the transport owner and must keep its Android SHA-1 patch.
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
            PatchHelper.Log("[Auth] Steam CM WebSocket using managed .NET transport");
    }

    private static void LogJavaHttpTransportOnce()
    {
        if (Interlocked.Exchange(ref _javaHttpTransportLogged, 1) == 0)
            PatchHelper.Log("[Auth] Steam WebAPI/CDN using Android Java HTTP bridge");
    }
}
