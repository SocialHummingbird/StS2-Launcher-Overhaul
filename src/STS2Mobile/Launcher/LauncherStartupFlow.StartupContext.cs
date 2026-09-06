using Godot;
using STS2Mobile.Patches;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly partial struct StartupContext
    {
        internal StartupContext(
            object game,
            Node gameNode,
            Label status,
            StartupMode mode,
            string attemptId
        )
        {
            Game = game;
            GameNode = gameNode;
            Status = status;
            Mode = mode;
            AttemptId = attemptId;
        }

        private object Game { get; }
        private Node GameNode { get; }
        private Label Status { get; }
        private StartupMode Mode { get; }
        private string AttemptId { get; }
        private LauncherStartupOperation Operation => LauncherHandoffStateOwner.Shared.GetOperation(AttemptId)
            ?? throw new System.OperationCanceledException("The launch attempt has been replaced.");

        internal bool ShouldSkipShaderWarmup()
            => Mode.ShouldSkipShaderWarmup();

        internal void SetSettingsAndSavesPhase()
            => SetPhase(
                PhaseSettingsAndSaves,
                Mode.SettingsAndSavesStatus
            );

        internal void SetPhase(string phase, string status)
        {
            Operation.SetStage(phase switch
            {
                PhaseGameStartup => LauncherStartupStage.Starting,
                PhaseSettingsAndSaves => LauncherStartupStage.SynchronizingSaves,
                _ => LauncherStartupStage.Preparing,
            });
            LauncherLaunchMarkers.WriteStartupPhase(phase);
            LauncherStartupStatus.Set(Status, status);
        }

        private void SetStatus(string status)
        {
            LauncherStartupStatus.Set(Status, status);
        }

        internal void ShowShaderWarmupSkipped()
        {
            PatchHelper.Log(Mode.ShaderWarmupSkipLog);
            SetStatus(Mode.ShaderWarmupSkipStatus);
        }

        internal ShaderWarmupPresentationHost ShowShaderWarmup()
            => ShaderWarmupPresentationHost.Show(GameNode);

        internal async Task WaitForVisibleStartupFrameAsync(string reason)
        {
            using var stage = new LauncherStartupStageScope(GameNode, Operation, System.TimeSpan.FromSeconds(15));
            PatchHelper.Log($"Waiting for rendered startup frame: {reason}");
            await stage.ProcessFrameAsync();
            await stage.PostDrawAsync();
        }
    }
}
