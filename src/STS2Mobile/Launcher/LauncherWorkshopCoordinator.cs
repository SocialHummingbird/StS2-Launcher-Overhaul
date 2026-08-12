using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed class LauncherWorkshopCoordinator
{
    private const string WorkshopModConsentMessage =
        "Steam Workshop mods can run community code and change game content. Syncing uses your subscribed Steam Workshop items only.";
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
            "Enable Workshop Mods",
            "Cancel"
        );

    internal void ClearPressed()
        => ShowClearStagedModsConfirmation(_view, ClearMods);

    internal static void ShowClearStagedModsConfirmation(
        LauncherView view,
        Action onConfirmed
    )
        => view.ShowConfirmation(
            "Clear all staged Workshop mods from this device? Your persisted choices remain visible, but selected Workshop mods cannot load until they are synced again.",
            onConfirmed,
            "Clear staged mods",
            "Cancel"
        );

    private async Task RunSyncAsync()
    {
        try
        {
            LauncherLaunchMarkers.RecordPhase("workshop sync requested");
            WorkshopModConsent.Accept("launcher-workshop-sync");
            _view.SetWorkshopButtonsDisabled(true);
            _view.SetStatus("Syncing Steam Workshop mods...");
            _view.AppendLog("Syncing Steam Workshop mods.");
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
        var detail = string.IsNullOrWhiteSpace(summary) ? "Workshop mods synced" : summary;
        _view.SetStatus($"{detail}. Restart the game if it was already running.");
        _view.AppendLog(detail);
        RefreshModsPresentation();
    }

    internal void FailSync(string message)
    {
        _view.SetStatus($"Workshop mod sync failed: {message}");
        _view.AppendLog($"Workshop mod sync failed: {message}");
        RefreshModsPresentation();
    }

    private void ClearMods()
    {
        try
        {
            LauncherLaunchMarkers.RecordPhase("workshop clear requested");
            _view.SetWorkshopButtonsDisabled(true);
            _view.SetStatus("Clearing staged Workshop mods...");
            _view.AppendLog("Clearing staged Workshop mods.");
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
            $"Workshop mods cleared: removed {removedCount} staged entries. Restart the game if it was already running."
        );
        _view.AppendLog(
            $"Workshop mods cleared: removed {removedCount} staged entries."
        );
        RefreshModsPresentation();
    }

    internal void FailClear(string message)
    {
        _view.SetStatus($"Workshop mod clear failed: {message}");
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
