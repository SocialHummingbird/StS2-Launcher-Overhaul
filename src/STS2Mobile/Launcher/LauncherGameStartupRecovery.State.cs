using Godot;
using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private readonly struct RecoveryUi
    {
        private RecoveryUi(Node gameNode, Label startupStatus)
        {
            GameNode = gameNode;
            StartupStatus = startupStatus;
        }

        private Node GameNode { get; }
        private Label StartupStatus { get; }

        internal static RecoveryUi For(Node gameNode, Label startupStatus)
            => new(gameNode, startupStatus);

        internal void Apply(RecoveryStateUpdate update)
            => update.Apply(GameNode, StartupStatus);

        internal void ShowControls()
            => LauncherStartupRecoveryControlPanel.Show(GameNode);

        internal void ScheduleCleanup(CanvasLayer recoveryControls)
            => LauncherGameStartupRecovery.ScheduleCleanup(
                recoveryControls,
                StartupStatus
            );
    }

    private static void ShowFailure(RecoveryUi ui, RecoveryStateUpdate update)
    {
        ui.Apply(update);
        ui.ShowControls();
    }

    private static void MarkRecoveredStartup(
        CanvasLayer recoveryControls,
        RecoveryUi ui,
        RecoveryStateUpdate update
    )
    {
        ui.Apply(update);
        ui.ScheduleCleanup(recoveryControls);
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
