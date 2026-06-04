using System;
using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int LargeAttachmentMaxChars = 256 * 1024;
    private const int SmallAttachmentMaxChars = 64 * 1024;

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
        yield return new DiagnosticAttachment(StartupMarker(dataDir), 2048);
        yield return new DiagnosticAttachment(AndroidUncaughtException(dataDir), 4096);
    }

    private static IEnumerable<InterestingDiagnosticTail> SummaryInterestingTails(string dataDir)
    {
        yield return new InterestingDiagnosticTail(BootstrapTrace(), 80);
        yield return new InterestingDiagnosticTail(StartupSceneSnapshot(dataDir), 80);
    }

    private static IEnumerable<DiagnosticAttachment> RawErrorLogFiles(string dataDir)
    {
        yield return new DiagnosticAttachment(
            StartupMarker(dataDir),
            SmallAttachmentMaxChars
        );
        yield return new DiagnosticAttachment(
            AndroidUncaughtException(dataDir),
            SmallAttachmentMaxChars
        );
        yield return new DiagnosticAttachment(BootstrapTrace(), LargeAttachmentMaxChars);
        yield return new DiagnosticAttachment(
            StartupSceneSnapshot(dataDir),
            LargeAttachmentMaxChars
        );
    }

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
            file.AppendHeader(sb);
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
        InterestingDiagnosticTail file
    )
    {
        sb.AppendLine();
        file.AppendHeader(sb);
        var read = file.Read();
        if (!read.HasContent())
        {
            read.AppendStatus(sb);
            return;
        }

        foreach (var line in file.InterestingLines(read))
            sb.AppendLine(line);
    }

    private static void AppendTruncatedFile(
        StringBuilder sb,
        DiagnosticAttachment file,
        string missingPrefix = "",
        string failedPrefix = ""
    )
    {
        var read = file.Read();
        if (read.HasContent())
        {
            sb.AppendLine(file.TruncatedContent(read));
            return;
        }

        read.AppendStatus(sb, missingPrefix, failedPrefix);
    }

    private static FileReadResult ReadFileText(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path))
                return FileReadResult.Missing();

            return FileReadResult.Read(System.IO.File.ReadAllText(path));
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
}
