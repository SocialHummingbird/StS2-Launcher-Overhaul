using Godot;
using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private readonly struct RecoveryUi
    {
        private RecoveryUi(Node gameNode, Label startupStatus, string attemptId)
        {
            GameNode = gameNode;
            StartupStatus = startupStatus;
            AttemptId = attemptId;
        }

        private Node GameNode { get; }
        private Label StartupStatus { get; }
        private string AttemptId { get; }

        internal static RecoveryUi For(
            Node gameNode,
            Label startupStatus,
            string attemptId
        )
            => new(gameNode, startupStatus, attemptId);

        internal void Apply(RecoveryStateUpdate update)
            => update.Apply(GameNode, StartupStatus);

        private void ShowControls()
            => LauncherStartupRecoveryControlPanel.Show(GameNode);

        internal void Cleanup(CanvasLayer recoveryControls)
            => RecoveryCleanupTarget.For(recoveryControls).Run();

        internal bool ShowFailure(RecoveryStateUpdate update)
        {
            if (!LauncherHandoffStateOwner.Shared.Fail(AttemptId))
            {
                PatchHelper.Log(
                    $"Ignoring stale startup-failure callback for attempt {AttemptId}"
                );
                return false;
            }

            Apply(update);
            ShowControls();
            return true;
        }

        internal bool MarkRecoveredStartup(
            CanvasLayer recoveryControls,
            RecoveryStateUpdate update
        )
        {
            var handoff = LauncherHandoffStateOwner.Shared.Capture();
            if (
                handoff.State != LauncherHandoffState.GameVisible
                || !string.Equals(
                    handoff.AttemptId,
                    AttemptId,
                    StringComparison.Ordinal
                )
            )
            {
                PatchHelper.Log(
                    $"Ignoring stale startup-observed callback for attempt {AttemptId}"
                );
                return false;
            }

            Apply(update);
            Cleanup(recoveryControls);
            return true;
        }
    }

    private readonly struct RecoveryCleanupTarget
    {
        private RecoveryCleanupTarget(CanvasLayer recoveryControls)
        {
            RecoveryControls = recoveryControls;
        }

        private CanvasLayer RecoveryControls { get; }

        internal static RecoveryCleanupTarget For(CanvasLayer recoveryControls)
            => new(recoveryControls);

        internal void Run()
        {
            PatchHelper.Log(
                "Post-startup recovery UI cleanup started after main-menu handoff"
            );
            var controlsHidden = HideIfAlive(RecoveryControls, "recovery controls");
            PatchHelper.Log(
                "Post-startup recovery UI hidden after game startup was observed; " +
                $"controlsHidden={controlsHidden}"
            );
            LauncherLaunchMarkers.ClearStartupMarker();

            var controlsCleared = QueueFreeIfAlive(RecoveryControls, "recovery controls");
            PatchHelper.Log(
                "Post-startup recovery UI cleanup finished after game startup was observed; " +
                $"controlsCleared={controlsCleared}, scene snapshot retained"
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
