using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private const int StartupWatchdogMs = 60_000;

    private readonly struct StartupContext
    {
        private StartupContext(
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
    }

    internal static async Task RunAsync(object game)
    {
        var gameNode = (Node)game;

        var launcher = new LauncherUI();
        launcher.SetGameMode(true);
        launcher.Initialize();
        gameNode.AddChild(launcher);
        PatchHelper.Log("Launcher UI displayed");
        await launcher.WaitForLaunch();

        var startupStatus = LauncherStartupStatus.CreateLabel(gameNode);
        var startupMode = StartupMode.CreateFromMarkers();
        var startup = new StartupContext(game, gameNode, startupStatus, startupMode);
        ApplyStartupSaveMode(startup);

        SetStartupPhase(startup, "launch requested", "Starting game...");
        PatchHelper.Log("User launched game, proceeding to startup...");

        ResetSaveManagerInstance();

        launcher.QueueFree();
        SetStartupPhase(startup, "launcher closed", "Launcher closed. Preparing game startup...");

        await RunShaderWarmupIfNeededAsync(startup);
        if (!InitializeSettingsAndSaves(startup))
            return;

        await RunGameStartupAsync(startup);
    }
}
