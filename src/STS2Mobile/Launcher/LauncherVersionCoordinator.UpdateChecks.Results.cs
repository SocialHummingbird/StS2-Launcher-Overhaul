namespace STS2Mobile.Launcher;

internal sealed partial class LauncherVersionCoordinator
{
    internal void CompleteUpdateCheck(LauncherUpdateCheckResult result)
    {
        RefreshGameBranchOptions();
        UpdateCheckViewUpdate.Completed(
            result.HasUpdate,
            STS2Mobile.Steam.SteamGameBranch.DisplayName(result.Branch)
        ).Apply(_view);
    }

    internal void FailUpdateCheck(LauncherBranchOperationFailure failure)
    {
        RefreshGameBranchOptions();
        UpdateCheckViewUpdate.Failed(
            LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, failure.Message),
            STS2Mobile.Steam.SteamGameBranch.DisplayName(failure.Branch)
        ).Apply(_view);
    }
}
