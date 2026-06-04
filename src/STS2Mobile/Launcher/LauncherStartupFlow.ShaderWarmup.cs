using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static async Task RunShaderWarmupIfNeededAsync(StartupContext startup)
    {
        if (startup.ShouldSkipShaderWarmup())
        {
            startup.ShowShaderWarmupSkipped();
            return;
        }

        if (!ShaderWarmupScreen.NeedsWarmup())
            return;

        await RunShaderWarmupWithStatusAsync(startup);
    }

    private static async Task RunShaderWarmupWithStatusAsync(StartupContext startup)
    {
        startup.SetPhase(PhaseShaderWarmup, "Warming shaders...");
        PatchHelper.Log("Shader warmup starting");

        await RunShaderWarmupAsync(startup);
        PatchHelper.Log("Shader warmup complete");
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
