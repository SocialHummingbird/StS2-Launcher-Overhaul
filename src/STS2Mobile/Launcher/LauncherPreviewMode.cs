using System;

namespace STS2Mobile.Launcher;

internal static class LauncherPreviewMode
{
    private const string EnvironmentVariable = "STS2_LAUNCHER_PREVIEW";

    internal static bool Enabled
        => string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            "1",
            StringComparison.Ordinal
        );
}
