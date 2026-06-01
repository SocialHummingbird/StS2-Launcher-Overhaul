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
        internal DiagnosticAttachment(DiagnosticFile file, int limit)
        {
            File = file;
            Limit = limit;
        }

        internal DiagnosticFile File { get; }
        internal int Limit { get; }
        internal string Label => File.Label;
        internal string Path => File.Path;
    }

    private readonly struct FileReadResult
    {
        private FileReadResult(string? text, string? error)
        {
            Text = text;
            Error = error;
        }

        internal string? Text { get; }
        internal string? Error { get; }
        internal bool HasText => Text != null;
        internal bool IsMissing => Text == null && Error == null;

        internal static FileReadResult Read(string text)
            => new(text, error: null);

        internal static FileReadResult Missing()
            => new(text: null, error: null);

        internal static FileReadResult Failed(string error)
            => new(text: null, error);
    }

    private static void AppendSummaryErrorDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendAttachments(
            sb,
            SummarySmallFiles(dataDir),
            SummaryFileStatusPrefix,
            SummaryFileStatusPrefix
        );

        foreach (var tail in SummaryInterestingTails(dataDir))
            AppendInterestingFileTail(sb, tail);

        AppendLogcatErrorSummary(sb);
    }

    private static void AppendRawErrorDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendAttachments(sb, RawErrorLogFiles(dataDir));
        AppendRawLogcatTail(sb);
    }

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

    private static DiagnosticAttachment Attachment(DiagnosticFile file, int limit)
        => new(file, limit);

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
                file,
                missingPrefix,
                failedPrefix
            );
        }
    }

    private static void AppendInterestingFileTail(
        StringBuilder sb,
        DiagnosticAttachment file
    )
    {
        sb.AppendLine();
        sb.AppendLine(Header(file.Label, file.Path));
        var read = ReadFileText(file.Path);
        if (!read.HasText)
        {
            sb.AppendLine(ReadStatus(read));
            return;
        }

        var lines = read.Text.Replace("\r\n", "\n").Split('\n');
        foreach (var line in SelectInterestingDiagnosticLines(lines, file.Limit))
            sb.AppendLine(line);
    }

    private static void AppendTruncatedFile(
        StringBuilder sb,
        DiagnosticAttachment file,
        string missingPrefix = "",
        string failedPrefix = ""
    )
    {
        var read = ReadFileText(file.Path);
        if (read.HasText)
        {
            sb.AppendLine(TruncateForDisplay(read.Text, file.Limit));
            return;
        }

        sb.AppendLine(ReadStatus(read, missingPrefix, failedPrefix));
    }

    private static FileReadResult ReadFileText(string path)
    {
        try
        {
            if (!File.Exists(path))
                return FileReadResult.Missing();

            return FileReadResult.Read(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            return FileReadResult.Failed(ex.Message);
        }
    }

    private static string TruncateForDisplay(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text ?? string.Empty;

        return text.Substring(0, maxChars) + "\n<truncated>";
    }

    private static string ReadStatus(
        FileReadResult read,
        string missingPrefix = "",
        string failedPrefix = ""
    )
        => read.IsMissing
            ? $"{missingPrefix}<missing>"
            : $"{failedPrefix}<failed to read: {read.Error}>";
}
