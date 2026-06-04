using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly partial struct StartupContext
    {
        internal StartupContext(
            object game,
            Node gameNode,
            Label status,
            StartupMode mode
        )
        {
            Game = game;
            GameNode = gameNode;
            Status = status;
            Mode = mode;
        }

        private object Game { get; }
        private Node GameNode { get; }
        private Label Status { get; }
        private StartupMode Mode { get; }

        internal bool ShouldSkipShaderWarmup()
            => Mode.ShouldSkipShaderWarmup();

        internal void SetSettingsAndSavesPhase()
            => SetPhase(
                PhaseSettingsAndSaves,
                Mode.SettingsAndSavesStatus
            );

        internal void SetPhase(string phase, string status)
        {
            LauncherLaunchMarkers.WriteStartupPhase(phase);
            LauncherStartupStatus.Set(Status, status);
        }

        private void SetStatus(string status)
        {
            LauncherStartupStatus.Set(Status, status);
        }

        internal void ApplySaveMode()
            => Mode.ApplySaveMode();

        internal void ShowShaderWarmupSkipped()
        {
            PatchHelper.Log(Mode.ShaderWarmupSkipLog);
            SetStatus(Mode.ShaderWarmupSkipStatus);
        }

        internal void AddChild(Node child)
            => GameNode.AddChild(child);
    }
}
