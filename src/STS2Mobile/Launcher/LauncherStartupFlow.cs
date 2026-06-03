using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private const string PhaseGameStartup = "game startup";
    private const string PhaseLaunchRequested = "launch requested";
    private const string PhaseLauncherClosed = "launcher closed";
    private const string PhaseManualSafeLaunch = "manual safe launch";
    private const string PhaseSettingsAndSaves = "settings and saves";
    private const string PhaseShaderWarmup = "shader warmup";
    private const int StartupWatchdogMs = 60_000;

    private readonly struct StartupContext
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

        private CanvasLayer ShowRecoveryControls()
            => LauncherStartupRecoveryControlPanel.Show(GameNode);

        private void WriteSceneSnapshot(string reason)
            => LauncherDiagnostics.WriteStartupSceneSnapshot(GameNode, reason);

        private Task StartGameStartupAsync()
            => LauncherStartupFlow.StartGameStartupAsync(Game);

        internal async Task RunGameStartupWithRecoveryAsync()
        {
            SetPhase(PhaseGameStartup, "Starting game scene...");
            PatchHelper.Log("Invoking NGame.GameStartup");

            var recoveryControls = ShowRecoveryControls();
            WriteSceneSnapshot("before NGame.GameStartup");
            var startupTask = StartGameStartupAsync();

            if (await RecoverIfWatchdogTimedOutAsync(startupTask, recoveryControls))
                return;

            await startupTask;
            PatchHelper.Log("NGame.GameStartup completed");
            if (!await EnsureMainMenuReadyAsync())
                return;

            MarkStartupObserved(recoveryControls);
        }

        private async Task<bool> RecoverIfWatchdogTimedOutAsync(
            Task startupTask,
            CanvasLayer recoveryControls
        )
        {
            var watchdogTask = Task.Delay(StartupWatchdogMs);
            if (await Task.WhenAny(startupTask, watchdogTask) != watchdogTask)
                return false;

            await HandleWatchdogAsync(recoveryControls);
            return true;
        }

        private Task HandleWatchdogAsync(CanvasLayer recoveryControls)
            => LauncherGameStartupRecovery.HandleWatchdogAsync(
                Game,
                GameNode,
                Status,
                recoveryControls,
                StartupWatchdogMs
            );

        private Task<bool> EnsureMainMenuReadyAsync()
            => LauncherGameStartupRecovery.EnsureMainMenuReadyAsync(Game, GameNode, Status);

        private void MarkStartupObserved(CanvasLayer recoveryControls)
            => LauncherGameStartupRecovery.MarkStartupObserved(
                recoveryControls,
                Status,
                GameNode
            );

        internal void HandleSettingsAndSavesFailure(Exception ex)
            => LauncherGameStartupRecovery.HandleSettingsAndSavesFailure(
                GameNode,
                Status,
                ex
            );

        internal void HandleFailure(Exception ex)
            => LauncherGameStartupRecovery.HandleFailure(GameNode, Status, ex);
    }

    internal static async Task RunAsync(object game)
    {
        var gameNode = (Node)game;

        var launcher = await ShowLauncherAndWaitForLaunchAsync(gameNode);
        var startup = CreateStartupContext(game, gameNode);
        startup.ApplySaveMode();

        startup.SetPhase(PhaseLaunchRequested, "Starting game...");
        PatchHelper.Log("User launched game, proceeding to startup...");

        ResetSaveManagerInstance();

        launcher.QueueFree();
        startup.SetPhase(PhaseLauncherClosed, "Launcher closed. Preparing game startup...");

        await RunShaderWarmupIfNeededAsync(startup);
        if (!InitializeSettingsAndSaves(startup))
            return;

        await RunGameStartupAsync(startup);
    }

    private static async Task<LauncherUI> ShowLauncherAndWaitForLaunchAsync(Node gameNode)
    {
        var launcher = new LauncherUI();
        launcher.SetGameMode(true);
        launcher.Initialize();
        gameNode.AddChild(launcher);
        PatchHelper.Log("Launcher UI displayed");
        await launcher.WaitForLaunch();

        return launcher;
    }

    private static StartupContext CreateStartupContext(object game, Node gameNode)
    {
        var startupStatus = LauncherStartupStatus.CreateLabel(gameNode);
        var startupMode = StartupMode.CreateFromMarkers();
        return new StartupContext(game, gameNode, startupStatus, startupMode);
    }
}
