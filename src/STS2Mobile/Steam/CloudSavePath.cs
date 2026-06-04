namespace STS2Mobile.Steam;

internal static class CloudSavePath
{
    private const string CurrentRunPathToken = "current_run";
    private const string PrefsPathToken = "prefs";
    private const string ProgressPathToken = "progress";
    private const string ProgressSaveFile = "progress.save";
    private const string SaveExtension = ".save";

    internal static string Canonicalize(string path)
        => path.Replace("user://", "").Replace("\\", "/");

    internal static string CanonicalizeLower(string path)
        => Canonicalize(path).ToLowerInvariant();

    internal static string Relative(string? path)
        => Canonicalize(path ?? string.Empty).TrimStart('/');

    internal static bool IsProgressSave(string path)
        => IsProgressSaveCanonicalLower(CanonicalizeLower(path));

    internal static bool IsCurrentRunSave(string path)
    {
        var canonLowerPath = CanonicalizeLower(path);
        return IsSave(canonLowerPath)
            && canonLowerPath.Contains(CurrentRunPathToken);
    }

    internal static bool IsImportantForBackup(string path)
    {
        var canonLowerPath = CanonicalizeLower(path);
        return canonLowerPath.Contains(ProgressSaveFile)
            || canonLowerPath.Contains(CurrentRunPathToken)
            || canonLowerPath.Contains(PrefsPathToken);
    }

    private static bool IsProgressSaveCanonicalLower(string canonLowerPath)
        => IsSave(canonLowerPath) && canonLowerPath.Contains(ProgressPathToken);

    private static bool IsSave(string canonLowerPath)
        => canonLowerPath.EndsWith(SaveExtension);
}
