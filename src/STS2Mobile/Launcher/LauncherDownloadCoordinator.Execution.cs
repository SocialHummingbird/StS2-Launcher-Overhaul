using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDownloadCoordinator
{
    private async Task DownloadAsync()
    {
        var branch = LauncherPreferences.ReadGameBranch();
        try
        {
            LauncherLaunchMarkers.RecordPhase(
                "game download requested",
                $"branch={branch}"
            );
            LauncherLaunchReadinessCache.Clear("game download requested");
            _view.ShowDownloadProgress("Connecting to Steam...");
            await _model.StartDownloadAsync(branch);
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("game download handler failed", ex.GetBaseException().Message);
            PatchHelper.Log($"[Launcher] Download handler failed: {ex}");
            FailDownload(new LauncherBranchOperationFailure(branch, ex.GetBaseException().Message));
        }
    }

    internal void UpdateDownloadProgress(DepotDownloader.DownloadProgress progress)
        => progress.ApplyTo(_view.SetDownloadProgress, _view.AppendLog);

    internal void CompleteDownload(string branch)
    {
        LauncherLaunchMarkers.RecordPhase(
            "game download completed",
            $"branch={branch}"
        );
        LauncherLaunchReadinessCache.Clear("game download completed");
        _refreshGameBranchOptions();
        LauncherLaunchReadiness readiness;
        try
        {
            readiness = _launch.RefreshSelectedRuntimeSlotEvidence(branch);
        }
        catch (Exception ex)
        {
            var problem = RuntimeValidationFailureMessage(branch, ex);
            LauncherLaunchMarkers.RecordPhase("game download runtime validation failed", problem);
            PatchHelper.Log($"[Launcher] {problem}");
            DownloadViewUpdate.Completed(
                filesReady: false,
                problem,
                STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)
            ).Apply(_view, _launch);
            return;
        }
        DownloadViewUpdate.Completed(
            readiness.Ready,
            readiness.ReadinessProblem,
            STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)
        ).Apply(_view, _launch);
        var integritySummary = LauncherGameFiles.BranchIntegritySummary(_model.DataDir, branch);
        if (!string.IsNullOrWhiteSpace(integritySummary))
            _view.AppendLog(integritySummary);
    }

    private static string RuntimeValidationFailureMessage(string branch, Exception exception)
    {
        var exceptionName = exception?.GetType().Name ?? "Exception";
        var message = string.IsNullOrWhiteSpace(exception?.Message)
            ? "No exception message was provided."
            : exception.Message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return $"Selected game version downloaded ({STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}), but runtime validation failed. Redownload selected version and attach a support report if it repeats. ({exceptionName}: {message})";
    }

    internal void FailDownload(LauncherBranchOperationFailure failure)
    {
        var branch = failure.Branch;
        var message = failure.Message;
        LauncherLaunchMarkers.RecordPhase("game download failed", message);
        _refreshGameBranchOptions();
        DownloadViewUpdate.Failed(
            LauncherBranchAvailabilityStatus.CompactFailureMessage(_model.DataDir, message),
            STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)
        ).Apply(_view, _launch);
    }

    internal void CancelDownload(string branch)
    {
        LauncherLaunchMarkers.RecordPhase("game download cancelled", $"branch={branch}");
        DownloadViewUpdate.Cancelled().Apply(_view, _launch);
    }

    internal void ShowDownloadReadyAction()
        => DownloadViewUpdate.Ready().Apply(_view, _launch);

    internal void ShowRedownloadSelectedVersionAction()
        => DownloadViewUpdate.Ready(RedownloadSelectedVersionButtonText).Apply(_view, _launch);
}
