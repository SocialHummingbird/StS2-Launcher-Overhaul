using System;

namespace STS2Mobile.Launcher;

internal readonly struct AndroidMainMenuResourceResolution
{
    private AndroidMainMenuResourceResolution(
        string logicalPath,
        string effectivePath
    )
    {
        LogicalPath = logicalPath;
        EffectivePath = effectivePath;
    }

    internal string LogicalPath { get; }
    internal string EffectivePath { get; }

    internal static AndroidMainMenuResourceResolution Create(
        string logicalPath,
        string loadedResourcePath
    )
    {
        var cleanLogicalPath = CleanVirtualPath(logicalPath);
        var cleanEffectivePath = string.IsNullOrWhiteSpace(loadedResourcePath)
            ? cleanLogicalPath
            : CleanVirtualPath(loadedResourcePath);
        return new AndroidMainMenuResourceResolution(
            cleanLogicalPath,
            string.IsNullOrEmpty(cleanEffectivePath)
                ? cleanLogicalPath
                : cleanEffectivePath
        );
    }

    private static string CleanVirtualPath(string path)
        => !string.IsNullOrWhiteSpace(path)
            && path.StartsWith("res://", StringComparison.Ordinal)
            && path.IndexOfAny(new[] { '\r', '\n' }) < 0
                ? path.Trim()
                : "<non-virtual>";
}
