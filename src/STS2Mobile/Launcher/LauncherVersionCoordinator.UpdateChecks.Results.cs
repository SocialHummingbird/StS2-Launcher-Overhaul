namespace STS2Mobile.Launcher;

internal sealed partial class LauncherVersionCoordinator
{
    internal void CompleteUpdateCheck(LauncherUpdateCheckResult result)
    {
        RefreshGameBranchOptions();
        if (!IsCurrentSelection(result.Branch))
        {
            _view.SetVersionPrimaryAction(
                LauncherVersionPrimaryAction.CheckForUpdates
            );
            _view.AppendLog(
                "Ignored an update result for a game version that is no longer selected."
            );
            return;
        }
        UpdateCheckViewUpdate.Completed(
            result.HasUpdate,
            STS2Mobile.Steam.SteamGameBranch.DisplayName(result.Branch)
        ).Apply(_view);
    }

    internal void FailUpdateCheck(LauncherBranchOperationFailure failure)
    {
        RefreshGameBranchOptions();
        if (!IsCurrentSelection(failure.Branch))
        {
            _view.SetVersionPrimaryAction(
                LauncherVersionPrimaryAction.CheckForUpdates
            );
            return;
        }
        UpdateCheckViewUpdate.Failed(
            LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, failure.Message),
            STS2Mobile.Steam.SteamGameBranch.DisplayName(failure.Branch)
        ).Apply(_view);
    }

    private static bool IsCurrentSelection(string branch)
        => string.Equals(
            STS2Mobile.Steam.SteamGameBranch.Normalize(branch),
            LauncherPreferences.ReadGameBranch(),
            System.StringComparison.OrdinalIgnoreCase
        );
}
