using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static Task RunShaderWarmupIfNeededAsync(StartupContext startup)
        => ShaderWarmupDecision.From(startup).RunIfNeededAsync();

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
