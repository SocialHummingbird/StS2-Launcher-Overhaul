using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int LargeAttachmentMaxChars = 256 * 1024;
    private const int SmallAttachmentMaxChars = 64 * 1024;

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
            AppendInterestingFileTail(sb, tail);
    }

    private static void AppendRawErrorLogAttachments(StringBuilder sb, string dataDir)
        => AppendAttachments(sb, RawErrorLogFiles(dataDir));

    private static IEnumerable<(string Label, string Path, int Limit)> SummarySmallFiles(string dataDir)
    {
        yield return Attachment(StartupMarker(dataDir), 2048);
        yield return Attachment(AndroidUncaughtException(dataDir), 4096);
    }

    private static IEnumerable<(string Label, string Path, int Limit)> SummaryInterestingTails(string dataDir)
    {
        yield return Attachment(BootstrapTrace(), 80);
        yield return Attachment(StartupSceneSnapshot(dataDir), 80);
    }

    private static IEnumerable<(string Label, string Path, int Limit)> RawErrorLogFiles(string dataDir)
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

    private static (string Label, string Path, int Limit) Attachment(
        (string Label, string Path) file,
        int limit
    )
        => (file.Label, file.Path, limit);

    private static void AppendAttachments(
        StringBuilder sb,
        IEnumerable<(string Label, string Path, int Limit)> files,
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
        (string Label, string Path, int Limit) file
    )
    {
        sb.AppendLine();
        sb.AppendLine(Header(file.Label, file.Path));
        var read = ReadFileText(file.Path);
        if (read.Text == null)
        {
            sb.AppendLine(ReadStatus(read.Error));
            return;
        }

        var lines = read.Text.Replace("\r\n", "\n").Split('\n');
        foreach (var line in SelectInterestingDiagnosticLines(lines, file.Limit))
            sb.AppendLine(line);
    }

    private static void AppendTruncatedFile(
        StringBuilder sb,
        (string Label, string Path, int Limit) file,
        string missingPrefix = "",
        string failedPrefix = ""
    )
    {
        var read = ReadFileText(file.Path);
        if (read.Text != null)
        {
            sb.AppendLine(TruncateForDisplay(read.Text, file.Limit));
            return;
        }

        sb.AppendLine(ReadStatus(read.Error, missingPrefix, failedPrefix));
    }

    private static (string? Text, string? Error) ReadFileText(string path)
    {
        try
        {
            if (!File.Exists(path))
                return (Text: null, Error: null);

            return (Text: File.ReadAllText(path), Error: null);
        }
        catch (Exception ex)
        {
            return (Text: null, Error: ex.Message);
        }
    }

    private static string TruncateForDisplay(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text ?? string.Empty;

        return text.Substring(0, maxChars) + "\n<truncated>";
    }

    private static string ReadStatus(
        string? error,
        string missingPrefix = "",
        string failedPrefix = ""
    )
        => error == null
            ? $"{missingPrefix}<missing>"
            : $"{failedPrefix}<failed to read: {error}>";
}
