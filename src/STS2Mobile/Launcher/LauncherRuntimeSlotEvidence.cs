using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeSlotEvidence
{
    internal const string MarkerFileName = "current_runtime_slot.json";

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
            PatchHelper.Log($"[Launcher] Failed to clear runtime slot evidence marker: {ex.Message}");
        }
    }
}
