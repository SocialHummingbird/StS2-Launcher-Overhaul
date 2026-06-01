using System;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static class Message
    {
        internal const string ScanningStatus = "Scanning for shaders...";
        internal const string CompilingStatus = "Compiling shaders...";
        internal const string DoneStatus = "Done!";
        internal const string ScreenInitialized = "[ShaderWarmup] Screen initialized";

        internal static string Collected(int materialCount)
            => $"[ShaderWarmup] Collected {materialCount} materials to warm";

        internal static string FoundScenes(int sceneCount)
            => $"[ShaderWarmup] Found {sceneCount} scenes to scan";

        internal static string Compiled(int materialCount, long elapsedMilliseconds)
            => $"Compiled {materialCount} shaders in {elapsedMilliseconds}ms";

        internal static string Completed(int materialCount, long elapsedMilliseconds)
            => $"[ShaderWarmup] Completed: {materialCount} materials in {elapsedMilliseconds}ms";

        internal static string UniqueShaders(int materialCount, int uniqueShaderCount)
            => $"[ShaderWarmup] {materialCount} total materials, {uniqueShaderCount} unique shaders";

        internal static string PropertyReadFailed(string propertyName, string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to read property {propertyName} in {scenePath}: {ex.Message}";

        internal static string SceneExtractFailed(string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to extract from {scenePath}: {ex.Message}";

        internal static string FoundLooseMaterials(int materialCount)
            => $"[ShaderWarmup] Found {materialCount} materials from loose resource files";

        internal static string FoundMaterialsDetail(int materialCount)
            => $"Found {materialCount} materials...";

        internal static string DirectoryEnumerationFailed(string dirPath, Exception ex)
            => $"[ShaderWarmup] Failed to enumerate {dirPath}: {ex.Message}";

        internal static string ResourceLoadFailed(string cleanPath, Exception ex)
            => $"[ShaderWarmup] Failed to load {cleanPath}: {ex.Message}";

        internal static string ScanningScenes(int index, int total)
            => $"Scanning scenes... {index} / {total}";

        internal static string MarkerCheckFailed(Exception ex)
            => $"[ShaderWarmup] NeedsWarmup check failed: {ex.Message}";

        internal static string MarkerMissing()
            => "[ShaderWarmup] NeedsWarmup=true (no marker file)";

        internal static string MarkerMatches(string content)
            => $"[ShaderWarmup] NeedsWarmup=false (marker v{content} matches)";

        internal static string MarkerMismatch(string content, int expectedVersion)
            => $"[ShaderWarmup] NeedsWarmup=true (marker v{content} != v{expectedVersion})";

        internal static string MarkerWriteFailed(Exception ex)
            => $"[ShaderWarmup] Failed to write version marker: {ex.Message}";

        internal static string ScreenBuildFailed(Exception ex)
            => $"[ShaderWarmup] BuildUI failed: {ex}";

        internal static string RunFailed(Exception ex)
            => $"[ShaderWarmup] Failed: {ex}";
    }
}
