using System;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static bool TryGetSafeBodyFilePath(string? path, out string safePath)
    {
        safePath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

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
                return false;
            }

            if (
                !normalized.Contains("/cache/sts2_cdn_", StringComparison.Ordinal)
                && !normalized.Contains("/files/tmp/sts2_cdn_", StringComparison.Ordinal)
            )
            {
                return false;
            }

            safePath = fullPath;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void DeleteBodyFileIfSafe(string? path)
    {
        if (TryGetSafeBodyFilePath(path, out var safePath))
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
