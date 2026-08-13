using System;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static string CompactLaunchButtonText(string text)
        => LaunchTitle(text);

    private static string LaunchTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Play";

        var normalized = text.Trim();
        return string.Equals(normalized, "Start Game", StringComparison.OrdinalIgnoreCase)
            ? "Play"
            : normalized;
    }

    private static string CompactPlaySyncDrawerText(string action, string detail)
        => $"{action}\n{detail}";

}
