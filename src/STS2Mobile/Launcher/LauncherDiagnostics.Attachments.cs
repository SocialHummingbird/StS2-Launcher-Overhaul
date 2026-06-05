using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
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
            file.AppendTruncatedContent(
                sb,
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
}
