using System;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    internal static SteamConfiguration Create()
    {
        AndroidJavaHttpMessageHandler.Prime();

        var config = SteamConfiguration.Create(builder =>
        {
            builder.WithProtocolTypes(OperatingSystem.IsAndroid() ? ProtocolTypes.Tcp : ProtocolTypes.WebSocket);

            if (!OperatingSystem.IsAndroid())
                return;

            builder.WithHttpClientFactory(AndroidJavaHttpMessageHandler.CreateClient);
            builder.WithMachineInfoProvider(new AndroidMachineInfoProvider());
        });

        SeedAndroidMachineIdCache(config);
        return config;
    }
}
