using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string UpdateCheckFailedButtonText = "CHECK FAILED";
    private const string UpdateCheckBlockedButtonText = "CHECK BLOCKED";
    private const string UpToDateButtonText = "UP TO DATE";
    private const string UpdateGameFilesButtonText = "UPDATE SELECTED VERSION";

    private bool _updateCheckRunning;

    private readonly struct UpdateCheckViewUpdate
    {
        private UpdateCheckViewUpdate(
            string logMessage = null,
            string updateButtonText = null,
            string downloadButtonText = null,
            string status = null,
            bool hideActions = false
        )
        {
            LogMessage = logMessage;
            UpdateButtonText = updateButtonText;
            DownloadButtonText = downloadButtonText;
            Status = status;
            HideActions = hideActions;
        }

        private string LogMessage { get; }
        private string UpdateButtonText { get; }
        private string DownloadButtonText { get; }
        private string Status { get; }
        private bool HideActions { get; }

        internal static UpdateCheckViewUpdate Completed(bool hasUpdate)
            => hasUpdate
                ? new(
                    downloadButtonText: UpdateGameFilesButtonText,
                    status: $"Update available for selected game version ({SelectedGameVersionName()}).",
                    hideActions: true
                )
                : new(
                    logMessage: $"Selected game version is up to date ({SelectedGameVersionName()}).",
                    updateButtonText: UpToDateButtonText
                );

        internal static UpdateCheckViewUpdate Failed(string message)
            => new(
                logMessage: $"Update check failed for selected game version ({SelectedGameVersionName()}): {message}",
                updateButtonText: UpdateCheckFailedButtonText,
                status: $"Update check failed for selected game version ({SelectedGameVersionName()}): {message}"
            );

        internal static UpdateCheckViewUpdate Blocked(string message)
            => new(
                logMessage: $"Update check blocked for selected game version ({SelectedGameVersionName()}): {message}",
                updateButtonText: UpdateCheckBlockedButtonText,
                status: $"Update check blocked for selected game version ({SelectedGameVersionName()}): {message}"
            );

        internal void Apply(LauncherView view)
        {
            if (LogMessage != null)
                view.AppendLog(LogMessage);

            if (HideActions)
                view.HideActions();

            if (DownloadButtonText != null)
                view.ShowDownloadAction(DownloadButtonText);

            if (Status != null)
                view.SetStatus(Status);

            if (UpdateButtonText != null)
                view.SetUpdateButtonText(UpdateButtonText);
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
            FailUpdateCheck(ex.Message);
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

    private void SetUpdateCheckBusy(bool busy)
        => _view.SetUpdateCheckBusy(busy);

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
