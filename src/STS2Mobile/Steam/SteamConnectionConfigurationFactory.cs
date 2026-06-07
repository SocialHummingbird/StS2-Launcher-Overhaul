using System;
using System.Threading;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private const ProtocolTypes AndroidProtocolTypes = ProtocolTypes.WebSocket;
    private static int _androidProtocolLogged;

    internal static SteamConfiguration Create()
    {
        AndroidJavaHttpMessageHandler.Prime();
        LogAndroidProtocolConfigurationOnce();

        var config = SteamConfiguration.Create(builder =>
        {
            builder.WithProtocolTypes(
                OperatingSystem.IsAndroid() ? AndroidProtocolTypes : ProtocolTypes.WebSocket
            );

            if (!OperatingSystem.IsAndroid())
                return;

            builder.WithHttpClientFactory(AndroidJavaHttpMessageHandler.CreateClient);
            builder.WithMachineInfoProvider(AndroidMachineInfo);
        });

        SeedAndroidMachineIdCache(config);
        return config;
    }

    private static void LogAndroidProtocolConfigurationOnce()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        if (Interlocked.Exchange(ref _androidProtocolLogged, 1) == 0)
            PatchHelper.Log("[Auth] Android Steam CM protocol configured: WebSocket");
    }
}
