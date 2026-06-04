using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal readonly struct StartupRecoveryDiagnosticsReport
    {
        internal StartupRecoveryDiagnosticsReport(string dataDir)
        {
            DataDir = dataDir;
        }

        private string DataDir { get; }

        internal string Write()
            => CreateTimestampedReport().Write();

        internal string BuildText()
            => CreateTimestampedReport().BuildText();

        private TimestampedReport CreateTimestampedReport()
        {
            var dataDir = DataDir;
            return TimestampedReport.StartupRecovery(
                dataDir,
                sb => AppendStartupRecoveryDiagnostics(sb, dataDir)
            );
        }
    }

    internal static StartupRecoveryDiagnosticsReport StartupRecoveryReport(
        string dataDir
    )
        => new(dataDir);

    private static void AppendStartupRecoveryDiagnostics(
        StringBuilder sb,
        string dataDir
    )
    {
        sb.AppendLine($"Data dir: {dataDir}");
        sb.AppendLine();

        foreach (var file in StartupRecoveryFiles(dataDir))
            AppendFileContentsSection(sb, file);

        AppendLogcatTail(
            sb,
            AndroidLogcatTail,
            StartupRecoveryTailLines
        );
    }

    private static IEnumerable<DiagnosticFile> StartupRecoveryFiles(string dataDir)
    {
        yield return StartupMarker(dataDir);
        yield return StartupSceneSnapshot(dataDir);
        yield return BootstrapTrace();
    }
}
