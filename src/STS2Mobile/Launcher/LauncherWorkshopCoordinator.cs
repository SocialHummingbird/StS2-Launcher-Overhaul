using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed class LauncherWorkshopCoordinator
{
    private const string WorkshopModConsentMessage =
        "Steam Workshop mods can run community code and change game content. Updating uses your subscribed Steam Workshop items only.";
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly Action _refreshModsPresentation;

    internal LauncherWorkshopCoordinator(
        LauncherModel model,
        LauncherView view,
        Action refreshModsPresentation
    )
    {
        _model = model;
        _view = view;
        _refreshModsPresentation = refreshModsPresentation ?? (() => { });
    }

    internal void SyncPressed()
        => _view.ShowConfirmation(
            WorkshopModConsentMessage,
            () => _ = RunSyncAsync(),
            "Update Workshop mods",
            "Cancel"
        );

    internal void ClearPressed()
        => ShowRemoveDownloadedModsConfirmation(_view, ClearMods);

    internal static void ShowRemoveDownloadedModsConfirmation(
        LauncherView view,
        Action onConfirmed
    )
        => view.ShowConfirmation(
            "Remove the launcher's local Workshop mod copies? Your selections and cached downloads remain, but these mods cannot load until Workshop mods are updated again.",
            onConfirmed,
            "Remove downloaded Workshop mods",
            "Cancel"
        );

    private async Task RunSyncAsync()
    {
        try
        {
            LauncherLaunchMarkers.RecordPhase("workshop sync requested");
            WorkshopModConsent.Accept("launcher-workshop-sync");
            _view.SetWorkshopButtonsDisabled(true);
            _view.SetStatus("Updating Workshop mods...", LauncherStatusSeverity.Working);
            _view.AppendLog("Updating Workshop mods.");
            await _model.StartWorkshopSyncAsync();
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("workshop sync failed", ex.GetBaseException().Message);
            PatchHelper.Log($"[Launcher] Workshop sync handler failed: {ex}");
            FailSync(ex.GetBaseException().Message);
        }
        finally
        {
            _view.SetWorkshopButtonsDisabled(false);
        }
    }

    internal void CompleteSync(string summary)
    {
        LauncherLaunchMarkers.RecordPhase("workshop sync completed", summary);
        var detail = string.IsNullOrWhiteSpace(summary) ? "Workshop mods updated" : summary;
        _view.SetStatus(
            $"{detail}. Restart the game if it was already running.",
            LauncherStatusSeverity.Information
        );
        _view.AppendLog(detail);
        RefreshModsPresentation();
    }

    internal void FailSync(string message)
    {
        _view.SetStatus($"Workshop mod update failed: {message}", LauncherStatusSeverity.Error);
        _view.AppendLog($"Workshop mod update failed: {message}");
        RefreshModsPresentation();
    }

    private void ClearMods()
    {
        try
        {
            LauncherLaunchMarkers.RecordPhase("workshop clear requested");
            _view.SetWorkshopButtonsDisabled(true);
            _view.SetStatus("Removing Workshop mods from the launcher...", LauncherStatusSeverity.Working);
            _view.AppendLog("Removing Workshop mods from the launcher.");
            WorkshopModConsent.Clear();
            _model.ClearWorkshopMods();
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("workshop clear failed", ex.GetBaseException().Message);
            PatchHelper.Log($"[Launcher] Workshop clear handler failed: {ex}");
            FailClear(ex.GetBaseException().Message);
        }
        finally
        {
            _view.SetWorkshopButtonsDisabled(false);
        }
    }

    internal void CompleteClear(int removedCount)
    {
        LauncherLaunchMarkers.RecordPhase("workshop clear completed", $"removedCount={removedCount}");
        _view.SetStatus(
            $"Removed {removedCount} Workshop mod entries from the launcher. Restart the game if it was already running.",
            LauncherStatusSeverity.Information
        );
        _view.AppendLog(
            $"Removed {removedCount} Workshop mod entries from the launcher."
        );
        RefreshModsPresentation();
    }

    internal void FailClear(string message)
    {
        _view.SetStatus($"Workshop mod clear failed: {message}", LauncherStatusSeverity.Error);
        _view.AppendLog($"Workshop mod clear failed: {message}");
        RefreshModsPresentation();
    }

    private void RefreshModsPresentation()
    {
        try
        {
            _refreshModsPresentation();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Mods presentation refresh failed: {ex.Message}");
        }
    }
}
