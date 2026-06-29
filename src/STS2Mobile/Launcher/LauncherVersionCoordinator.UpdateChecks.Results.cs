namespace STS2Mobile.Launcher;

internal sealed partial class LauncherVersionCoordinator
{
    internal void CompleteUpdateCheck(bool hasUpdate)
    {
        RefreshGameBranchOptions();
        UpdateCheckViewUpdate.Completed(hasUpdate).Apply(_view);
    }

    internal void FailUpdateCheck(string message)
    {
        RefreshGameBranchOptions();
        UpdateCheckViewUpdate.Failed(
            LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, message)
        ).Apply(_view);
    }
}
