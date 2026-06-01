using System;
using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendFileSummary(
        StringBuilder sb,
        string label,
        string path,
        long inlineContentLimit
    )
    {
        try
        {
            var read = ReadFileText(path);
            sb.AppendLine($"{label}: {path}");
            if (read.Text == null)
            {
                sb.AppendLine(
                    read.Error == null
                        ? "  exists=False"
                        : $"  failed={read.Error}"
                );
                return;
            }

            var file = new FileInfo(path);
            sb.AppendLine("  exists=True");
            sb.AppendLine($"  bytes={file.Length}");
            sb.AppendLine($"  modifiedUtc={file.LastWriteTimeUtc:O}");
            if (file.Length <= inlineContentLimit)
                sb.AppendLine($"  contents={SingleLine(read.Text)}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"{label}: failed to inspect {path}: {ex.Message}");
        }
    }

    private static void AppendFileContentsSection(StringBuilder sb, FileReference file)
    {
        sb.AppendLine(Header(file.Label, file.Path));

        try
        {
            var read = ReadFileText(file.Path);
            if (read.Text == null)
            {
                sb.AppendLine(
                    read.Error == null
                        ? "  exists=False"
                        : $"  failed={read.Error}"
                );
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
}
