using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string UpdateCheckFailedButtonText = "CHECK FAILED";
    private const string UpToDateButtonText = "UP TO DATE";
    private const string UpdateGameFilesButtonText = "UPDATE GAME FILES";
    private const string UpdateAvailableStatus = "Update available!";

    private bool _updateCheckRunning;

    private void RunUpdateCheck()
        => _ = RunUpdateCheckAsync();

    private async Task RunUpdateCheckAsync()
    {
        if (_updateCheckRunning)
            return;

        _updateCheckRunning = true;
        SetUpdateCheckBusy(busy: true);

        try
        {
            await CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Check for updates failed: {ex}");
            ShowUpdateCheckFailed(ex.Message);
        }
        finally
        {
            _updateCheckRunning = false;
            SetUpdateCheckBusy(busy: false);
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        // Check for launcher (APK) updates from GitHub in parallel with game file updates.
        var appUpdateTask = CheckForAppUpdatesAsync();
        await _model.CheckForUpdatesAsync();
        await appUpdateTask;
    }

    private void SetUpdateCheckBusy(bool busy)
        => _view.SetUpdateCheckBusy(busy);

    private void ShowUpdateCheckFailed(string message)
    {
        _view.AppendLog($"Update check failed: {message}");
        _view.SetUpdateButtonText(UpdateCheckFailedButtonText);
    }

    private void CompleteUpdateCheck(bool hasUpdate)
    {
        if (hasUpdate)
        {
            _view.HideActions();
            _view.ShowDownloadAction(UpdateGameFilesButtonText);
            _view.SetStatus(UpdateAvailableStatus);
            return;
        }

        _view.SetUpdateButtonText(UpToDateButtonText);
    }

    private void FailUpdateCheck(string message)
    {
        ShowUpdateCheckFailed(message);
    }
}
