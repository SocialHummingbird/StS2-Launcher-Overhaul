using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly struct ShaderWarmupDecision
    {
        private readonly StartupContext _startup;
        private readonly bool _shouldRun;
        private readonly bool _shouldShowSkipped;

        private ShaderWarmupDecision(
            StartupContext startup,
            bool shouldRun,
            bool shouldShowSkipped
        )
        {
            _startup = startup;
            _shouldRun = shouldRun;
            _shouldShowSkipped = shouldShowSkipped;
        }

        internal static ShaderWarmupDecision From(StartupContext startup)
        {
            var skipWarmup = startup.ShouldSkipShaderWarmup();
            return new ShaderWarmupDecision(
                startup,
                ShaderWarmupScreen.NeedsWarmup() && !skipWarmup,
                skipWarmup
            );
        }

        internal async Task RunIfNeededAsync()
        {
            if (_shouldRun)
            {
                _startup.SetPhase(PhaseShaderWarmup, "Warming shaders...");
                PatchHelper.Log("Shader warmup starting");

                await RunShaderWarmupAsync(_startup);
                PatchHelper.Log("Shader warmup complete");
                return;
            }

            if (_shouldShowSkipped)
                _startup.ShowShaderWarmupSkipped();
        }
    }

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
