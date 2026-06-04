using System;
using System.Net.Http;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string LatestLauncherReleaseApiUrl =
        "https://api.github.com/repos/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest";
    private const string LauncherRepoReleasesPage =
        "https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest";
    private const string LauncherUpdateUserAgent = "StS2-Launcher";
    private const string ReleaseNameProperty = "name";
    private const string ReleaseTagNameProperty = "tag_name";
    private const string VersionNumberPattern = @"\d+(?:\.\d+)*";
    private static readonly TimeSpan LauncherUpdateTimeout = TimeSpan.FromSeconds(15);

    private sealed class LauncherUpdateCheck
    {
        private readonly LauncherVersion _installedVersion;

        private LauncherUpdateCheck(LauncherVersion installedVersion)
        {
            _installedVersion = installedVersion;
        }

        internal static LauncherUpdateCheck ForInstalledVersion(
            LauncherVersion installedVersion
        )
            => new(installedVersion);

        internal async Task<string> RunAsync()
        {
            var latestVersion = await FetchLatestVersionAsync();
            if (!latestVersion.HasValue)
                return null;

            if (!latestVersion.Value.IsNewerThan(_installedVersion))
                return null;

            return latestVersion.Value.ToString();
        }

        private static async Task<LauncherVersion?> FetchLatestVersionAsync()
        {
            using var http = CreateHttpClient();
            var response = await http
                .GetStringAsync(LatestLauncherReleaseApiUrl)
                .ConfigureAwait(false);

            return ParseLatestReleaseVersion(response);
        }

        private static HttpClient CreateHttpClient()
        {
            var http = OperatingSystem.IsAndroid()
                ? AndroidJavaHttpMessageHandler.CreateCdnClient()
                : new HttpClient { Timeout = LauncherUpdateTimeout };
            http.Timeout = LauncherUpdateTimeout;
            http.DefaultRequestHeaders.Add("User-Agent", LauncherUpdateUserAgent);
            return http;
        }
    }

    private static async Task<string> CheckLatestLauncherVersionAsync()
    {
        if (!LauncherVersion.TryParse(GetInstalledLauncherVersion(), out var installedVersion))
            return null;

        return await LauncherUpdateCheck
            .ForInstalledVersion(installedVersion)
            .RunAsync();
    }

    private static string GetInstalledLauncherVersion()
    {
        try
        {
            return AndroidGodotAppBridge.GetVersionName();
        }
        catch
        {
            return null;
        }
    }
}
