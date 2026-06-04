using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private enum ShaderWarmupState
    {
        NotNeeded,
        Run,
        Skipped,
    }

    private readonly struct ShaderWarmupDecision
    {
        private readonly StartupContext _startup;
        private readonly ShaderWarmupState _state;

        private ShaderWarmupDecision(
            StartupContext startup,
            ShaderWarmupState state
        )
        {
            _startup = startup;
            _state = state;
        }

        internal static ShaderWarmupDecision From(StartupContext startup)
        {
            if (startup.ShouldSkipShaderWarmup())
                return new ShaderWarmupDecision(startup, ShaderWarmupState.Skipped);

            return new ShaderWarmupDecision(
                startup,
                ShaderWarmupScreen.NeedsWarmup()
                    ? ShaderWarmupState.Run
                    : ShaderWarmupState.NotNeeded
            );
        }

        internal async Task RunIfNeededAsync()
        {
            switch (_state)
            {
                case ShaderWarmupState.Run:
                    await RunAsync();
                    return;

                case ShaderWarmupState.Skipped:
                    _startup.ShowShaderWarmupSkipped();
                    return;
            }
        }

        private async Task RunAsync()
        {
            _startup.SetPhase(PhaseShaderWarmup, "Warming shaders...");
            PatchHelper.Log("Shader warmup starting");

            await RunShaderWarmupAsync(_startup);
            PatchHelper.Log("Shader warmup complete");
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
