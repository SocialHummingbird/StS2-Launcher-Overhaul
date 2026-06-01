using System;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private const ProtocolTypes AndroidProtocolTypes = ProtocolTypes.WebSocket;

    internal static SteamConfiguration Create()
    {
        AndroidJavaHttpMessageHandler.Prime();

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
}
