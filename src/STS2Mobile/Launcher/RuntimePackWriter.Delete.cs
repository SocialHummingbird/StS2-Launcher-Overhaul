using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    internal static void DeleteRuntimePack(GameRuntimeSlot slot, string reason)
    {
        if (slot == null)
            return;

        try
        {
            var packDirectory = Path.GetDirectoryName(slot.RuntimePackManifestPath);
            if (string.IsNullOrWhiteSpace(packDirectory) || !Directory.Exists(packDirectory))
                return;

            Directory.Delete(packDirectory, recursive: true);
            PatchHelper.Log($"[Launcher] Deleted runtime pack for '{slot.Branch}' because {reason}.");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to delete runtime pack for '{slot.Branch}': {ex.Message}");
        }
    }
}
