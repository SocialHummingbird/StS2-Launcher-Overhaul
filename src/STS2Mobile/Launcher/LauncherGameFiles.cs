using System.IO;
using Godot;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static string PckPath(string dataDir) =>
        PckPath(dataDir, LauncherPreferences.ReadGameBranch());

    internal static string PckPath(string dataDir, string branch) =>
        Path.Combine(GameDirectoryPath(dataDir, branch), LauncherStorageNames.GamePck);

    internal static string GameDirectoryPath(string dataDir, string branch) =>
        SteamGameInstallPaths.GameDirectory(dataDir, branch);

    internal static bool Ready() => Ready(OS.GetDataDir());
}
