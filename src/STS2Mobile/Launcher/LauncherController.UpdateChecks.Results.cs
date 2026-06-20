namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void CompleteUpdateCheck(bool hasUpdate)
    {
        RefreshGameBranchOptions();
        UpdateCheckViewUpdate.Completed(hasUpdate).Apply(_view);
    }

    private void FailUpdateCheck(string message)
    {
        RefreshGameBranchOptions();
        UpdateCheckViewUpdate.Failed(
            LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, message)
        ).Apply(_view);
    }
}
