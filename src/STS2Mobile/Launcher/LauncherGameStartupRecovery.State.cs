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
        private RecoveryStateUpdate(
            string reason,
            string statusMessage,
            string? snapshotReason = null
        )
        {
            Reason = reason;
            StatusMessage = statusMessage;
            SnapshotReason = snapshotReason;
        }

        private string Reason { get; }
        private string StatusMessage { get; }
        private string? SnapshotReason { get; }
        private string EffectiveSnapshotReason => SnapshotReason ?? Reason;

        internal static RecoveryStateUpdate Create(
            string reason,
            string statusMessage,
            string? snapshotReason = null
        )
            => new(reason, statusMessage, snapshotReason);

        internal void Apply(Node gameNode, Label startupStatus)
        {
            LauncherLaunchMarkers.WriteStartupPhase(Reason);
            LauncherDiagnostics.WriteStartupSceneSnapshot(
                gameNode,
                EffectiveSnapshotReason
            );
            LauncherStartupStatus.Set(startupStatus, StatusMessage);
        }
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
        update.Apply(gameNode, startupStatus);
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
