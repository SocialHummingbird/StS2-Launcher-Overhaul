using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly partial struct StartupContext
    {
        internal Task RunGameStartupWithRecoveryAsync()
            => new GameStartupAttempt(this).RunAsync();

        private CanvasLayer ShowRecoveryControls()
            => LauncherStartupRecoveryControlPanel.Show(GameNode);

        private void WriteSceneSnapshot(string reason)
            => LauncherDiagnostics.WriteStartupSceneSnapshot(GameNode, reason);

        private Task StartGameStartupAsync()
            => LauncherStartupFlow.StartGameStartupAsync(Game);
    }
}
