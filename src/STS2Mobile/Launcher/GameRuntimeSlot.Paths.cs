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

    private static string FindSourceAssemblyPath(string gameDirectory)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            return Path.Combine(gameDirectory ?? string.Empty, "data_sts2_windows_x86_64", GameAssemblyFileName);

        foreach (var directory in Directory.EnumerateDirectories(gameDirectory, "data_*", SearchOption.TopDirectoryOnly))
        {
            var candidate = Path.Combine(directory, GameAssemblyFileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(gameDirectory, "data_sts2_windows_x86_64", GameAssemblyFileName);
    }

    private static string FindActiveAndroidAssemblyPath(string dataDir)
    {
        var publishRoot = Path.Combine(dataDir, ".godot", "mono", "publish");
        if (Directory.Exists(publishRoot))
        {
            foreach (var directory in Directory.EnumerateDirectories(publishRoot, "*", SearchOption.TopDirectoryOnly))
            {
                var candidate = Path.Combine(directory, GameAssemblyFileName);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return Path.Combine(publishRoot, "arm64", GameAssemblyFileName);
    }
}