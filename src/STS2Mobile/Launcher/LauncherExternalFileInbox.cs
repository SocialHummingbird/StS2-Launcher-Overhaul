using System;
using System.IO;
using STS2Mobile;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherExternalFileInbox
{
    internal static string[]? ConsumeLines(
        string fileName,
        string failureMessage
    )
        => ConsumeFile(fileName, failureMessage, File.ReadAllLines);

    internal static string? ConsumeText(
        string fileName,
        string failureMessage
    )
        => ConsumeFile(fileName, failureMessage, File.ReadAllText);

    private static T? ConsumeFile<T>(
        string fileName,
        string failureMessage,
        Func<string, T> read
    )
        where T : class
    {
        var path = GetExistingInboxPath(fileName);
        if (path == null)
            return null;

        try
        {
            var content = read(path);
            File.Delete(path);
            return content;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"{failureMessage}: {ex.Message}");
            return null;
        }
    }

    private static string? GetExistingInboxPath(string fileName)
    {
        var path = GetPath(fileName);
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
            ? path
            : null;
    }

    private static string? GetPath(string fileName)
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
