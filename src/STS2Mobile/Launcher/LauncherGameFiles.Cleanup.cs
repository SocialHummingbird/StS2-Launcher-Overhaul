using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to delete directory {path}: {ex.Message}");
        }
    }
}
