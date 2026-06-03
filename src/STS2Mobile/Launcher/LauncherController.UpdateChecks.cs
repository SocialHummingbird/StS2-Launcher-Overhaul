using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private bool _updateCheckRunning;

    private readonly struct UpdateCheckUi
    {
        private const string CheckFailedButtonText = "CHECK FAILED";
        private const string UpToDateButtonText = "UP TO DATE";
        private const string UpdateGameFilesButtonText = "UPDATE GAME FILES";
        private const string UpdateAvailableStatus = "Update available!";

        internal static UpdateCheckUi Create()
            => new();

        internal void SetBusy(LauncherView view, bool busy)
            => view.SetUpdateCheckBusy(busy);

        internal void ShowFailed(LauncherView view, string message)
        {
            view.AppendLog($"Update check failed: {message}");
            view.SetUpdateButtonText(CheckFailedButtonText);
        }

        internal void ShowCompleted(LauncherView view, bool hasUpdate)
        {
            if (hasUpdate)
            {
                view.HideActions();
                view.ShowDownloadAction(UpdateGameFilesButtonText);
                view.SetStatus(UpdateAvailableStatus);
                return;
            }

            view.SetUpdateButtonText(UpToDateButtonText);
        }
    }

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
        => UpdateCheckUi.Create().SetBusy(_view, busy);

    private void ShowUpdateCheckFailed(string message)
        => UpdateCheckUi.Create().ShowFailed(_view, message);

    private void CompleteUpdateCheck(bool hasUpdate)
        => UpdateCheckUi.Create().ShowCompleted(_view, hasUpdate);

    private void FailUpdateCheck(string message)
    {
        ShowUpdateCheckFailed(message);
    }
}
