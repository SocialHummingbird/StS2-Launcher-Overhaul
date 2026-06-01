using System;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static class Message
    {
        public const string ScanningStatus = "Scanning for shaders...";
        public const string CompilingStatus = "Compiling shaders...";
        public const string DoneStatus = "Done!";
        public const string ScreenInitialized = "[ShaderWarmup] Screen initialized";

        public static string Collected(int materialCount)
            => $"[ShaderWarmup] Collected {materialCount} materials to warm";

        public static string FoundScenes(int sceneCount)
            => $"[ShaderWarmup] Found {sceneCount} scenes to scan";

        public static string Compiled(int materialCount, long elapsedMilliseconds)
            => $"Compiled {materialCount} shaders in {elapsedMilliseconds}ms";

        public static string Completed(int materialCount, long elapsedMilliseconds)
            => $"[ShaderWarmup] Completed: {materialCount} materials in {elapsedMilliseconds}ms";

        public static string UniqueShaders(int materialCount, int uniqueShaderCount)
            => $"[ShaderWarmup] {materialCount} total materials, {uniqueShaderCount} unique shaders";

        public static string PropertyReadFailed(string propertyName, string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to read property {propertyName} in {scenePath}: {ex.Message}";

        public static string SceneExtractFailed(string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to extract from {scenePath}: {ex.Message}";

        public static string FoundLooseMaterials(int materialCount)
            => $"[ShaderWarmup] Found {materialCount} materials from loose resource files";

        public static string FoundMaterialsDetail(int materialCount)
            => $"Found {materialCount} materials...";

        public static string DirectoryEnumerationFailed(string dirPath, Exception ex)
            => $"[ShaderWarmup] Failed to enumerate {dirPath}: {ex.Message}";

        public static string ResourceLoadFailed(string cleanPath, Exception ex)
            => $"[ShaderWarmup] Failed to load {cleanPath}: {ex.Message}";

        public static string ScanningScenes(int index, int total)
            => $"Scanning scenes... {index} / {total}";

        public static string MarkerCheckFailed(Exception ex)
            => $"[ShaderWarmup] NeedsWarmup check failed: {ex.Message}";

        public static string MarkerMissing()
            => "[ShaderWarmup] NeedsWarmup=true (no marker file)";

        public static string MarkerMatches(string content)
            => $"[ShaderWarmup] NeedsWarmup=false (marker v{content} matches)";

        public static string MarkerMismatch(string content, int expectedVersion)
            => $"[ShaderWarmup] NeedsWarmup=true (marker v{content} != v{expectedVersion})";

        public static string MarkerWriteFailed(Exception ex)
            => $"[ShaderWarmup] Failed to write version marker: {ex.Message}";

        public static string ScreenBuildFailed(Exception ex)
            => $"[ShaderWarmup] BuildUI failed: {ex}";

        public static string RunFailed(Exception ex)
            => $"[ShaderWarmup] Failed: {ex}";
    }
}
