using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static string BuildRuntimePackManifestPath(string dataDir, string branch)
        => Path.Combine(
            RuntimePackDirectoryPath(dataDir, branch),
            CompatibilityManifestFileName
        );

    internal static string RuntimePackDirectoryPath(string dataDir, string branch)
        => Path.Combine(
            dataDir,
            RuntimePacksDirectory,
            SteamGameBranch.StateDirectoryName(branch)
        );

    internal static string FindSourceAssemblyPath(string gameDirectory)
    {
        var expectedPath = Path.Combine(
            gameDirectory ?? string.Empty,
            "data_sts2_windows_x86_64",
            GameAssemblyFileName
        );
        if (File.Exists(expectedPath) || string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            return expectedPath;

        foreach (var directory in Directory.EnumerateDirectories(gameDirectory, "data_*", SearchOption.TopDirectoryOnly))
        {
            var candidate = Path.Combine(directory, GameAssemblyFileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return expectedPath;
    }

    internal static string FindActiveAndroidAssemblyPath(string dataDir)
    {
        var publishRoot = Path.Combine(dataDir, ".godot", "mono", "publish");
        var expectedPath = Path.Combine(publishRoot, "arm64", GameAssemblyFileName);
        if (File.Exists(expectedPath) || !Directory.Exists(publishRoot))
            return expectedPath;

        if (Directory.Exists(publishRoot))
        {
            foreach (var directory in Directory.EnumerateDirectories(publishRoot, "*", SearchOption.TopDirectoryOnly))
            {
                var candidate = Path.Combine(directory, GameAssemblyFileName);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return expectedPath;
    }
}
