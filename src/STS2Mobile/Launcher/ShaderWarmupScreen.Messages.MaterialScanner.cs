using System;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class Message
    {
        internal static string FoundScenes(int sceneCount)
            => $"[ShaderWarmup] Found {sceneCount} scenes to scan";

        internal static string UniqueShaders(int materialCount, int uniqueShaderCount)
            => $"[ShaderWarmup] {materialCount} total materials, {uniqueShaderCount} unique shaders";

        internal static string PropertyReadFailed(string propertyName, string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to read property {propertyName} in {scenePath}: {ex.Message}";

        internal static string SceneExtractFailed(string scenePath, Exception ex)
            => $"[ShaderWarmup] Failed to extract from {scenePath}: {ex.Message}";

        internal static string FoundLooseMaterials(int materialCount)
            => $"[ShaderWarmup] Found {materialCount} materials from loose resource files";

        internal static string DirectoryEnumerationFailed(string dirPath, Exception ex)
            => $"[ShaderWarmup] Failed to enumerate {dirPath}: {ex.Message}";

        internal static string ResourceLoadFailed(string cleanPath, Exception ex)
            => $"[ShaderWarmup] Failed to load {cleanPath}: {ex.Message}";
    }
}
