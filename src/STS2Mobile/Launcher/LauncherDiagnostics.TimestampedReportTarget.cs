using System;
using System.IO;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const string DiagnosticsDirectory = "diagnostics";

    private readonly struct TimestampedReportTarget
    {
        private readonly string _fileNamePrefix;
        private readonly string _fallbackDirectory;

        private TimestampedReportTarget(string fileNamePrefix, string fallbackDirectory)
        {
            _fileNamePrefix = fileNamePrefix;
            _fallbackDirectory = fallbackDirectory;
        }

        internal static TimestampedReportTarget For(
            string fileNamePrefix,
            string fallbackDirectory
        )
            => new(fileNamePrefix, fallbackDirectory);

        internal string Write(string text)
        {
            var fileName = $"{_fileNamePrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
            var targetPath = TryGetExternalDiagnosticsPath(fileName)
                ?? Path.Combine(_fallbackDirectory, fileName);

            var parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            File.WriteAllText(targetPath, text);
            return targetPath;
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
}
