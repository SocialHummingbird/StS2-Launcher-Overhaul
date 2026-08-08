using System;
using System.IO;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimeSlotMetadata
{
    private static (string Version, string Commit, string BuildId) ReadReleaseInfo(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return ("<missing>", "<missing>", "<missing>");

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            return (
                ReadString(root, "version", "Version", "gameVersion", "game_version", "releaseVersion", "release_version"),
                ReadString(root, "commit", "Commit", "gitCommit", "git_commit", "sha", "revision"),
                ReadString(root, "buildId", "build_id", "build", "Build", "steamBuildId", "steam_build_id")
            );
        }
        catch (Exception ex)
        {
            return ($"<unreadable:{ex.GetType().Name}>", "<unreadable>", "<unreadable>");
        }
    }

    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.String)
                    return value.GetString() ?? string.Empty;
                if (value.ValueKind == JsonValueKind.Number || value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                    return value.ToString();
            }
        }

        return "<missing>";
    }
}
