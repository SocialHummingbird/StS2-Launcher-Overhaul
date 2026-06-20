using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimePatchValidationEvidence
{
    internal const string MarkerFileName = "last_runtime_patch_validation.json";
    private const string RuntimeCacheMarkerFileName = "current_runtime_cache.txt";

    internal static string MarkerPath(string dataDir)
        => Path.Combine(dataDir, MarkerFileName);

    internal static bool MarkerPresent(string dataDir)
        => File.Exists(MarkerPath(dataDir));

    internal static void Clear(string dataDir)
    {
        try
        {
            var path = MarkerPath(dataDir);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to clear runtime patch validation evidence: {ex.Message}");
        }
    }
}
