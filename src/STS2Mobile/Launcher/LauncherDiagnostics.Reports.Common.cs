using System.Text;
using STS2Mobile.Steam;

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

    private static bool MarkerBranchMatchesSelected(string markerBranch, string selectedBranch)
        => !string.IsNullOrWhiteSpace(markerBranch)
            && !markerBranch.StartsWith("<", System.StringComparison.Ordinal)
            && string.Equals(
                SteamGameBranch.Normalize(markerBranch),
                SteamGameBranch.Normalize(selectedBranch),
                System.StringComparison.OrdinalIgnoreCase
            );
}