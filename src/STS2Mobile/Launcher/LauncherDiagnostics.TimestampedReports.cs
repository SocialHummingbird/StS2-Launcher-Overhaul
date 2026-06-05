using System;
using System.IO;
using System.Text;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const string DiagnosticsDirectory = "diagnostics";
    private const string GeneratedUtcLabel = "Generated UTC";

    private readonly struct TimestampedText
    {
        internal TimestampedText(
            string title,
            string generatedAtLabel,
            Action<StringBuilder> appendBody
        )
        {
            Title = title;
            GeneratedAtLabel = generatedAtLabel;
            AppendBody = appendBody;
        }

        private string Title { get; }
        private string GeneratedAtLabel { get; }
        private Action<StringBuilder> AppendBody { get; }

        internal string Build()
            => BuildTimestampedText(Title, GeneratedAtLabel, AppendBody);

        internal string Write(string fileNamePrefix, string fallbackDirectory)
            => WriteTimestampedReport(fileNamePrefix, fallbackDirectory, Build());
    }

    private static string WriteTimestampedReport(
        string fileNamePrefix,
        string fallbackDirectory,
        string text
    )
    {
        var fileName = $"{fileNamePrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
        var targetPath = TryGetExternalDiagnosticsPath(fileName)
            ?? Path.Combine(fallbackDirectory, fileName);

        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        System.IO.File.WriteAllText(targetPath, text);
        return targetPath;
    }

    private static TimestampedText CreateTimestampedText(
        string title,
        string generatedAtLabel,
        Action<StringBuilder> appendBody
    )
        => new(title, generatedAtLabel, appendBody);

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
