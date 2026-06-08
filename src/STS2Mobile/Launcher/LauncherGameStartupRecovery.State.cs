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

        private void ShowControls()
            => LauncherStartupRecoveryControlPanel.Show(GameNode);

        internal void ScheduleCleanup(CanvasLayer recoveryControls)
            => RecoveryCleanupTarget.For(recoveryControls, StartupStatus)
                .Schedule();

        internal void ShowFailure(RecoveryStateUpdate update)
        {
            Apply(update);
            ShowControls();
        }

        internal void MarkRecoveredStartup(
            CanvasLayer recoveryControls,
            RecoveryStateUpdate update
        )
        {
            Apply(update);
            ScheduleCleanup(recoveryControls);
        }
    }

    private readonly struct RecoveryCleanupTarget
    {
        private RecoveryCleanupTarget(
            CanvasLayer recoveryControls,
            Label startupStatus
        )
        {
            RecoveryControls = recoveryControls;
            StartupStatus = startupStatus;
        }

        private CanvasLayer RecoveryControls { get; }
        private Label StartupStatus { get; }

        internal static RecoveryCleanupTarget For(
            CanvasLayer recoveryControls,
            Label startupStatus
        )
            => new(recoveryControls, startupStatus);

        internal void Schedule()
            => _ = RunAsync();

        private async Task RunAsync()
        {
            await Task.Delay(PostStartupRecoveryMs);
            LauncherLaunchMarkers.ClearStartupMarker();

            var controlsCleared = QueueFreeIfAlive(RecoveryControls, "recovery controls");
            var statusCleared = QueueFreeIfAlive(StartupStatus, "startup status");

            PatchHelper.Log(
                "Post-startup recovery cleanup finished; " +
                $"controlsCleared={controlsCleared}, statusCleared={statusCleared}, scene snapshot retained"
            );
        }

        private static bool QueueFreeIfAlive(Node node, string label)
        {
            if (node is null)
                return true;

            try
            {
                node.QueueFree();
                return true;
            }
            catch (ObjectDisposedException)
            {
                PatchHelper.Log($"Post-startup recovery {label} already disposed");
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"Post-startup recovery {label} cleanup failed: {ex.Message}");
                return false;
            }
        }
    }
}