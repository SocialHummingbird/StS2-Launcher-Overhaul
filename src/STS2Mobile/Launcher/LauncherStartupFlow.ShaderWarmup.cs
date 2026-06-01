using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static async Task RunShaderWarmupIfNeededAsync(StartupContext startup)
    {
        if (ShaderWarmupScreen.NeedsWarmup() && !startup.SkipShaderWarmup)
        {
            startup.SetPhase(PhaseShaderWarmup, "Warming shaders...");
            PatchHelper.Log("Shader warmup starting");

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

            PatchHelper.Log("Shader warmup complete");
        }
        else if (startup.SkipShaderWarmup)
        {
            startup.LogShaderWarmupSkip();
            startup.SetShaderWarmupSkipStatus();
        }
    }
}
