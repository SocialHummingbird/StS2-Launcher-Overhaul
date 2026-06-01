using Godot;
using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private const string MainMenuGuardFailureReason = "main menu guard failed";
    private const string MainMenuRecoveryFailureReason =
        "main menu recovery failed after watchdog";
    private const string StartupObservationReason = "post-startup observation";
    private const string WatchdogStalledReason = "game startup watchdog";
    private const string WatchdogRecoveredReason = "main menu recovered after watchdog";

    private static void ShowFailure(
        Node gameNode,
        Label startupStatus,
        string reason,
        string statusMessage,
        string? snapshotReason = null
    )
    {
        RecordStartupState(gameNode, startupStatus, reason, statusMessage, snapshotReason);
        LauncherStartupRecoveryControlPanel.Show(gameNode);
    }

    private static void RecordStartupState(
        Node gameNode,
        Label startupStatus,
        string reason,
        string statusMessage,
        string? snapshotReason = null
    )
    {
        LauncherLaunchMarkers.WriteStartupPhase(reason);
        LauncherDiagnostics.WriteStartupSceneSnapshot(
            gameNode,
            snapshotReason ?? reason
        );
        LauncherStartupStatus.Set(startupStatus, statusMessage);
    }

    private static void SetRecoveryStatus(Label startupStatus, string status)
    {
        LauncherStartupStatus.Set(startupStatus, status);
    }

    private static void MarkRecoveredStartup(
        CanvasLayer recoveryControls,
        Label startupStatus,
        Node gameNode,
        string reason,
        string statusMessage,
        string? snapshotReason = null
    )
    {
        RecordStartupState(gameNode, startupStatus, reason, statusMessage, snapshotReason);
        ScheduleCleanup(recoveryControls, startupStatus);
    }

    private static void ScheduleCleanup(CanvasLayer recoveryControls, Label startupStatus)
        => _ = RunScheduledCleanupAsync(recoveryControls, startupStatus);

    private static async Task RunScheduledCleanupAsync(
        CanvasLayer recoveryControls,
        Label startupStatus
    )
    {
        try
        {
            await Task.Delay(PostStartupRecoveryMs);
            LauncherLaunchMarkers.ClearStartupMarker();
            recoveryControls?.QueueFree();
            startupStatus?.QueueFree();
            PatchHelper.Log("Post-startup recovery controls cleared; scene snapshot retained");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Post-startup recovery cleanup failed: {ex.Message}");
        }
    }
}
