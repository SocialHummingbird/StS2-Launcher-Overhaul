using System;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static class Message
    {
        private const string ScanningStatus = "Scanning for shaders...";
        private const string CompilingStatus = "Compiling shaders...";
        private const string DoneStatus = "Done!";
        private const string ScreenInitialized = "[ShaderWarmup] Screen initialized";

        private static string Collected(int materialCount)
            => $"[ShaderWarmup] Collected {materialCount} materials to warm";

        private static string FoundScenes(int sceneCount)
            => $"[ShaderWarmup] Found {sceneCount} scenes to scan";

        private static string Compiled(int materialCount, long elapsedMilliseconds)
            => $"Compiled {materialCount} shaders in {elapsedMilliseconds}ms";

        private static string Completed(int materialCount, long elapsedMilliseconds)
            => $"[ShaderWarmup] Completed: {materialCount} materials in {elapsedMilliseconds}ms";

        private static string UniqueShaders(int materialCount, int uniqueShaderCount)
            => $"[ShaderWarmup] {materialCount} total materials, {uniqueShaderCount} unique shaders";

        private static string PropertyReadFailed(string propertyName, string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to read property {propertyName} in {scenePath}: {ex.Message}";

        private static string SceneExtractFailed(string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to extract from {scenePath}: {ex.Message}";

        private static string FoundLooseMaterials(int materialCount)
            => $"[ShaderWarmup] Found {materialCount} materials from loose resource files";

        private static string FoundMaterialsDetail(int materialCount)
            => $"Found {materialCount} materials...";

        private static string DirectoryEnumerationFailed(string dirPath, Exception ex)
            => $"[ShaderWarmup] Failed to enumerate {dirPath}: {ex.Message}";

        private static string ResourceLoadFailed(string cleanPath, Exception ex)
            => $"[ShaderWarmup] Failed to load {cleanPath}: {ex.Message}";

        private static string ScanningScenes(int index, int total)
            => $"Scanning scenes... {index} / {total}";

        private static string MarkerCheckFailed(Exception ex)
            => $"[ShaderWarmup] NeedsWarmup check failed: {ex.Message}";

        private static string MarkerMissing()
            => "[ShaderWarmup] NeedsWarmup=true (no marker file)";

        private static string MarkerMatches(string content)
            => $"[ShaderWarmup] NeedsWarmup=false (marker v{content} matches)";

        private static string MarkerMismatch(string content, int expectedVersion)
            => $"[ShaderWarmup] NeedsWarmup=true (marker v{content} != v{expectedVersion})";

        private static string MarkerWriteFailed(Exception ex)
            => $"[ShaderWarmup] Failed to write version marker: {ex.Message}";

        private static string ScreenBuildFailed(Exception ex)
            => $"[ShaderWarmup] BuildUI failed: {ex}";

        private static string RunFailed(Exception ex)
            => $"[ShaderWarmup] Failed: {ex}";
    }
}
