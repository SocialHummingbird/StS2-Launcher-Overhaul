using System;
using System.Text.RegularExpressions;
using System.Threading;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private const ProtocolTypes AndroidProtocolTypes = ProtocolTypes.Tcp;
    internal const bool SteamKitDebugLogsSanitized = true;
    internal static bool SteamKitDebugLogsOptInEnabled
        => OperatingSystem.IsAndroid()
            && string.Equals(
                Environment.GetEnvironmentVariable("STS2_STEAMKIT_DEBUG_LOGS"),
                "1",
                StringComparison.Ordinal
            );
    private static readonly Regex SensitiveJsonValueRegex = new Regex(
        "\"(?<key>password|passwd|refresh[_-]?token|access[_-]?token|login[_-]?key|steamLoginSecure|sessionid|shared[_-]?secret|identity[_-]?secret|guard[_-]?code|twofactorcode|authorization)\"\\s*:\\s*\"[^\"]*\"",
        RegexOptions.IgnoreCase
    );
    private static readonly Regex SensitiveKeyValueRegex = new Regex(
        "\\b(?<key>password|passwd|refresh[_-]?token|access[_-]?token|login[_-]?key|steamLoginSecure|sessionid|shared[_-]?secret|identity[_-]?secret|guard[_-]?code|twofactorcode|authorization)\\b\\s*[:=]\\s*['\"]?[^'\"\\s,;&]+",
        RegexOptions.IgnoreCase
    );
    private static readonly Regex BearerTokenRegex = new Regex(
        "\\bBearer\\s+[A-Za-z0-9._~+/=-]+",
        RegexOptions.IgnoreCase
    );
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

        if (!SteamKitDebugLogsOptInEnabled)
        {
            DebugLog.Enabled = false;
            PatchHelper.Log("[Auth] Android SteamKit debug logging disabled by default; set sts2_steamkit_debug_logs=1 to enable sanitized diagnostics");
            return;
        }

        DebugLog.Enabled = true;
        PatchHelper.Log("[Auth] Android SteamKit debug logging enabled with credential/token sanitization");
        DebugLog.AddListener((category, message) =>
        {
            if (
                string.IsNullOrWhiteSpace(category)
                || string.IsNullOrWhiteSpace(message)
            )
                return;

            PatchHelper.Log(
                $"[Auth][SteamKit:{SanitizeSteamKitDebugMessage(category)}] {SanitizeSteamKitDebugMessage(message)}"
            );
        });
    }

    private static string SanitizeSteamKitDebugMessage(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sanitized = SensitiveJsonValueRegex.Replace(
            value,
            match => $"\"{match.Groups["key"].Value}\":\"<redacted>\""
        );
        sanitized = SensitiveKeyValueRegex.Replace(
            sanitized,
            match => $"{match.Groups["key"].Value}=<redacted>"
        );
        return BearerTokenRegex.Replace(sanitized, "Bearer <redacted>");
    }
}
