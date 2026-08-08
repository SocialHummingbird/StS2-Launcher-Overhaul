using System;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class Message
    {
        internal const string ScanningStatus = "Scanning for shaders...";
        internal const string CompilingStatus = "Compiling shaders...";
        internal const string DoneStatus = "Done!";
        internal const string InitialDetail = "Enumerating resources...";
        internal const string ScreenInitialized = "[ShaderWarmup] Screen initialized";

        internal static string Collected(int materialCount)
            => $"[ShaderWarmup] Collected {materialCount} materials to warm";

        internal static string Completed(WarmupCompletion completion)
            => $"[ShaderWarmup] Completed: {completion.MaterialCount} materials in {completion.ElapsedMilliseconds}ms";

        internal static string CompletedPartial(WarmupPartialCompletion completion)
            => $"[ShaderWarmup] Time-budgeted: rendered {completion.RenderedMaterialCount}/{completion.TotalMaterialCount} materials in {completion.ElapsedMilliseconds}ms";

        internal static string ScreenBuildFailed(Exception ex)
            => $"[ShaderWarmup] BuildUI failed: {ex}";

        internal static string RunFailed(Exception ex)
            => $"[ShaderWarmup] Failed: {ex}";

        internal static string StatusMarkerWriteFailed(Exception ex)
            => $"[ShaderWarmup] Failed to write status marker: {ex.Message}";

        internal static string WatchdogWarning(int seconds)
            => $"[ShaderWarmup] Still active after {seconds}s; wrote watchdog status marker";

        internal static string TimeBudgetReached(int seconds)
            => $"[ShaderWarmup] Time budget reached after {seconds}s; continuing startup with partial warmup";
    }
}
