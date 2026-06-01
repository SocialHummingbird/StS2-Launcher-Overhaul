using System.IO;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static string PckPath(string dataDir) =>
        Path.Combine(dataDir, LauncherStorageNames.GameDirectory, LauncherStorageNames.GamePck);

    internal static bool Ready() => Ready(OS.GetDataDir());

    internal static bool Ready(string dataDir)
        => IsValidPck(PckPath(dataDir));

    internal static void DeleteDownloadedState(string dataDir)
    {
        DeleteDirectory(Path.Combine(dataDir, LauncherStorageNames.GameDirectory));
        DeleteDirectory(Path.Combine(dataDir, LauncherStorageNames.DownloadStateDirectory));
        LauncherLaunchMarkers.ClearStartupMarker();
        PatchHelper.Log("[Launcher] Deleted downloaded game files and download state");
    }

}
