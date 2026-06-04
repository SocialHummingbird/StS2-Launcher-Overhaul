using System;
using System.IO;
using System.Text;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const string DiagnosticsDirectory = "diagnostics";

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
        {
            var fileName = $"{_fileNamePrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
            var targetPath = TryGetExternalDiagnosticsPath(fileName)
                ?? Path.Combine(_fallbackDirectory, fileName);

            var parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            File.WriteAllText(targetPath, BuildText());
            return targetPath;
        }
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

    private static string TryGetExternalDiagnosticsPath(string fileName)
    {
        try
        {
            var externalDir = AndroidGodotAppBridge.GetExternalFilesDirPath();
            if (string.IsNullOrWhiteSpace(externalDir))
                return null;

            var diagnosticsDir = Path.Combine(externalDir, DiagnosticsDirectory);
            Directory.CreateDirectory(diagnosticsDir);
            return Path.Combine(diagnosticsDir, fileName);
        }
        catch
        {
            return null;
        }
    }
}
