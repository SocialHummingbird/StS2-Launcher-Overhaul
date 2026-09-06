using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly partial struct StartupContext
    {
        internal async Task RunGameStartupWithRecoveryAsync()
        {
            SetPhase(PhaseGameStartup, "Starting game scene...");
            var recoveryControls = OperatingSystem.IsAndroid() ? null : LauncherStartupRecoveryControlPanel.Show(GameNode);
            await WaitForVisibleStartupFrameAsync("starting game");
            LauncherGameSceneReadiness.BeginStartup(Game, AttemptId);
            try
            {
                // No fallback starts a competing task. The operation continues observing
                // uncancellable game work even when the foreground deadline expires.
                using (var stage = new LauncherStartupStageScope(GameNode, Operation, TimeSpan.FromMilliseconds(StartupWatchdogMs)))
                {
                    var game = Game;
                    var task = Operation.StartGame(() => LauncherStartupFlow.StartGameStartupAsync(game));
                    await stage.WaitAsync(task);
                }
                LauncherGameStartupRecovery.MarkGameStartupCompleted(Game, GameNode);
                Operation.SetStage(LauncherStartupStage.WaitingForMenu);
                SetStatus("Waiting for the game menu...");
                var startup = this;
                await LauncherGameStartupRecovery.CompleteMainMenuHandoffAsync(
                    async () => await LauncherGameStartupRecovery.EnsureMainMenuReadyAsync(startup.Game, startup.GameNode, startup.Status, startup.AttemptId)
                        && startup.Operation.Complete(),
                    () => LauncherGameStartupRecovery.MarkStartupObserved(startup.Game, recoveryControls, startup.Status, startup.GameNode, startup.AttemptId),
                    () =>
                    {
                        LauncherGameSceneReadiness.EndStartup(startup.AttemptId);
                        return LauncherGameStartupRecovery.HoldAndroidStartupTaskAfterObservedAsync();
                    });
            }
            finally
            {
                LauncherGameSceneReadiness.EndStartup(AttemptId);
            }
        }
    }
}
