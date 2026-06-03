using System;
using System.Reflection;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static async Task RunGameStartupAsync(StartupContext startup)
    {
        try
        {
            await startup.RunGameStartupWithRecoveryAsync();
        }
        catch (TargetInvocationException ex)
        {
            var root = ex.InnerException ?? ex;
            startup.HandleFailure(root);
        }
        catch (Exception ex)
        {
            startup.HandleFailure(ex);
        }
    }
}
