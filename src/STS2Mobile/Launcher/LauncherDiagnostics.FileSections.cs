using System;
using System.IO;
using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private readonly struct DiagnosticFileSnapshot
    {
        private DiagnosticFileSnapshot(
            FileReadResult read,
            long bytes,
            DateTime modifiedUtc
        )
        {
            Read = read;
            Bytes = bytes;
            ModifiedUtc = modifiedUtc;
        }

        internal FileReadResult Read { get; }
        internal long Bytes { get; }
        internal DateTime ModifiedUtc { get; }
        internal bool HasText => Read.HasText;
        internal string Text => Read.Content;

        internal static DiagnosticFileSnapshot From(DiagnosticFile file)
        {
            var read = ReadFileText(file.Path);
            if (!read.HasText)
                return new DiagnosticFileSnapshot(read, bytes: 0, modifiedUtc: default);

            var info = new FileInfo(file.Path);
            return new DiagnosticFileSnapshot(read, info.Length, info.LastWriteTimeUtc);
        }
    }

    private static void AppendFileSummary(
        StringBuilder sb,
        DiagnosticFile file,
        long inlineContentLimit
    )
    {
        try
        {
            var snapshot = DiagnosticFileSnapshot.From(file);
            sb.AppendLine($"{file.Label}: {file.Path}");
            if (!snapshot.HasText)
            {
                AppendFileReadStatus(sb, snapshot.Read);
                return;
            }

            sb.AppendLine("  exists=True");
            sb.AppendLine($"  bytes={snapshot.Bytes}");
            sb.AppendLine($"  modifiedUtc={snapshot.ModifiedUtc:O}");
            if (snapshot.Bytes <= inlineContentLimit)
                sb.AppendLine($"  contents={SingleLine(snapshot.Text)}");
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
            var snapshot = DiagnosticFileSnapshot.From(file);
            if (!snapshot.HasText)
            {
                AppendFileReadStatus(sb, snapshot.Read);
                sb.AppendLine();
                return;
            }

            sb.AppendLine(
                $"  exists=True bytes={snapshot.Bytes} modifiedUtc={snapshot.ModifiedUtc:O}"
            );
            sb.AppendLine("  contents:");
            sb.AppendLine(snapshot.Text);
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
