using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherDownloadCoordinator
{
    internal bool TryStartAutomaticRepair(Action afterSuccess = null)
        => TryStartAutomaticRepair(
            LauncherPreferences.ReadGameBranch(),
            afterSuccess
        );

    internal bool TryStartAutomaticRepair(
        string branch,
        Action afterSuccess = null
    )
    {
        var started = false;
        RouteInstalledVersionReadiness(
            LocalPckRepairOperation.ClassifyForLauncherRouting(
                _model.DataDir,
                branch
            ),
            ready: () => { },
            automaticRepair: () =>
            {
                started = true;
                DownloadViewUpdate.AutomaticRepairStarted().Apply(_view, _launch);
                if (_automaticRepairContinuation != null
                    && _model.SelectedVersionOperationIsRunning)
                {
                    if (afterSuccess != null)
                        _automaticRepairContinuation = afterSuccess;
                    return;
                }

                _automaticRepairContinuation = afterSuccess ?? (() => { });
                _ = RunSelectedVersionOperationAsync(
                    SteamGameBranch.Normalize(branch),
                    automaticRepairOnly: true
                );
            },
            redownloadRequired: () => { }
        );
        return started;
    }

    internal bool TryShowRedownloadRequired()
        => TryShowRedownloadRequired(LauncherPreferences.ReadGameBranch());

    internal bool TryShowRedownloadRequired(string branch)
    {
        if (!LauncherGameFiles.HasBranchMetadataProblem(_model.DataDir, branch))
            return false;

        _view.HideActions();
        _view.SetStatus(
            RedownloadRequiredStatus,
            LauncherStatusSeverity.Warning
        );
        ShowRedownloadSelectedVersionAction();
        return true;
    }

    internal static void RouteInstalledVersionReadiness(
        InstalledGameVersionReadiness readiness,
        Action ready,
        Action automaticRepair,
        Action redownloadRequired
    )
    {
        if (ready == null)
            throw new ArgumentNullException(nameof(ready));
        if (automaticRepair == null)
            throw new ArgumentNullException(nameof(automaticRepair));
        if (redownloadRequired == null)
            throw new ArgumentNullException(nameof(redownloadRequired));

        switch (readiness)
        {
            case InstalledGameVersionReadiness.Ready:
                ready();
                return;
            case InstalledGameVersionReadiness.AutomaticRepair:
                automaticRepair();
                return;
            case InstalledGameVersionReadiness.RedownloadRequired:
                redownloadRequired();
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(readiness),
                    readiness,
                    "Unknown installed-version readiness."
                );
        }
    }

    private async Task DownloadAsync()
        => await RunSelectedVersionOperationAsync(
            LauncherPreferences.ReadGameBranch(),
            automaticRepairOnly: false
        );

    private async Task RunSelectedVersionOperationAsync(
        string branch,
        bool automaticRepairOnly
    )
    {
        try
        {
            LauncherLaunchMarkers.RecordPhase(
                automaticRepairOnly
                    ? "automatic PCK repair requested"
                    : "game download requested",
                $"branch={branch}; localRepairOnly={automaticRepairOnly}"
            );
            LauncherLaunchReadinessCache.Clear(
                automaticRepairOnly
                    ? "automatic PCK repair requested"
                    : "game download requested"
            );
            if (automaticRepairOnly)
            {
                await _model.StartAutomaticRepairAsync(branch);
            }
            else
            {
                _view.ShowDownloadProgress("Connecting to Steam...");
                await _model.StartDownloadAsync(branch);
            }
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase(
                automaticRepairOnly
                    ? "automatic PCK repair handler failed"
                    : "game download handler failed",
                ex.GetBaseException().Message
            );
            PatchHelper.Log(
                automaticRepairOnly
                    ? $"[Launcher] Automatic PCK repair handler failed: {ex}"
                    : $"[Launcher] Download handler failed: {ex}"
            );
            FailDownload(new LauncherBranchOperationFailure(branch, ex.GetBaseException().Message));
        }
    }

    internal void UpdateDownloadProgress(DepotDownloader.DownloadProgress progress)
        => progress.ApplyTo(_view.SetDownloadProgress, _view.AppendLog);

    internal void CompleteDownload(BranchInstallCompletion completion)
    {
        if (completion == null)
            throw new ArgumentNullException(nameof(completion));
        var automaticRepairContinuation = _automaticRepairContinuation;
        _automaticRepairContinuation = null;
        var branch = completion.Branch;
        LauncherLaunchMarkers.RecordPhase(
            "game download completed",
            $"branch={branch}; gameIdentity={completion.GameIdentity.Id}; transaction={completion.TransactionId:D}"
        );
        LauncherLaunchReadinessCache.Clear("game download completed");
        _refreshGameBranchOptions();
        LauncherLaunchReadiness readiness;
        try
        {
            readiness = _launch.RefreshSelectedRuntimeSlotEvidence(completion);
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
        if (readiness.Ready)
            automaticRepairContinuation?.Invoke();
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
        _automaticRepairContinuation = null;
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
        _automaticRepairContinuation = null;
        LauncherLaunchMarkers.RecordPhase("game download cancelled", $"branch={branch}");
        DownloadViewUpdate.Cancelled().Apply(_view, _launch);
    }

    internal void ShowDownloadReadyAction()
        => DownloadViewUpdate.Ready().Apply(_view, _launch);

    internal void ShowRedownloadSelectedVersionAction()
        => DownloadViewUpdate.Ready(RedownloadSelectedVersionButtonText).Apply(_view, _launch);
}
