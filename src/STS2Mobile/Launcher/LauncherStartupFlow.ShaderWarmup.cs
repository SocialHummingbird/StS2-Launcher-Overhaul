using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static async Task RunShaderWarmupIfNeededAsync(StartupContext startup)
    {
        var needsWarmup = ShaderWarmupScreen.NeedsWarmup();
        var skipWarmup = startup.ShouldSkipShaderWarmup();
        if (needsWarmup && !skipWarmup)
        {
            startup.SetPhase(PhaseShaderWarmup, "Warming shaders...");
            PatchHelper.Log("Shader warmup starting");

            await RunShaderWarmupAsync(startup);
            PatchHelper.Log("Shader warmup complete");
            return;
        }

        if (skipWarmup)
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
