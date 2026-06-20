using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private async Task DownloadAsync()
    {
        try
        {
            _view.ShowDownloadProgress("Connecting to Steam...");
            await _model.StartDownloadAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Download handler failed: {ex}");
            FailDownload(ex.GetBaseException().Message);
        }
    }

    private void UpdateDownloadProgress(DepotDownloader.DownloadProgress progress)
        => progress.ApplyTo(_view.SetDownloadProgress, _view.AppendLog);

    private void CompleteDownload()
    {
        RefreshGameBranchOptions();
        var branch = LauncherPreferences.ReadGameBranch();
        if (LauncherGameFiles.DownloadedForValidation(_model.DataDir, branch))
            PatchCompatibilityValidator.ValidateSelectedVersion(_model.DataDir, branch);
        var filesReady = LauncherGameFiles.Ready(_model.DataDir, branch);
        var readinessProblem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch);
        LauncherRuntimeSlotEvidence.Write(_model.DataDir, branch, filesReady, readinessProblem);
        DownloadViewUpdate.Completed(
            filesReady,
            readinessProblem
        ).Apply(this);
        var integritySummary = LauncherGameFiles.BranchIntegritySummary(_model.DataDir, branch);
        if (!string.IsNullOrWhiteSpace(integritySummary))
            _view.AppendLog(integritySummary);
    }

    private void FailDownload(string message)
    {
        RefreshGameBranchOptions();
        DownloadViewUpdate.Failed(
            LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, message)
        ).Apply(this);
    }

    private void CancelDownload()
        => DownloadViewUpdate.Cancelled().Apply(this);

    private void ShowDownloadReadyAction()
        => DownloadViewUpdate.Ready().Apply(this);

    private void ShowRedownloadSelectedVersionAction()
        => DownloadViewUpdate.Ready(RedownloadSelectedVersionButtonText).Apply(this);
}
