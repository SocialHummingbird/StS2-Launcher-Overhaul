using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly struct ShaderWarmupDecision
    {
        private ShaderWarmupDecision(bool shouldRun, bool shouldShowSkipped)
        {
            ShouldRun = shouldRun;
            ShouldShowSkipped = shouldShowSkipped;
        }

        internal bool ShouldRun { get; }
        internal bool ShouldShowSkipped { get; }

        internal static ShaderWarmupDecision From(StartupContext startup)
        {
            var skipWarmup = startup.ShouldSkipShaderWarmup();
            return new ShaderWarmupDecision(
                ShaderWarmupScreen.NeedsWarmup() && !skipWarmup,
                skipWarmup
            );
        }
    }

    private static async Task RunShaderWarmupIfNeededAsync(StartupContext startup)
    {
        var decision = ShaderWarmupDecision.From(startup);
        if (decision.ShouldRun)
        {
            startup.SetPhase(PhaseShaderWarmup, "Warming shaders...");
            PatchHelper.Log("Shader warmup starting");

            await RunShaderWarmupAsync(startup);
            PatchHelper.Log("Shader warmup complete");
            return;
        }

        if (decision.ShouldShowSkipped)
            startup.ShowShaderWarmupSkipped();
    }

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
