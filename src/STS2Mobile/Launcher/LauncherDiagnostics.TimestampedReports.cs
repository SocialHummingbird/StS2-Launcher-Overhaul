using System;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private readonly struct TimestampedReport
    {
        private readonly string _fileNamePrefix;
        private readonly string _fallbackDirectory;
        private readonly string _title;
        private readonly string _generatedAtLabel;
        private readonly Action<StringBuilder> _appendBody;

        private TimestampedReport(
            string fileNamePrefix,
            string fallbackDirectory,
            string title,
            string generatedAtLabel,
            Action<StringBuilder> appendBody
        )
        {
            _fileNamePrefix = fileNamePrefix;
            _fallbackDirectory = fallbackDirectory;
            _title = title;
            _generatedAtLabel = generatedAtLabel;
            _appendBody = appendBody;
        }

        internal static TimestampedReport Launcher(
            string fallbackDirectory,
            Action<StringBuilder> appendBody
        )
            => new(
                "sts2-launcher-diagnostics",
                fallbackDirectory,
                "STS2 Launcher diagnostics",
                "Generated UTC",
                appendBody
            );

        internal static TimestampedReport StartupRecovery(
            string fallbackDirectory,
            Action<StringBuilder> appendBody
        )
            => new(
                "sts2-startup-recovery-diagnostics",
                fallbackDirectory,
                "STS2 startup recovery diagnostics",
                "Generated UTC",
                appendBody
            );

        internal string BuildText()
            => BuildTimestampedText(_title, _generatedAtLabel, _appendBody);

        internal string Write()
            => TimestampedReportTarget
                .For(_fileNamePrefix, _fallbackDirectory)
                .Write(BuildText());
    }

    private static string BuildTimestampedText(
        string title,
        string generatedAtLabel,
        Action<StringBuilder> appendBody
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine($"{generatedAtLabel}: {DateTime.UtcNow:O}");
        appendBody(sb);
        return sb.ToString();
    }
}
