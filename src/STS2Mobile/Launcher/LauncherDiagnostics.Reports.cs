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
                "StS2 Mobile diagnostics",
                GeneratedUtcLabel,
                AppendFullLauncherDiagnostics
            ).Write(
                "sts2-mobile-diagnostics",
                _state.DataDir
            );

        private void AppendFullLauncherDiagnostics(StringBuilder sb)
        {
            AppendPublicSharingWarning(sb);
            AppendLauncherState(sb, LauncherStateDetail.Detailed);
            AppendLauncherPreferences(sb, _state.DataDir);
            AppendFullReportDiagnostics(sb, _state.DataDir);
        }
    }

    private static void AppendPublicSharingWarning(StringBuilder sb)
    {
        sb.AppendLine("Public sharing warning: review and redact this diagnostics report before posting publicly.");
        sb.AppendLine("It may include account names, local paths, device details, save/cloud state, and log excerpts.");
        sb.AppendLine();
    }
}
