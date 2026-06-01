using System;
using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendFileSummary(
        StringBuilder sb,
        DiagnosticFile file,
        long inlineContentLimit
    )
    {
        try
        {
            var read = ReadFileText(file.Path);
            sb.AppendLine($"{file.Label}: {file.Path}");
            if (!read.HasText)
            {
                AppendFileReadStatus(sb, read);
                return;
            }

            var info = new FileInfo(file.Path);
            sb.AppendLine("  exists=True");
            sb.AppendLine($"  bytes={info.Length}");
            sb.AppendLine($"  modifiedUtc={info.LastWriteTimeUtc:O}");
            if (info.Length <= inlineContentLimit)
                sb.AppendLine($"  contents={SingleLine(read.Text)}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"{file.Label}: failed to inspect {file.Path}: {ex.Message}");
        }
    }

    private static void AppendFileContentsSection(
        StringBuilder sb,
        DiagnosticFile file
    )
    {
        sb.AppendLine(Header(file.Label, file.Path));

        try
        {
            var read = ReadFileText(file.Path);
            if (!read.HasText)
            {
                AppendFileReadStatus(sb, read);
                sb.AppendLine();
                return;
            }

            var info = new FileInfo(file.Path);
            sb.AppendLine(
                $"  exists=True bytes={info.Length} modifiedUtc={info.LastWriteTimeUtc:O}"
            );
            sb.AppendLine("  contents:");
            sb.AppendLine(read.Text);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  failed={ex.Message}");
        }

        sb.AppendLine();
    }

    private static string SingleLine(string text)
        => text.Replace('\n', ' ').Replace('\r', ' ');

    private static void AppendFileReadStatus(StringBuilder sb, FileReadResult read)
        => sb.AppendLine(
            read.IsMissing
                ? "  exists=False"
                : $"  failed={read.Error}"
        );
}
