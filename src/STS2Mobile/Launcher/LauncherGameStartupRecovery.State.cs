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
            PatchHelper.Log(
                "Post-startup recovery UI cleanup scheduled after game startup was observed; " +
                $"cleanupDelayMs={PostStartupRecoveryMs}, preserving last game-scene trace"
            );
            var controlsHidden = HideIfAlive(RecoveryControls, "recovery controls");
            var statusHidden = HideIfAlive(StartupStatus, "startup status");
            PatchHelper.Log(
                "Post-startup recovery UI hidden after game startup was observed; " +
                $"controlsHidden={controlsHidden}, statusHidden={statusHidden}"
            );
            await Task.Delay(PostStartupRecoveryMs);
            LauncherLaunchMarkers.ClearStartupMarker();

            var controlsCleared = QueueFreeIfAlive(RecoveryControls, "recovery controls");
            var statusCleared = LauncherStartupStatus.QueueFree(StartupStatus);

            PatchHelper.Log(
                "Post-startup recovery UI cleanup finished after game startup was observed; " +
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

        private static bool HideIfAlive(Node node, string label)
        {
            if (node is null)
                return true;

            try
            {
                DisableInteraction(node);
                node.ProcessMode = Node.ProcessModeEnum.Disabled;

                if (node is CanvasItem canvasItem)
                    canvasItem.Visible = false;
                else if (node is CanvasLayer canvasLayer)
                    canvasLayer.Visible = false;

                return true;
            }
            catch (ObjectDisposedException)
            {
                PatchHelper.Log($"Post-startup recovery {label} already disposed");
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"Post-startup recovery {label} hide failed: {ex.Message}");
                return false;
            }
        }

        private static void DisableInteraction(Node node)
        {
            if (node is Control control)
            {
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
                control.FocusMode = Control.FocusModeEnum.None;
                if (control.HasFocus())
                    control.ReleaseFocus();
            }

            foreach (var child in node.GetChildren())
                DisableInteraction(child);
        }
    }
}
