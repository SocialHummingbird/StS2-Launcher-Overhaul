using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static void DeleteDownloadedState(string dataDir)
        => DeleteDownloadedState(dataDir, LauncherPreferences.ReadGameBranch());

    internal static void DeleteDownloadedState(string dataDir, string branch)
    {
        branch = SteamGameBranch.StorageIdentity(branch);
        var result = SelectedBranchRecovery.Execute(
            dataDir,
            branch,
            SelectedBranchRecoveryMode.FullRedownload
        );
        result.RequireSuccess();
        PatchHelper.Log(
            $"[Launcher] Cleared selected branch '{branch}' for redownload ({result.RemovedArtifactCount} owned artifact(s))."
        );
    }
}
