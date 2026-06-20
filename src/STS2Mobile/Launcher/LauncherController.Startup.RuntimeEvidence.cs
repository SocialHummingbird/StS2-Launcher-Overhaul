namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool RefreshSelectedRuntimeAndCheckReady()
    {
        RefreshSelectedRuntimeSlotEvidence();
        return LauncherGameFiles.Ready(_model.DataDir, LauncherPreferences.ReadGameBranch());
    }

    private string SelectedVersionReadyStatus()
    {
        RefreshSelectedRuntimeSlotEvidence();
        return SelectedVersionReadyStatus(_model.LoggedInStatus());
    }

    private string SelectedVersionReadyStatus(string baseStatus)
    {
        var branch = LauncherPreferences.ReadGameBranch();
        return $"{baseStatus} Selected game version: {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Active install slot: {STS2Mobile.Steam.SteamGameInstallPaths.VersionSlotKind(branch)}. Runtime pairing is verified when launching.";
    }

    private string SelectedVersionDownloadRequiredStatus()
    {
        var branch = LauncherPreferences.ReadGameBranch();
        return $"{_model.LoggedInStatus()} Download selected game version: {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Active install slot: {STS2Mobile.Steam.SteamGameInstallPaths.VersionSlotKind(branch)}.";
    }

    private void RefreshSelectedRuntimeSlotEvidence()
    {
        var branch = LauncherPreferences.ReadGameBranch();
        if (LauncherGameFiles.DownloadedForValidation(_model.DataDir, branch))
            PatchCompatibilityValidator.ValidateSelectedVersion(_model.DataDir, branch);

        var filesReady = LauncherGameFiles.Ready(_model.DataDir, branch);
        var readinessProblem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch);
        LauncherRuntimeSlotEvidence.Write(_model.DataDir, branch, filesReady, readinessProblem);
    }
}
