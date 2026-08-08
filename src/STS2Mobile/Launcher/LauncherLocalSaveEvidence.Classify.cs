using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherLocalSaveEvidence
{
    private static bool IsImportantSaveEvidence(string dataDir, string file)
    {
        var relativePath = Path.GetRelativePath(dataDir, file).Replace('\\', '/');
        var lowerPath = relativePath.ToLowerInvariant();
        var name = Path.GetFileName(lowerPath);

        return FileHasContent(file)
            && (
                lowerPath.EndsWith(".save", StringComparison.Ordinal)
                || lowerPath.EndsWith(".save.backup", StringComparison.Ordinal)
                || lowerPath.EndsWith(".run", StringComparison.Ordinal)
                || lowerPath.EndsWith(".bak", StringComparison.Ordinal)
                || string.Equals(name, "prefs", StringComparison.Ordinal)
                || string.Equals(name, "prefs.save", StringComparison.Ordinal)
                || string.Equals(name, "prefs.backup", StringComparison.Ordinal)
                || string.Equals(name, "prefs.save.backup", StringComparison.Ordinal)
            );
    }

    private static bool FileHasContent(string file)
    {
        try
        {
            return new FileInfo(file).Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
