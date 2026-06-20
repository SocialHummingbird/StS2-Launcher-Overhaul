using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLocalSaveEvidence
{
    private static string[] SafeFiles(string directory)
    {
        try
        {
            return Directory.GetFiles(directory);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string[] SafeDirectories(string directory)
    {
        try
        {
            return Directory.GetDirectories(directory);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
