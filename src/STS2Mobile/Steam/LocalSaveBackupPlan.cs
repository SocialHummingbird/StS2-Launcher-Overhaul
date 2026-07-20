using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static class LocalSaveBackupPlan
{
    internal const string CurrentDirectoryName = "Current";
    internal const string HistoryDirectoryName = "History";
    internal const int MaxFiles = 1000;
    internal const int MaxHistoryGenerations = 20;

    internal static string NormalizeRelativePath(string path)
        => (path ?? string.Empty)
            .Replace("user://", string.Empty)
            .Replace('\\', '/')
            .TrimStart('/');

    internal static bool IsBackupEligible(string path)
    {
        var lower = NormalizeRelativePath(path).ToLowerInvariant();
        var name = FileName(lower);
        return lower.EndsWith(".save", StringComparison.Ordinal)
            || lower.EndsWith(".save.backup", StringComparison.Ordinal)
            || lower.EndsWith(".run", StringComparison.Ordinal)
            || string.Equals(name, "prefs", StringComparison.Ordinal)
            || string.Equals(name, "prefs.backup", StringComparison.Ordinal);
    }

    internal static bool ShouldRestoreMissing(string path)
    {
        var lower = NormalizeRelativePath(path).ToLowerInvariant();
        var name = FileName(lower);
        return string.Equals(name, "profile.save", StringComparison.Ordinal)
            || string.Equals(name, "progress.save", StringComparison.Ordinal)
            || string.Equals(name, "progress.save.backup", StringComparison.Ordinal)
            || string.Equals(name, "prefs", StringComparison.Ordinal)
            || string.Equals(name, "prefs.backup", StringComparison.Ordinal)
            || string.Equals(name, "prefs.save", StringComparison.Ordinal)
            || string.Equals(name, "prefs.save.backup", StringComparison.Ordinal);
    }

    internal static bool TryResolveUnderRoot(
        string root,
        string relativePath,
        out string fullPath
    )
    {
        fullPath = string.Empty;
        var normalized = NormalizeRelativePath(relativePath);
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(normalized))
            return false;

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment is "." or ".." || segment.Contains(':'))
                return false;
        }

        try
        {
            var fullRoot = Path.GetFullPath(root);
            var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? fullRoot
                : fullRoot + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(
                Path.Combine(fullRoot, normalized.Replace('/', Path.DirectorySeparatorChar))
            );
            if (!candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
                return false;

            fullPath = candidate;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string FileName(string normalizedPath)
    {
        var separator = normalizedPath.LastIndexOf('/');
        return separator >= 0 ? normalizedPath[(separator + 1)..] : normalizedPath;
    }
}
