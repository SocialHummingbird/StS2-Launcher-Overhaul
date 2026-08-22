using System.Threading.Tasks;
using System.Threading;
using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private const string PhaseGameStartup = "game startup";
    private const string PhaseLaunchRequested = "launch requested";
    private const string PhaseLauncherCovered = "launcher covered";
    private const string PhaseManualSafeLaunch = "manual safe launch";
    private const string PhaseSettingsAndSaves = "settings and saves";
    private const string PhaseShaderWarmup = "shader warmup";
    private const int StartupWatchdogMs = 60_000;

    internal static async Task RunAsync(object game)
    {
        if (ModEntry.HasStartupFallbackReason)
        {
            PatchHelper.Log("Startup fallback is active; blocking downloaded game startup.");
            await Task.Delay(Timeout.InfiniteTimeSpan);
            return;
        }

        var gameNode = (Node)game;
        AndroidBridgePump.EnsureInstalled(gameNode);

        var launcher = await ShowLauncherAndWaitForLaunchAsync(gameNode);
        var startup = CreateStartupContext(game, gameNode);
        await StartGameAfterLauncherAsync(launcher, startup);
    }

    private static async Task StartGameAfterLauncherAsync(
        LauncherUI launcher,
        StartupContext startup
    )
        => await new StartupLaunchSequence(launcher, startup).RunAsync();

    private readonly struct StartupLaunchSequence
    {
        internal StartupLaunchSequence(LauncherUI launcher, StartupContext startup)
        {
            Launcher = launcher;
            Startup = startup;
        }

        private LauncherUI Launcher { get; }
        private StartupContext Startup { get; }

        internal async Task RunAsync()
        {
            BeginLaunch();
            CoverLauncher();
            await Startup.WaitForVisibleStartupFrameAsync("launcher covered");
            await RunStartupAsync();
        }

        private void BeginLaunch()
        {
            Startup.SetPhase(PhaseLaunchRequested, "Starting game...");
            PatchHelper.Log("User launched game, proceeding to startup...");
        }

        private void CoverLauncher()
        {
            Startup.SetPhase(
                PhaseLauncherCovered,
                "Preparing game startup..."
            );
        }

        private async Task RunStartupAsync()
        {
            await RunShaderWarmupIfNeededAsync(Startup);
            if (!await InitializeSettingsAndSavesAsync(Startup))
                return;

            await RunGameStartupAsync(Startup);
        }
    }

    private static async Task<LauncherUI> ShowLauncherAndWaitForLaunchAsync(Node gameNode)
    {
        var launcher = LauncherHandoffStateOwner.Shared.ShowLauncher(
            gameNode,
            inGameMode: true
        );
        var launcherInitialized = launcher.Initialize();
        PatchHelper.Log("Launcher UI displayed");
        if (launcherInitialized)
            await launcher.NotifyBootTransitionWhenVisibleAsync();
        await launcher.WaitForLaunch();

        return launcher;
    }

    private static StartupContext CreateStartupContext(object game, Node gameNode)
    {
        var handoff = LauncherHandoffStateOwner.Shared.Capture();
        if (
            handoff.State != LauncherHandoffState.HandoffPending
            || string.IsNullOrWhiteSpace(handoff.AttemptId)
        )
            throw new InvalidOperationException(
                "Game startup requires an active authoritative handoff attempt."
            );

        var startupStatus = LauncherHandoffStateOwner.Shared.ShowStartupStatus(
            handoff.AttemptId,
            gameNode
        );
        var startupMode = StartupMode.CreateFromMarkers();
        AndroidMainMenuPreparation.ObserveCurrentMountedResourceSetIdentity();
        return new StartupContext(
            game,
            gameNode,
            startupStatus,
            startupMode,
            handoff.AttemptId
        );
    }
}
