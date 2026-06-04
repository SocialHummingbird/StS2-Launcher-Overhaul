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

        internal async Task<string?> RunAsync()
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
                ? AndroidJavaHttpMessageHandler.CreateClient(HttpClientPurpose.CDN)
                : new HttpClient { Timeout = LauncherUpdateTimeout };
            http.Timeout = LauncherUpdateTimeout;
            http.DefaultRequestHeaders.Add("User-Agent", LauncherUpdateUserAgent);
            return http;
        }
    }

    private readonly struct LauncherVersion
    {
        private LauncherVersion(string text, int[] parts)
        {
            Text = text;
            Parts = parts;
        }

        private string Text { get; }
        private int[] Parts { get; }

        internal static bool TryParse(string? version, out LauncherVersion parsed)
        {
            parsed = default;
            if (string.IsNullOrEmpty(version))
                return false;

            var match = Regex.Match(version, VersionNumberPattern);
            if (!match.Success)
                return false;

            var text = match.Value.TrimStart('v', 'V');
            parsed = new LauncherVersion(text, ParseParts(text));
            return true;
        }

        internal bool IsNewerThan(LauncherVersion other)
            => CompareTo(other) > 0;

        private int CompareTo(LauncherVersion other)
        {
            var len = Math.Max(Parts.Length, other.Parts.Length);
            for (var i = 0; i < len; i++)
            {
                var current = PartOrZero(Parts, i);
                var target = PartOrZero(other.Parts, i);
                if (current != target)
                    return current - target;
            }

            return 0;
        }

        public override string ToString()
            => Text;

        private static int[] ParseParts(string version)
        {
            var textParts = version.Split('.');
            var parts = new int[textParts.Length];
            for (var i = 0; i < textParts.Length; i++)
                parts[i] = int.TryParse(textParts[i], out var value) ? value : 0;

            return parts;
        }

        private static int PartOrZero(int[] parts, int index)
            => index < parts.Length ? parts[index] : 0;
    }

    private static async Task<string?> CheckLatestLauncherVersionAsync()
    {
        if (!LauncherVersion.TryParse(GetInstalledLauncherVersion(), out var installedVersion))
            return null;

        return await LauncherUpdateCheck
            .ForInstalledVersion(installedVersion)
            .RunAsync();
    }

    private static string? GetInstalledLauncherVersion()
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

    private static LauncherVersion? ParseLatestReleaseVersion(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return LauncherVersion.TryParse(ReadReleaseVersionText(doc.RootElement), out var version)
            ? version
            : null;
    }

    private static string? ReadReleaseVersionText(JsonElement root)
        => ReadOptionalString(root, ReleaseTagNameProperty)
            ?? ReadOptionalString(root, ReleaseNameProperty);

    private static string? ReadOptionalString(JsonElement root, string property)
        => root.TryGetProperty(property, out var value)
            ? value.GetString()
            : null;
}
