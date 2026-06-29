using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDownloadCoordinator
{
    private async Task DownloadAsync()
    {
        try
        {
            LauncherLaunchMarkers.RecordPhase(
                "game download requested",
                $"branch={LauncherPreferences.ReadGameBranch()}"
            );
            _view.ShowDownloadProgress("Connecting to Steam...");
            await _model.StartDownloadAsync();
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("game download handler failed", ex.GetBaseException().Message);
            PatchHelper.Log($"[Launcher] Download handler failed: {ex}");
            FailDownload(ex.GetBaseException().Message);
        }
    }

    internal void UpdateDownloadProgress(DepotDownloader.DownloadProgress progress)
        => progress.ApplyTo(_view.SetDownloadProgress, _view.AppendLog);

    internal void CompleteDownload()
    {
        LauncherLaunchMarkers.RecordPhase(
            "game download completed",
            $"branch={LauncherPreferences.ReadGameBranch()}"
        );
        _refreshGameBranchOptions();
        var branch = LauncherPreferences.ReadGameBranch();
        if (LauncherGameFiles.DownloadedForValidation(_model.DataDir, branch))
            PatchCompatibilityValidator.ValidateSelectedVersion(_model.DataDir, branch);
        var filesReady = LauncherGameFiles.Ready(_model.DataDir, branch);
        var readinessProblem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch);
        LauncherRuntimeSlotEvidence.Write(_model.DataDir, branch, filesReady, readinessProblem);
        DownloadViewUpdate.Completed(filesReady, readinessProblem).Apply(_view, _launch);
        var integritySummary = LauncherGameFiles.BranchIntegritySummary(_model.DataDir, branch);
        if (!string.IsNullOrWhiteSpace(integritySummary))
            _view.AppendLog(integritySummary);
    }

    internal void FailDownload(string message)
    {
        LauncherLaunchMarkers.RecordPhase("game download failed", message);
        _refreshGameBranchOptions();
        DownloadViewUpdate.Failed(
            LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, message)
        ).Apply(_view, _launch);
    }

    internal void CancelDownload()
    {
        LauncherLaunchMarkers.RecordPhase("game download cancelled");
        DownloadViewUpdate.Cancelled().Apply(_view, _launch);
    }

    internal void ShowDownloadReadyAction()
        => DownloadViewUpdate.Ready().Apply(_view, _launch);

    internal void ShowRedownloadSelectedVersionAction()
        => DownloadViewUpdate.Ready(RedownloadSelectedVersionButtonText).Apply(_view, _launch);
}
