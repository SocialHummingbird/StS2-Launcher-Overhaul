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

    private readonly struct RecoveryStateUpdate
    {
        internal RecoveryStateUpdate(
            string reason,
            string statusMessage,
            string? snapshotReason = null
        )
        {
            Reason = reason;
            StatusMessage = statusMessage;
            SnapshotReason = snapshotReason;
        }

        internal string Reason { get; }
        internal string StatusMessage { get; }
        internal string? SnapshotReason { get; }
        internal string EffectiveSnapshotReason => SnapshotReason ?? Reason;
    }

    private static void ShowFailure(
        Node gameNode,
        Label startupStatus,
        RecoveryStateUpdate update
    )
    {
        RecordStartupState(gameNode, startupStatus, update);
        LauncherStartupRecoveryControlPanel.Show(gameNode);
    }

    private static void RecordStartupState(
        Node gameNode,
        Label startupStatus,
        RecoveryStateUpdate update
    )
    {
        LauncherLaunchMarkers.WriteStartupPhase(update.Reason);
        LauncherDiagnostics.WriteStartupSceneSnapshot(
            gameNode,
            update.EffectiveSnapshotReason
        );
        LauncherStartupStatus.Set(startupStatus, update.StatusMessage);
    }

    private static void SetRecoveryStatus(Label startupStatus, string status)
    {
        LauncherStartupStatus.Set(startupStatus, status);
    }

    private static void MarkRecoveredStartup(
        CanvasLayer recoveryControls,
        Label startupStatus,
        Node gameNode,
        RecoveryStateUpdate update
    )
    {
        RecordStartupState(gameNode, startupStatus, update);
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
