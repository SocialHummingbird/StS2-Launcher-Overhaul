using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static class ShaderWarmupPresentationRun
{
    internal static async Task ExecuteAsync(
        Func<Task> runPresentation,
        Func<bool> cleanupPresentation
    )
    {
        try
        {
            await runPresentation();
        }
        finally
        {
            cleanupPresentation();
        }
    }
}
