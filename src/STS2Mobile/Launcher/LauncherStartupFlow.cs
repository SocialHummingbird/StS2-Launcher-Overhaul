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

        await ShowLauncherAndWaitForLaunchAsync(gameNode);
        var startup = CreateStartupContext(game, gameNode);
        try
        {
            startup.SetPhase(PhaseLaunchRequested, "Preparing game startup...");
            await startup.WaitForVisibleStartupFrameAsync("startup status shown");
            await RunShaderWarmupIfNeededAsync(startup);
            if (await InitializeSettingsAndSavesAsync(startup))
                await RunGameStartupAsync(startup);
        }
        catch (Exception ex)
        {
            startup.HandleFailure(ex);
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
