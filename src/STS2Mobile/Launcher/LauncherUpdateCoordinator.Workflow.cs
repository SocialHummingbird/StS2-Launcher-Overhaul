using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUpdateCoordinator
{
    private async Task CheckForUpdatesAsync()
    {
        // Check for launcher (APK) updates from GitHub in parallel with game file updates.
        var appUpdateTask = CheckForAppUpdatesAsync();
        var selectedBranch = LauncherPreferences.ReadGameBranch();
        var updateProblem = LauncherBranchCatalog.SelectedOptionDownloadProblem(
            selectedBranch,
            LauncherBranchCatalog.ReadVisibleBranches(_model.DataDir)
        );

        if (!string.IsNullOrWhiteSpace(updateProblem))
        {
            UpdateCheckViewUpdate.Blocked(
                updateProblem.Replace("Download blocked:", "Update check blocked:")
            ).Apply(_view);
        }
        else
        {
            await _model.CheckForUpdatesAsync();
        }

        await appUpdateTask;
    }
}
