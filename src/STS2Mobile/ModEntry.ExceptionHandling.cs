using System;
using System.Threading.Tasks;

namespace STS2Mobile;

public static partial class ModEntry
{
    private static void InstallManagedExceptionHandlers()
    {
        if (System.Threading.Interlocked.Exchange(ref _exceptionHandlersInstalled, 1) == 1)
            return;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            try
            {
                BootstrapTrace.Log($"Unhandled managed exception: {args.ExceptionObject}");
            }
            catch (Exception ex)
            {
                BootstrapTrace.Log($"Managed exception handler logging failed: {ex.Message}");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            try
            {
                BootstrapTrace.Log($"Unobserved task exception: {args.Exception}");
            }
            catch (Exception ex)
            {
                BootstrapTrace.Log($"Managed exception handler logging failed: {ex.Message}");
            }
        };

        BootstrapTrace.Log("Managed exception handlers installed");
    }
}
