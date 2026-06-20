using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static void DeleteDownloadedState(string dataDir)
        => DeleteDownloadedState(dataDir, LauncherPreferences.ReadGameBranch());

    internal static void DeleteDownloadedState(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        var gameDirectory = GameDirectoryPath(dataDir, branch);
        var downloadStateDirectory = SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch);
        var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch);
        var gameDirectoryExisted = Directory.Exists(gameDirectory);
        var downloadStateDirectoryExisted = Directory.Exists(downloadStateDirectory);
        var runtimePackDirectoryExisted = Directory.Exists(runtimePackDirectory);
        WriteRedownloadMarker(
            dataDir,
            branch,
            gameDirectory,
            gameDirectoryExisted,
            null,
            downloadStateDirectory,
            downloadStateDirectoryExisted,
            null,
            runtimePackDirectory,
            runtimePackDirectoryExisted,
            null
        );
        DeleteDirectory(gameDirectory);
        DeleteDirectory(downloadStateDirectory);
        DeleteDirectory(runtimePackDirectory);
        LauncherRuntimeSlotEvidence.Clear(dataDir);
        LauncherRuntimeCacheEvidence.Clear(dataDir);
        LauncherRuntimePatchValidationEvidence.Clear(dataDir);
        WriteRedownloadMarker(
            dataDir,
            branch,
            gameDirectory,
            gameDirectoryExisted,
            Directory.Exists(gameDirectory),
            downloadStateDirectory,
            downloadStateDirectoryExisted,
            Directory.Exists(downloadStateDirectory),
            runtimePackDirectory,
            runtimePackDirectoryExisted,
            Directory.Exists(runtimePackDirectory)
        );
        LauncherLaunchMarkers.ClearStartupMarker();
        PatchHelper.Log($"[Launcher] Deleted downloaded game files and download state for branch '{branch}'");
    }
}
