using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendPreviousLaunchPhase(StringBuilder sb, string label)
    {
        var phase = LauncherLaunchMarkers.ReadStartupPhase();
        if (!string.IsNullOrWhiteSpace(phase))
            sb.AppendLine($"{label}: {phase}");
    }

    private static string ValueOrMissing(string value)
        => string.IsNullOrWhiteSpace(value) ? MissingDiagnosticValue : value;

    private static string BoolText(bool value)
        => value ? "true" : "false";

}
