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

    internal static void Revoke(string dataDir)
    {
        var path = MarkerPath(dataDir);
        if (!File.Exists(path))
            return;

        File.Delete(path);
        if (File.Exists(path))
            throw new IOException($"Failed to revoke launch authorization marker: {path}.");

        PatchHelper.Log($"[Launcher] Revoked runtime-slot launch authorization: {path}");
    }

}
