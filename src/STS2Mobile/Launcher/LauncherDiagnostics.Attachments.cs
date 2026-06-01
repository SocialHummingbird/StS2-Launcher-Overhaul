using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int LargeAttachmentMaxChars = 256 * 1024;
    private const int SmallAttachmentMaxChars = 64 * 1024;

    private readonly struct DiagnosticAttachment
    {
        private DiagnosticAttachment(string label, string path, int limit)
        {
            Label = label;
            Path = path;
            Limit = limit;
        }

        private string Label { get; }
        private string Path { get; }
        private int Limit { get; }
    }

    private readonly struct DiagnosticFileText
    {
        private DiagnosticFileText(string text, string error)
        {
            Text = text;
            Error = error;
        }

        private string Text { get; }
        private string Error { get; }

        private static DiagnosticFileText Missing()
            => new(null, null);

        private static DiagnosticFileText Read(string text)
            => new(text, null);

        private static DiagnosticFileText Failed(string error)
            => new(null, error);
    }

    private static void AppendSummaryErrorDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendSummarySmallFileAttachments(sb, dataDir);
        AppendSummaryInterestingFileTails(sb, dataDir);
        AppendLogcatErrorSummary(sb);
    }

    private static void AppendRawErrorDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendRawErrorLogAttachments(sb, dataDir);
        AppendRawLogcatTail(sb);
    }

    private static void AppendSummarySmallFileAttachments(StringBuilder sb, string dataDir)
        => AppendAttachments(
            sb,
            SummarySmallFiles(dataDir),
            SummaryFileStatusPrefix,
            SummaryFileStatusPrefix
        );

    private static void AppendSummaryInterestingFileTails(StringBuilder sb, string dataDir)
    {
        foreach (var tail in SummaryInterestingTails(dataDir))
            AppendInterestingFileTail(sb, tail.Label, tail.Path, tail.Limit);
    }

    private static void AppendRawErrorLogAttachments(StringBuilder sb, string dataDir)
        => AppendAttachments(sb, RawErrorLogFiles(dataDir));

    private static IEnumerable<DiagnosticAttachment> SummarySmallFiles(string dataDir)
    {
        yield return Attachment(StartupMarker(dataDir), 2048);
        yield return Attachment(AndroidUncaughtException(dataDir), 4096);
    }

    private static IEnumerable<DiagnosticAttachment> SummaryInterestingTails(string dataDir)
    {
        yield return Attachment(BootstrapTrace(), 80);
        yield return Attachment(StartupSceneSnapshot(dataDir), 80);
    }

    private static IEnumerable<DiagnosticAttachment> RawErrorLogFiles(string dataDir)
    {
        yield return Attachment(
            StartupMarker(dataDir),
            SmallAttachmentMaxChars
        );
        yield return Attachment(
            AndroidUncaughtException(dataDir),
            SmallAttachmentMaxChars
        );
        yield return Attachment(BootstrapTrace(), LargeAttachmentMaxChars);
        yield return Attachment(
            StartupSceneSnapshot(dataDir),
            LargeAttachmentMaxChars
        );
    }

    private static DiagnosticAttachment Attachment(FileReference file, int limit)
        => new(file.Label, file.Path, limit);

    private static void AppendAttachments(
        StringBuilder sb,
        IEnumerable<DiagnosticAttachment> files,
        string missingPrefix = "",
        string failedPrefix = ""
    )
    {
        foreach (var file in files)
        {
            sb.AppendLine();
            sb.AppendLine(Header(file.Label, file.Path));
            AppendTruncatedFile(
                sb,
                file.Path,
                file.Limit,
                missingPrefix,
                failedPrefix
            );
        }
    }

    private static void AppendInterestingFileTail(
        StringBuilder sb,
        string label,
        string path,
        int maxLines
    )
    {
        sb.AppendLine();
        sb.AppendLine(Header(label, path));
        var read = ReadFileText(path);
        if (read.Text == null)
        {
            sb.AppendLine(ReadStatus(read.Error));
            return;
        }

        var lines = read.Text.Replace("\r\n", "\n").Split('\n');
        foreach (var line in SelectInterestingDiagnosticLines(lines, maxLines))
            sb.AppendLine(line);
    }

    private static void AppendTruncatedFile(
        StringBuilder sb,
        string path,
        int maxChars,
        string missingPrefix = "",
        string failedPrefix = ""
    )
    {
        var read = ReadFileText(path);
        if (read.Text != null)
        {
            sb.AppendLine(TruncateForDisplay(read.Text, maxChars));
            return;
        }

        sb.AppendLine(ReadStatus(read.Error, missingPrefix, failedPrefix));
    }

    private static DiagnosticFileText ReadFileText(string path)
    {
        try
        {
            if (!File.Exists(path))
                return DiagnosticFileText.Missing();

            return DiagnosticFileText.Read(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            return DiagnosticFileText.Failed(ex.Message);
        }
    }

    private static string TruncateForDisplay(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text ?? string.Empty;

        return text.Substring(0, maxChars) + "\n<truncated>";
    }

    private static string ReadStatus(
        string error,
        string missingPrefix = "",
        string failedPrefix = ""
    )
        => error == null
            ? $"{missingPrefix}<missing>"
            : $"{failedPrefix}<failed to read: {error}>";
}
