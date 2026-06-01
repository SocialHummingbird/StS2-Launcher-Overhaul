using System;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static string? GetSafeBodyFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var normalized = fullPath.Replace('\\', '/');
            var fileName = Path.GetFileName(fullPath);
            if (
                !fileName.StartsWith("sts2_cdn_", StringComparison.Ordinal)
                || !fileName.EndsWith(".bin", StringComparison.Ordinal)
            )
            {
                return null;
            }

            if (
                !normalized.Contains("/cache/sts2_cdn_", StringComparison.Ordinal)
                && !normalized.Contains("/files/tmp/sts2_cdn_", StringComparison.Ordinal)
            )
            {
                return null;
            }

            return fullPath;
        }
        catch
        {
            return null;
        }
    }

    private static void DeleteBodyFileIfSafe(string? path)
    {
        var safePath = GetSafeBodyFilePath(path);
        if (safePath != null)
            DeleteFileQuietly(safePath);
    }

    private static void DeleteFileQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
