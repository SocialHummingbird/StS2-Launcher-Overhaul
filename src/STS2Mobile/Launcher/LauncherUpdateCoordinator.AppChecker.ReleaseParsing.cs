using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUpdateCoordinator
{
    private static LauncherVersion? ParseLatestReleaseVersion(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return LauncherVersion.TryParse(ReadReleaseVersionText(doc.RootElement), out var version)
            ? version
            : null;
    }

    private static string ReadReleaseVersionText(JsonElement root)
        => ReadOptionalString(root, ReleaseTagNameProperty)
            ?? ReadOptionalString(root, ReleaseNameProperty);

    private static string ReadOptionalString(JsonElement root, string property)
        => root.TryGetProperty(property, out var value)
            ? value.GetString()
            : null;
}
