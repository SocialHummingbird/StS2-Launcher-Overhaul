using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _updateCheckRunning;

    private void RunUpdateCheck()
        => _ = RunUpdateCheckAsync();

    private async Task RunUpdateCheckAsync()
    {
        if (_updateCheckRunning)
            return;

        _updateCheckRunning = true;
        SetUpdateCheckBusy(true);

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
            SetUpdateCheckBusy(false);
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
    {
        _view.Actions.SetUpdateButtonDisabled(busy);
        if (busy)
            _view.Actions.SetUpdateButtonText("Checking...");
    }

    private void ShowUpdateCheckFailed(string message)
    {
        _view.AppendLog($"Update check failed: {message}");
        _view.Actions.SetUpdateButtonText("CHECK FAILED");
    }

    private void CompleteUpdateCheck(bool hasUpdate)
    {
        if (hasUpdate)
        {
            _view.Actions.HideAll();
            ShowDownloadAction("UPDATE GAME FILES");
            _view.SetStatus("Update available!");
        }
        else
        {
            _view.Actions.SetUpdateButtonText("UP TO DATE");
        }
    }

    private void FailUpdateCheck(string message)
    {
        ShowUpdateCheckFailed(message);
    }
}
