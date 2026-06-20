using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLocalSaveEvidence
{
    private const int MaxFilesToInspect = 1000;
    private const int MaxDirectoriesToInspect = 250;

    private static readonly string[] IgnoredDirectoryNames =
    {
        ".godot",
        "cache",
        LauncherStorageNames.DownloadStateDirectory,
        LauncherStorageNames.GameDirectory,
        LauncherStorageNames.GameVersionsDirectory,
        LauncherStorageNames.MonoDirectory,
        LauncherStorageNames.PublishDirectory,
        "tmp",
    };

    internal static bool HasImportantSaveEvidence(string dataDir)
        => CountImportantSaveEvidence(dataDir) > 0;

    internal static int CountImportantSaveEvidence(string dataDir)
    {
        if (string.IsNullOrWhiteSpace(dataDir) || !Directory.Exists(dataDir))
            return 0;

        var count = 0;
        foreach (var file in EnumerateFilesSafely(dataDir))
        {
            if (IsImportantSaveEvidence(dataDir, file))
                count++;
        }

        return count;
    }
}
