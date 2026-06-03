using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly struct ShaderWarmupDecision
    {
        private ShaderWarmupDecision(bool shouldRun, bool shouldSkip)
        {
            ShouldRun = shouldRun;
            ShouldSkip = shouldSkip;
        }

        private bool ShouldRun { get; }
        private bool ShouldSkip { get; }

        internal static ShaderWarmupDecision Create(StartupContext startup)
        {
            var needsWarmup = ShaderWarmupScreen.NeedsWarmup();
            var skipWarmup = startup.ShouldSkipShaderWarmup();
            return new(
                shouldRun: needsWarmup && !skipWarmup,
                shouldSkip: skipWarmup
            );
        }

        internal async Task ApplyAsync(StartupContext startup)
        {
            if (ShouldRun)
            {
                startup.SetPhase(PhaseShaderWarmup, "Warming shaders...");
                PatchHelper.Log("Shader warmup starting");

                await RunShaderWarmupAsync(startup);
                PatchHelper.Log("Shader warmup complete");
                return;
            }

            if (ShouldSkip)
            {
                startup.LogShaderWarmupSkip();
                startup.SetShaderWarmupSkipStatus();
            }
        }
    }

    private static async Task RunShaderWarmupIfNeededAsync(StartupContext startup)
        => await ShaderWarmupDecision.Create(startup).ApplyAsync(startup);

    private static async Task RunShaderWarmupAsync(StartupContext startup)
    {
        var warmup = new ShaderWarmupScreen();
        startup.AddChild(warmup);
        try
        {
            await warmup.RunAsync();
        }
        finally
        {
            warmup.QueueFree();
        }
    }
}
