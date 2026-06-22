using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private async Task RunAutomationAsync(LauncherAutomationRequest request)
    {
        WriteAutomationMarker(request, "started");
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Branch))
            {
                LauncherPreferences.SaveGameBranch(request.Branch);
                LauncherBranchAvailabilityStatus.Clear(_model.DataDir);
                RefreshGameBranchOptions();
            }

            _runOnMainThread(() =>
                _view.AppendLog($"[Automation] Running {request.Action} for {SteamGameBranch.DisplayName(LauncherPreferences.ReadGameBranch())}.")
            );

            if (request.RefreshCatalog)
            {
                _runOnMainThread(() => _view.SetRefreshGameVersionsBusy(true));
                await _model.RefreshBranchCatalogAsync().ConfigureAwait(false);
                _runOnMainThread(RefreshGameBranchOptions);
                _runOnMainThread(() => _view.SetRefreshGameVersionsBusy(false));
            }

            if (request.CheckUpdates)
                await _model.CheckForUpdatesAsync().ConfigureAwait(false);

            if (request.Redownload)
            {
                _model.ResetGameFilesForRedownload();
                _runOnMainThread(() =>
                    _view.AppendLog("[Automation] Selected game version cache cleared before replacement download.")
                );
            }

            if (request.Download)
                await _model.StartDownloadAsync().ConfigureAwait(false);

            if (request.WorkshopClear)
            {
                _runOnMainThread(() =>
                    _view.AppendLog("[Automation] Clearing staged Workshop mods; Steam Cloud Push is not run.")
                );
                _model.ClearWorkshopMods();
            }

            if (request.WorkshopSync)
            {
                _runOnMainThread(() =>
                    _view.AppendLog("[Automation] Syncing Workshop mods; Steam Cloud Push is not run.")
                );
                WorkshopModConsent.Accept("automation-workshop-sync");
                await _model.StartWorkshopSyncAsync().ConfigureAwait(false);
            }

            if (request.LaunchSafe)
            {
                _runOnMainThread(() =>
                {
                    _view.AppendLog("[Automation] Safe launch requested after replacement download.");
                    RefreshSelectedRuntimeSlotEvidence();
                    _model.LaunchSafe();
                });
            }

            WriteAutomationMarker(request, "completed");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Automation] {request.Action} failed: {ex}");
            WriteAutomationMarker(request, "failed", ex.Message);
        }
        finally
        {
            _runOnMainThread(() => _view.SetRefreshGameVersionsBusy(false));
        }
    }
}
