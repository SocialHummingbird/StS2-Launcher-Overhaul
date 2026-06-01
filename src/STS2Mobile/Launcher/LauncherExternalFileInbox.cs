using System;
using System.IO;
using STS2Mobile;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherExternalFileInbox
{
    internal static bool TryConsumeLines(
        string fileName,
        string failureMessage,
        out string[] lines
    )
    {
        lines = null;
        if (!TryGetExistingInboxPath(fileName, out var path))
            return false;

        try
        {
            lines = File.ReadAllLines(path);
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"{failureMessage}: {ex.Message}");
            return false;
        }
    }

    internal static bool TryConsumeText(
        string fileName,
        string failureMessage,
        out string text
    )
    {
        text = null;
        if (!TryGetExistingInboxPath(fileName, out var path))
            return false;

        try
        {
            text = File.ReadAllText(path);
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"{failureMessage}: {ex.Message}");
            return false;
        }
    }

    private static bool TryGetExistingInboxPath(string fileName, out string path)
    {
        path = GetPath(fileName);
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    private static string GetPath(string fileName)
    {
        try
        {
            var dir = AndroidGodotAppBridge.GetExternalFilesDirPath();
            return string.IsNullOrWhiteSpace(dir)
                ? null
                : Path.Combine(dir, fileName);
        }
        catch
        {
            return null;
        }
    }
}
