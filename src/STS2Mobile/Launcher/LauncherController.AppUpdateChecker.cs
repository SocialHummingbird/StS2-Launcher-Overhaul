using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SteamKit2;
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
    private static readonly TimeSpan LauncherUpdateTimeout = TimeSpan.FromSeconds(15);

    private static async Task<string?> CheckLatestLauncherVersionAsync()
    {
        var currentVersion = GetInstalledLauncherVersion();
        if (currentVersion == null)
            return null;

        using var http = OperatingSystem.IsAndroid()
            ? AndroidJavaHttpMessageHandler.CreateClient(HttpClientPurpose.CDN)
            : new HttpClient { Timeout = LauncherUpdateTimeout };
        http.Timeout = LauncherUpdateTimeout;
        http.DefaultRequestHeaders.Add("User-Agent", LauncherUpdateUserAgent);

        var response = await http.GetStringAsync(LatestLauncherReleaseApiUrl).ConfigureAwait(false);
        var release = ParseReleaseMetadata(response);
        if (release.Name == null)
            return null;

        var latestVersion = NormalizeVersion(release.Tag ?? release.Name);
        var installedVersion = NormalizeVersion(currentVersion);

        if (latestVersion == null || installedVersion == null)
            return null;

        if (CompareVersions(latestVersion, installedVersion) <= 0)
            return null;

        return latestVersion;
    }

    private static string? GetInstalledLauncherVersion()
    {
        try
        {
            if (!AndroidGodotAppBridge.TryGetInstance(out var godotApp))
                return null;

            return (string)godotApp.Call("getVersionName");
        }
        catch
        {
            return null;
        }
    }

    private static (string? Name, string? Tag) ParseReleaseMetadata(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var releaseName = root.TryGetProperty(ReleaseNameProperty, out var nameProp)
            ? nameProp.GetString()
            : null;
        var releaseTag = root.TryGetProperty(ReleaseTagNameProperty, out var tagProp)
            ? tagProp.GetString()
            : null;

        return (Name: releaseName, Tag: releaseTag);
    }

    private static string? NormalizeVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            return null;

        var match = Regex.Match(version, @"\d+(?:\.\d+)*(?:\.\d+)*");
        return match.Success ? match.Value.TrimStart('v', 'V') : null;
    }

    private static int CompareVersions(string a, string b)
    {
        var aParts = a.Split('.');
        var bParts = b.Split('.');
        var len = Math.Max(aParts.Length, bParts.Length);

        for (int i = 0; i < len; i++)
        {
            int aVal = i < aParts.Length && int.TryParse(aParts[i], out var av) ? av : 0;
            int bVal = i < bParts.Length && int.TryParse(bParts[i], out var bv) ? bv : 0;
            if (aVal != bVal)
                return aVal - bVal;
        }

        return 0;
    }
}
