using System;
using System.Diagnostics;
using STS2Mobile.Patches;

namespace STS2Mobile;

internal static partial class StartupPatchOrchestrator
{
    private const string ForceCriticalPatchFailureVariable = "STS2_FORCE_CRITICAL_PATCH_FAILURE";

    private static bool ForceCriticalPatchFailureRequested()
        => string.Equals(
            Environment.GetEnvironmentVariable(ForceCriticalPatchFailureVariable),
            "1",
            StringComparison.Ordinal
        );

    private static StartupPatchResult ForcedCriticalPatchFailureResult(Stopwatch stopwatch)
    {
        stopwatch.Stop();
        PatchHelper.Log("[startup] Forced critical patch failure requested for fallback validation");
        return new StartupPatchResult(
            new[]
            {
                new PatchGroupResult(
                    "core",
                    true,
                    stopwatch.Elapsed,
                    new[]
                    {
                        new PatchAttempt(
                            "Forced critical patch failure",
                            false,
                            TimeSpan.Zero,
                            "Injected by STS2_FORCE_CRITICAL_PATCH_FAILURE"
                        )
                    }
                )
            },
            criticalFailed: true,
            stopwatch.Elapsed
        );
    }
}
