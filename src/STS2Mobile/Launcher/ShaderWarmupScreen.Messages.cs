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

        internal static string ScreenBuildFailed(Exception ex)
            => $"[ShaderWarmup] BuildUI failed: {ex}";

        internal static string RunFailed(Exception ex)
            => $"[ShaderWarmup] Failed: {ex}";
    }
}
