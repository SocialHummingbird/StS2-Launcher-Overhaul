using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const string StartupRecoveryDiagnosticsTitle =
        "STS2 startup recovery diagnostics";

    internal static string WriteStartupRecoveryDiagnosticsReport(
        string dataDir
    )
        => StartupRecoveryDiagnostics(dataDir).Write(
            "sts2-startup-recovery-diagnostics",
            dataDir
        );

    internal static string BuildStartupRecoveryDiagnosticsText(string dataDir)
        => StartupRecoveryDiagnostics(dataDir).Build();

    private static TimestampedText StartupRecoveryDiagnostics(string dataDir)
        => CreateTimestampedText(
            StartupRecoveryDiagnosticsTitle,
            GeneratedUtcLabel,
            sb => AppendStartupRecoveryDiagnostics(sb, dataDir)
        );

    private static void AppendStartupRecoveryDiagnostics(
        StringBuilder sb,
        string dataDir
    )
    {
        sb.AppendLine($"Data dir: {dataDir}");
        sb.AppendLine();

        AppendFileContentsSections(sb, StartupRecoveryFiles(dataDir));

        AppendLogcatTail(
            sb,
            AndroidLogcatTail,
            StartupRecoveryTailLines
        );
    }

    private static IEnumerable<DiagnosticFile> StartupRecoveryFiles(string dataDir)
    {
        foreach (var file in StartupStateFiles(dataDir))
            yield return file;

        yield return BootstrapTrace();
    }
}
