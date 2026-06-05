using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const string MissingDiagnosticValue = "<none>";

    internal sealed partial class Snapshot
    {
        internal Snapshot(
            string dataDir,
            string accountName,
            bool hasSavedCredentials,
            bool gameFilesReady,
            string sessionState,
            string failReason
        )
        {
            _state = new LauncherStateReport(
                dataDir,
                accountName,
                hasSavedCredentials,
                gameFilesReady,
                sessionState,
                failReason
            );
        }

        private readonly LauncherStateReport _state;

        internal string WriteDiagnosticsReport()
            => CreateTimestampedText(
                "STS2 Launcher diagnostics",
                GeneratedUtcLabel,
                AppendFullLauncherDiagnostics
            ).Write(
                "sts2-launcher-diagnostics",
                _state.DataDir
            );

        private void AppendFullLauncherDiagnostics(StringBuilder sb)
        {
            AppendLauncherState(sb, LauncherStateDetail.Detailed);
            AppendLauncherPreferences(sb);
            AppendFullReportDiagnostics(sb, _state.DataDir);
        }
    }

    private static void AppendLauncherPreferences(StringBuilder sb)
    {
        var preferences = LauncherPreferences.ReadActionPreferences();
        sb.AppendLine($"Cloud sync pref: {preferences.CloudSyncEnabled}");
        sb.AppendLine($"Local backup pref: {preferences.LocalBackupEnabled}");
    }

    private static void AppendPreviousLaunchPhase(StringBuilder sb, string label)
    {
        var phase = LauncherLaunchMarkers.ReadStartupPhase();
        if (!string.IsNullOrWhiteSpace(phase))
            sb.AppendLine($"{label}: {phase}");
    }

    private static string ValueOrMissing(string value)
        => string.IsNullOrWhiteSpace(value) ? MissingDiagnosticValue : value;
}
