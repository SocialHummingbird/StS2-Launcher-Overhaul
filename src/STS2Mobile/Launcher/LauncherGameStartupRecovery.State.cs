using Godot;
using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private readonly struct StartupRecoveryState
    {
        private StartupRecoveryState(
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

        private static StartupRecoveryState WatchdogStalled() =>
            new(
                "game startup watchdog",
                "Game startup stalled. Attempting main menu recovery..."
            );

        private static StartupRecoveryState WatchdogRecovered() =>
            new(
                "main menu recovered after watchdog",
                "Main menu recovered after startup stall. Recovery controls remain briefly."
            );

        private static StartupRecoveryState StartupObserved() =>
            new(
                "post-startup observation",
                "Game startup returned. Recovery controls remain briefly.",
                "after NGame.GameStartup returned"
            );
    }

    private static void ShowFailure(
        Node gameNode,
        Label startupStatus,
        string reason,
        string statusMessage
    )
    {
        ShowFailure(
            gameNode,
            startupStatus,
            new StartupRecoveryState(reason, statusMessage)
        );
    }

    private static void ShowFailure(
        Node gameNode,
        Label startupStatus,
        StartupRecoveryState state
    )
    {
        RecordStartupState(gameNode, startupStatus, state);
        LauncherStartupRecoveryControlPanel.Show(gameNode);
    }

    private static void RecordStartupState(
        Node gameNode,
        Label startupStatus,
        StartupRecoveryState state
    )
    {
        LauncherLaunchMarkers.WriteStartupPhase(state.Reason);
        LauncherDiagnostics.WriteStartupSceneSnapshot(
            gameNode,
            state.SnapshotReason ?? state.Reason
        );
        LauncherStartupStatus.Set(startupStatus, state.StatusMessage);
    }

    private static void MarkRecoveredStartup(
        CanvasLayer recoveryControls,
        Label startupStatus,
        Node gameNode,
        StartupRecoveryState state
    )
    {
        RecordStartupState(gameNode, startupStatus, state);
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
