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
        private readonly struct GameStartupAttempt
        {
            private readonly StartupContext _startup;

            internal GameStartupAttempt(StartupContext startup)
            {
                _startup = startup;
            }

            internal async Task RunAsync()
            {
                _startup.SetPhase(PhaseGameStartup, "Starting game scene...");
                PatchHelper.Log("Invoking NGame.GameStartup");

                var recoveryControls = _startup.ShowRecoveryControls();
                _startup.WriteSceneSnapshot("before NGame.GameStartup");
                var startupTask = _startup.StartGameStartupAsync();

                if (await RecoverIfWatchdogTimedOutAsync(startupTask, recoveryControls))
                    return;

                await startupTask;
                PatchHelper.Log("NGame.GameStartup completed");
                if (!await EnsureMainMenuReadyAsync())
                    return;

                MarkStartupObserved(recoveryControls);
            }

            private Task<bool> RecoverIfWatchdogTimedOutAsync(
                Task startupTask,
                CanvasLayer recoveryControls
            )
            {
                var game = _startup.Game;
                var gameNode = _startup.GameNode;
                var status = _startup.Status;
                return StartupWatchdog
                    .For(
                        startupTask,
                        () => LauncherGameStartupRecovery.HandleWatchdogAsync(
                            game,
                            gameNode,
                            status,
                            recoveryControls,
                            StartupWatchdogMs
                        )
                    )
                    .RecoverIfTimedOutAsync();
            }

            private Task<bool> EnsureMainMenuReadyAsync()
                => LauncherGameStartupRecovery.EnsureMainMenuReadyAsync(
                    _startup.Game,
                    _startup.GameNode,
                    _startup.Status
                );

            private void MarkStartupObserved(CanvasLayer recoveryControls)
                => LauncherGameStartupRecovery.MarkStartupObserved(
                    recoveryControls,
                    _startup.Status,
                    _startup.GameNode
                );
        }

        private readonly struct StartupWatchdog
        {
            private readonly Task _startupTask;
            private readonly Func<Task> _recoverAsync;

            private StartupWatchdog(Task startupTask, Func<Task> recoverAsync)
            {
                _startupTask = startupTask;
                _recoverAsync = recoverAsync;
            }

            internal static StartupWatchdog For(
                Task startupTask,
                Func<Task> recoverAsync
            )
                => new(startupTask, recoverAsync);

            internal async Task<bool> RecoverIfTimedOutAsync()
            {
                if (await LauncherTimeout.CompletesWithinAsync(
                    _startupTask,
                    StartupWatchdogMs
                ))
                    return false;

                await _recoverAsync();
                return true;
            }
        }

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

        internal Task RunGameStartupWithRecoveryAsync()
            => new GameStartupAttempt(this).RunAsync();

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
