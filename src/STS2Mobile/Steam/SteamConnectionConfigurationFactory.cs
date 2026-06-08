using System;
using System.Threading;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private const ProtocolTypes AndroidProtocolTypes = ProtocolTypes.Tcp;
    private static int _androidProtocolLogged;
    private static int _androidSteamKitDebugLogged;

    internal static SteamConfiguration Create()
    {
        AndroidJavaHttpMessageHandler.Prime();
        ConfigureAndroidSteamKitDebugLogOnce();
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
            PatchHelper.Log("[Auth] Android Steam CM protocol configured: TCP");
    }

    private static void ConfigureAndroidSteamKitDebugLogOnce()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        if (Interlocked.Exchange(ref _androidSteamKitDebugLogged, 1) != 0)
            return;

        DebugLog.Enabled = true;
        DebugLog.AddListener((category, message) =>
        {
            if (
                string.IsNullOrWhiteSpace(category)
                || string.IsNullOrWhiteSpace(message)
            )
                return;

            PatchHelper.Log($"[Auth][SteamKit:{category}] {message}");
        });
    }
}
